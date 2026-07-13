using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Evo.Infrastructure.Services.Ads.Config;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Core.Async;
using R3;

namespace Evo.Infrastructure.Services.Ads
{
    public sealed class AdsService : IAdsService
    {
        private const string ADS_SERVICE_SOURCE = nameof(AdsService);

        private readonly AdsAdapterSelector _adapterSelector;
        private readonly AdsServiceOptions _options;
        private readonly AdsConfig _adsConfig;
        private readonly IAnalyticsService _analyticsService;
        private readonly Dictionary<AvailabilityKey, ReactiveProperty<AdsAvailability>> _availabilityStreams = new();
        private readonly HashSet<IAdsAdapter> _availabilitySubscribedAdapters = new();
        private readonly AsyncGate _showGate = new();
        private bool _isShowing;
        private AdType _showingAdType;
        private string _showingPlacementId;

        public AdsService(
            IEnumerable<IAdsAdapterFactory> adapterFactories,
            IConfigService configService = null,
            IAnalyticsService analyticsService = null,
            AdsServiceOptions options = default)
        {
            _analyticsService = analyticsService;
            AdsRoutingConfig routingConfig = null;
            AdsConfig adsConfig = null;
            PlatformCatalog platformCatalog = null;
            if (configService != null)
            {
                configService.TryGet(out routingConfig);
                configService.TryGet(out adsConfig);
                configService.TryGet(out platformCatalog);
            }

            _adsConfig = adsConfig;
            _options = options.Equals(default(AdsServiceOptions))
                ? BuildOptionsFromConfig(adsConfig)
                : options;
            var platformId = platformCatalog?.CurrentPlatformId;
            var order = routingConfig != null ? routingConfig.BuildAdapterOrder(platformId) : null;
            _adapterSelector = new AdsAdapterSelector(adapterFactories?.ToList(), order);
        }

        public bool CanShow(AdType adType, string placementId = null)
        {
            return GetAvailability(adType, placementId).CanShow;
        }

        public AdsAvailability GetAvailability(AdType adType, string placementId = null)
        {
            var adapters = EnumerateAdaptersWithEventSubscriptions();
            if (adapters.Count == 0)
            {
                return new AdsAvailability(
                    adType,
                    placementId,
                    false,
                    false,
                    false,
                    IsShowing(adType, placementId),
                    null,
                    "No ads adapters registered.");
            }

            IAdsAdapter firstConfigured = null;
            IAdsAdapter firstLoading = null;
            for (var i = 0; i < adapters.Count; i++)
            {
                var adapter = adapters[i];
                firstConfigured ??= adapter;

                if (adapter is IAdsAvailabilityAdapter availabilityAdapter &&
                    availabilityAdapter.IsLoading(adType, placementId))
                {
                    firstLoading ??= adapter;
                }

                if (adapter.IsReady(adType, placementId))
                {
                    return new AdsAvailability(
                        adType,
                        placementId,
                        true,
                        true,
                        firstLoading != null,
                        IsShowing(adType, placementId),
                        adapter.AdapterId);
                }
            }

            return new AdsAvailability(
                adType,
                placementId,
                true,
                false,
                firstLoading != null,
                IsShowing(adType, placementId),
                firstLoading?.AdapterId ?? firstConfigured?.AdapterId,
                firstLoading != null ? null : "No ready adapter found.");
        }

        public Observable<AdsAvailability> ObserveAvailability(AdType adType, string placementId = null)
        {
            var stream = GetAvailabilityStream(adType, placementId);
            stream.Value = GetAvailability(adType, placementId);
            return stream;
        }

        public void Preload(AdType adType, string placementId = null)
        {
            var adapters = EnumerateAdaptersWithEventSubscriptions();
            for (var i = 0; i < adapters.Count; i++)
            {
                if (adapters[i] is IAdsPreloadAdapter preloadAdapter)
                {
                    try
                    {
                        preloadAdapter.Preload(adType, placementId);
                    }
                    catch (Exception ex)
                    {
                        EvoDebug.LogError(
                            $"Adapter '{adapters[i].AdapterId}' preload failed: {ex.Message}",
                            ADS_SERVICE_SOURCE);
                    }
                }
            }

            NotifyAvailabilityChanged(adType, placementId);
        }

