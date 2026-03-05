using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.Ads.Config;
using _Project.Scripts.Infrastructure.Services.Analytics;
using _Project.Scripts.Infrastructure.Services.Config;
using _Project.Scripts.Infrastructure.Services.Debug;
using _Project.Scripts.Infrastructure.Services.PlatformInfo.Config;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Services.Ads
{
    public sealed class AdsService : IAdsService
    {
        private const string ADS_SERVICE_SOURCE = nameof(AdsService);

        private readonly AdsAdapterSelector _adapterSelector;
        private readonly AdsServiceOptions _options;
        private readonly AdsConfig _adsConfig;
        private readonly IAnalyticsService _analyticsService;

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
            return _adapterSelector.TryGetReady(adType, placementId, null, out _);
        }

        public async UniTask<AdsShowResult> ShowAsync(
            AdsShowRequest request,
            CancellationToken cancellationToken = default)
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
            EvoDebug.Log(
                $"Ads show result: type={result.AdType}, status={result.Status}, adapter='{result.AdapterId ?? "none"}', placement='{request.PlacementId ?? "none"}', fallback={result.IsFallbackUsed}, durationMs={result.DurationMs}, error='{result.Error ?? "none"}'.",
                ADS_SERVICE_SOURCE);
            return result;
        }
    }
}