        public async UniTask<AdsShowResult> ShowAsync(
            AdsShowRequest request,
            CancellationToken cancellationToken = default)
        {
            using var showLease = await _showGate.EnterAsync(cancellationToken);
            SetShowing(request.AdType, request.PlacementId, true);
            try
            {
                return await ShowInternalAsync(request, cancellationToken);
            }
            finally
            {
                SetShowing(request.AdType, request.PlacementId, false);
            }
        }

        private async UniTask<AdsShowResult> ShowInternalAsync(
            AdsShowRequest request,
            CancellationToken cancellationToken)
        {
            EvoDebug.Log(
                $"Ads show requested: type={request.AdType}, placement='{request.PlacementId ?? "none"}'.",
                ADS_SERVICE_SOURCE);

            if (request.AdType == AdType.Banner)
            {
                return Finalize(
                    request,
                    new AdsShowResult(
                        AdsShowStatus.Skipped,
                        request.AdType,
                        null,
                        false,
                        "Use ShowBanner/HideBanner for banner flow.",
                        0));
            }

            if (!_adapterSelector.EnumerateOrderedAdapters().Any())
            {
                return Finalize(
                    request,
                    new AdsShowResult(
                        AdsShowStatus.Skipped,
                        request.AdType,
                        null,
                        false,
                        "No ads adapters registered.",
                        0));
            }

            var timeoutMs = GetShowTimeoutMs(request);

            var attempt = 0;
            var skippedAdapters = new HashSet<IAdsAdapter>();
            while (attempt <= _options.MaxFallbackAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_adapterSelector.TryGetReady(request.AdType, request.PlacementId, skippedAdapters, out var adapter))
                {
                    return Finalize(
                        request,
                        new AdsShowResult(
                            AdsShowStatus.Skipped,
                            request.AdType,
                            null,
                            attempt > 0,
                            "No ready adapter found.",
                            0));
                }

                TrackInterstitialStart(request);
                var result = await ExecuteWithTimeout(adapter, request, timeoutMs, attempt > 0, cancellationToken);
                if (result.Status == AdsShowStatus.Shown)
                {
                    return Finalize(request, result);
                }

                skippedAdapters.Add(adapter);
                attempt++;
            }

            return Finalize(
                request,
                new AdsShowResult(
                    AdsShowStatus.Failed,
                    request.AdType,
                    null,
                    true,
                    "Show flow exhausted fallback attempts.",
                    0));
        }

        public void ShowBanner(string placementId = null)
        {
            NotifyAvailabilityChanged(AdType.Banner, placementId);
            if (_adapterSelector.TryGetReady(AdType.Banner, placementId, null, out var adapter))
            {
                adapter.ShowBanner(placementId);
            }
            else
            {
                EvoDebug.LogWarning("ShowBanner skipped: no ready banner adapter.", ADS_SERVICE_SOURCE);
            }
        }

        public void HideBanner(string placementId = null)
        {
            var adapters = _adapterSelector.EnumerateOrderedAdapters();
            for (var i = 0; i < adapters.Count; i++)
            {
                adapters[i].HideBanner(placementId);
            }
            NotifyAvailabilityChanged(AdType.Banner, placementId);
        }

        private async UniTask<AdsShowResult> ExecuteWithTimeout(
            IAdsAdapter adapter,
            AdsShowRequest request,
            int timeoutMs,
            bool isFallbackUsed,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                try
                {
                    var showTask = adapter.ShowAsync(request, linkedCts.Token).AsTask();
                    var timeoutTask = Task.Delay(timeoutMs, linkedCts.Token);

                    var completedTask = await Task.WhenAny(showTask, timeoutTask);
                    if (completedTask != showTask)
                    {
                        linkedCts.Cancel();
                        sw.Stop();
                        return new AdsShowResult(
                            AdsShowStatus.Timeout,
                            request.AdType,
                            adapter.AdapterId,
                            isFallbackUsed,
                            $"Timeout after {timeoutMs} ms.",
                            (int)sw.ElapsedMilliseconds);
                    }

                    linkedCts.Cancel();
                    var adapterResult = await showTask;
                    sw.Stop();

                    return new AdsShowResult(
                        adapterResult.Status,
                        request.AdType,
                        string.IsNullOrEmpty(adapterResult.AdapterId) ? adapter.AdapterId : adapterResult.AdapterId,
                        isFallbackUsed,
                        adapterResult.Error,
                        (int)sw.ElapsedMilliseconds);
                }
                finally
                {
                    linkedCts.Dispose();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                return new AdsShowResult(
                    AdsShowStatus.Skipped,
                    request.AdType,
                    adapter.AdapterId,
                    isFallbackUsed,
                    "Canceled by caller.",
                    (int)sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                EvoDebug.LogError($"Adapter '{adapter.AdapterId}' failed: {ex.Message}", ADS_SERVICE_SOURCE);
                return new AdsShowResult(
                    AdsShowStatus.Failed,
                    request.AdType,
                    adapter.AdapterId,
                    isFallbackUsed,
                    ex.Message,
                    (int)sw.ElapsedMilliseconds);
            }
        }

        private int GetShowTimeoutMs(AdsShowRequest request)
        {
            if (request.TimeoutMsOverride.HasValue && request.TimeoutMsOverride.Value > 0)
            {
                return request.TimeoutMsOverride.Value;
            }

            if (_adsConfig != null)
            {
                return _adsConfig.GetShowTimeoutMs(request.AdType);
            }

            return _options.DefaultShowTimeoutMs;
        }

        private static AdsServiceOptions BuildOptionsFromConfig(AdsConfig adsConfig)
        {
            if (adsConfig == null)
            {
                return new AdsServiceOptions();
            }

            return new AdsServiceOptions(
                adsConfig.DefaultShowTimeoutMs,
                adsConfig.MaxFallbackAttempts);
        }

        private void TrackShowResult(AdsShowRequest request, AdsShowResult result)
        {
            if (_analyticsService == null)
            {
                return;
            }

            if (request.AdType == AdType.Banner)
            {
                return;
            }

            var adData = new AdEventData(
                AnalyticsEventKeys.EVENT_AD_SHOW_RESULT,
                platform: result.AdapterId,
                networkName: result.AdapterId,
                unitId: null,
                format: request.AdType.ToString(),
                placement: request.PlacementId,
                networkPlacement: null,
                currency: null,
                revenue: 0,
                countryCode: null);

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [AnalyticsEventKeys.PARAM_STATUS] = result.Status.ToString(),
                [AnalyticsEventKeys.PARAM_AD_TYPE] = request.AdType.ToString(),
                [AnalyticsEventKeys.PARAM_ADAPTER_ID] = result.AdapterId,
                [AnalyticsEventKeys.PARAM_FALLBACK_USED] = result.IsFallbackUsed,
                [AnalyticsEventKeys.PARAM_DURATION_MS] = result.DurationMs
            };

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                parameters[AnalyticsEventKeys.PARAM_ERROR] = result.Error;
            }

            _analyticsService.TrackAd(adData, parameters);

            TrackAdImpression(request, result);
            TrackReward(request, result);
            TrackInterstitialResult(request, result);
        }

        private void TrackInterstitialStart(AdsShowRequest request)
        {
            if (_analyticsService == null || request.AdType != AdType.Interstitial)
            {
                return;
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(request.PlacementId))
            {
                parameters[AnalyticsEventKeys.PARAM_PLACEMENT] = request.PlacementId;
            }

            _analyticsService.TrackCustom(AnalyticsEventKeys.EVENT_INTER_START, parameters.Count > 0 ? parameters : null);
        }

        private void TrackInterstitialResult(AdsShowRequest request, AdsShowResult result)
        {
            if (_analyticsService == null || request.AdType != AdType.Interstitial)
            {
                return;
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(request.PlacementId))
            {
                parameters[AnalyticsEventKeys.PARAM_PLACEMENT] = request.PlacementId;
            }

            if (result.Status == AdsShowStatus.Shown)
            {
                _analyticsService.TrackCustom(AnalyticsEventKeys.EVENT_INTER_SUCCESS, parameters.Count > 0 ? parameters : null);
            }
            else
            {
                _analyticsService.TrackCustom(AnalyticsEventKeys.EVENT_INTER_FAIL, parameters.Count > 0 ? parameters : null);
            }
        }

        private void TrackAdImpression(AdsShowRequest request, AdsShowResult result)
        {
            if (_analyticsService == null || result.Status != AdsShowStatus.Shown)
            {
                return;
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(request.PlacementId))
            {
                parameters[AnalyticsEventKeys.PARAM_PLACEMENT] = request.PlacementId;
            }

            _analyticsService.TrackCustom(AnalyticsEventKeys.EVENT_AD_IMPRESSION, parameters.Count > 0 ? parameters : null);
        }

        private void TrackReward(AdsShowRequest request, AdsShowResult result)
        {
            if (_analyticsService == null || request.AdType != AdType.Rewarded || result.Status != AdsShowStatus.Shown)
            {
                return;
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(request.PlacementId))
            {
                parameters[AnalyticsEventKeys.PARAM_PLACEMENT] = request.PlacementId;
            }

            _analyticsService.TrackCustom(AnalyticsEventKeys.EVENT_AD_REWARD, parameters.Count > 0 ? parameters : null);
        }

        private AdsShowResult Finalize(AdsShowRequest request, AdsShowResult result)
        {
            TrackShowResult(request, result);
            NotifyAvailabilityChanged(request.AdType, request.PlacementId);
            EvoDebug.Log(
                $"Ads show result: type={result.AdType}, status={result.Status}, adapter='{result.AdapterId ?? "none"}', placement='{request.PlacementId ?? "none"}', fallback={result.IsFallbackUsed}, durationMs={result.DurationMs}, error='{result.Error ?? "none"}'.",
                ADS_SERVICE_SOURCE);
            return result;
        }

        private IReadOnlyList<IAdsAdapter> EnumerateAdaptersWithEventSubscriptions()
        {
            var adapters = _adapterSelector.EnumerateOrderedAdapters();
            for (var i = 0; i < adapters.Count; i++)
            {
                SubscribeAvailabilityEvents(adapters[i]);
            }

            return adapters;
        }

        private void SubscribeAvailabilityEvents(IAdsAdapter adapter)
        {
            if (adapter == null || _availabilitySubscribedAdapters.Contains(adapter))
            {
                return;
            }

            _availabilitySubscribedAdapters.Add(adapter);
            if (adapter is IAdsAvailabilityEvents events)
            {
                events.AvailabilityChanged += NotifyAvailabilityChanged;
            }
        }

        private ReactiveProperty<AdsAvailability> GetAvailabilityStream(AdType adType, string placementId)
        {
            var key = new AvailabilityKey(adType, placementId);
            if (_availabilityStreams.TryGetValue(key, out var stream))
            {
                return stream;
            }

            stream = new ReactiveProperty<AdsAvailability>(GetAvailability(adType, placementId));
            _availabilityStreams[key] = stream;
            return stream;
        }

        private void NotifyAvailabilityChanged(AdType adType, string placementId)
        {
            foreach (var pair in _availabilityStreams.ToArray())
            {
                if (pair.Key.Matches(adType, placementId))
                {
                    pair.Value.Value = GetAvailability(pair.Key.AdType, pair.Key.PlacementId);
                }
            }
        }

        private bool IsShowing(AdType adType, string placementId)
        {
            if (!_isShowing || _showingAdType != adType)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(placementId))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(_showingPlacementId))
            {
                return true;
            }

            return string.Equals(_showingPlacementId ?? string.Empty, placementId, StringComparison.Ordinal);
        }

        private void SetShowing(AdType adType, string placementId, bool isShowing)
        {
            _showingAdType = adType;
            _showingPlacementId = placementId;
            _isShowing = isShowing;
            NotifyAvailabilityChanged(adType, placementId);
        }

        private readonly struct AvailabilityKey : IEquatable<AvailabilityKey>
        {
            public AdType AdType { get; }
            public string PlacementId { get; }

            public AvailabilityKey(AdType adType, string placementId)
            {
                AdType = adType;
                PlacementId = placementId ?? string.Empty;
            }

            public bool Equals(AvailabilityKey other)
            {
                return AdType == other.AdType &&
                       string.Equals(PlacementId, other.PlacementId, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is AvailabilityKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)AdType * 397) ^ StringComparer.Ordinal.GetHashCode(PlacementId);
                }
            }

            public bool Matches(AdType adType, string placementId)
            {
                if (AdType != adType)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(placementId))
                {
                    return true;
                }

                return string.Equals(PlacementId, placementId, StringComparison.Ordinal) ||
                       string.IsNullOrWhiteSpace(PlacementId);
            }
        }
    }
}
