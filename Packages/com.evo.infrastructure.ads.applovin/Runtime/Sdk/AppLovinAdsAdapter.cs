using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Ads.AppLovin
{
    public sealed class AppLovinAdsAdapter :
        IAdsAdapter,
        IAdsPreloadAdapter,
        IAdsAvailabilityAdapter,
        IAdsAvailabilityEvents,
        IDisposable
    {
        private const string SOURCE = "AppLovin MAX Ads Adapter";

        private readonly AppLovinAdsAdapterConfig _config;
        private readonly IAnalyticsService _analytics;

        private UniTaskCompletionSource<AdsShowResult> _interstitialCompletion;
        private UniTaskCompletionSource<AdsShowResult> _rewardedCompletion;
        private bool _rewardReceived;
        private bool _initializationAttempted;
        private bool _available;
        private bool _interstitialLoading;
        private bool _rewardedLoading;
        private bool _subscribed;
        private bool _disposed;
        private bool _warningLogged;

        public AppLovinAdsAdapter(IConfigService configs, IAnalyticsService analytics = null)
        {
            _analytics = analytics;
            configs?.TryGet(out _config);

            AdapterId = string.IsNullOrWhiteSpace(_config?.AdapterId)
                ? AppLovinAdsAdapterFactory.ID
                : _config.AdapterId.Trim();

            InitializeDeferredAsync().Forget(exception =>
                FailInitialization($"Deferred initialization failed: {exception.Message}"));
        }

        public string AdapterId { get; }
        public int Priority => 0;
        public bool IsInitialized => _initializationAttempted;

        public event Action<AdType, string> AvailabilityChanged;

        public bool IsReady(AdType type, string placementId = null)
        {
            if (!_available || _disposed)
            {
                return false;
            }

            try
            {
                switch (type)
                {
                    case AdType.Interstitial:
                        return HasValue(_config.InterstitialPlacementId) &&
                               _interstitialCompletion == null &&
                               MaxSdk.IsInterstitialReady(_config.InterstitialPlacementId);
                    case AdType.Rewarded:
                        return HasValue(_config.RewardedPlacementId) &&
                               _rewardedCompletion == null &&
                               MaxSdk.IsRewardedAdReady(_config.RewardedPlacementId);
                    case AdType.Banner:
                        return HasValue(_config.BannerPlacementId);
                    default:
                        return false;
                }
            }
            catch (Exception exception)
            {
                WarnOnce($"MAX readiness check failed: {exception.Message}");
                return false;
            }
        }

        public bool IsLoading(AdType type, string placementId = null)
        {
            return type switch
            {
                AdType.Interstitial => _interstitialLoading,
                AdType.Rewarded => _rewardedLoading,
                _ => false
            };
        }

        public void Preload(AdType type, string placementId = null)
        {
            switch (type)
            {
                case AdType.Interstitial:
                    LoadInterstitial();
                    break;
                case AdType.Rewarded:
                    LoadRewarded();
                    break;
            }
        }

        public UniTask<AdsShowResult> ShowAsync(
            AdsShowRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_initializationAttempted)
            {
                return CompletedResult(request.AdType, AdsShowStatus.Skipped, "MAX is not initialized yet.");
            }

            if (!_available)
            {
                return CompletedResult(request.AdType, AdsShowStatus.Skipped, "MAX is unavailable.");
            }

            return request.AdType switch
            {
                AdType.Interstitial => ShowInterstitial(request, cancellationToken),
                AdType.Rewarded => ShowRewarded(request, cancellationToken),
                _ => CompletedResult(request.AdType, AdsShowStatus.Skipped, "Unsupported ad type.")
            };
        }

        public void ShowBanner(string placementId = null)
        {
            if (!_available || !HasValue(_config?.BannerPlacementId))
            {
                return;
            }

            try
            {
                var configuration = new MaxSdkBase.AdViewConfiguration(
                    MaxSdkBase.AdViewPosition.BottomCenter);
                MaxSdk.CreateBanner(_config.BannerPlacementId, configuration);
                MaxSdk.ShowBanner(_config.BannerPlacementId);
            }
            catch (Exception exception)
            {
                WarnOnce($"MAX banner show failed: {exception.Message}");
            }
        }

        public void HideBanner(string placementId = null)
        {
            if (!_available || !HasValue(_config?.BannerPlacementId))
            {
                return;
            }

            try
            {
                MaxSdk.HideBanner(_config.BannerPlacementId);
            }
            catch (Exception exception)
            {
                WarnOnce($"MAX banner hide failed: {exception.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _available = false;
            Unsubscribe();
            CompleteInterstitial(AdsShowStatus.Skipped, "Adapter disposed.");
            CompleteRewarded(AdsShowStatus.Skipped, "Adapter disposed.");
        }

        private async UniTask InitializeDeferredAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);

            try
            {
                if (_config == null)
                {
                    FailInitialization("AppLovin config is missing; adapter is disabled.");
                    return;
                }

                if (!HasValue(_config.AppKey))
                {
                    FailInitialization("AppLovin SDK key is missing; adapter is disabled.");
                    return;
                }

                if (!HasAnyAdUnit())
                {
                    FailInitialization("AppLovin ad unit ids are missing; adapter is disabled.");
                    return;
                }

                Subscribe();
                if (MaxSdk.IsInitialized())
                {
                    OnInitialized(null);
                }
                else
                {
                    MaxSdk.InitializeSdk();
                }
            }
            catch (Exception exception)
            {
                FailInitialization($"MAX initialization failed: {exception.Message}");
            }
            finally
            {
                _initializationAttempted = true;
            }
        }

        private void OnInitialized(MaxSdkBase.SdkConfiguration configuration)
        {
            if (_disposed)
            {
                return;
            }

            _available = true;
            _initializationAttempted = true;
            LoadInterstitial();
            LoadRewarded();
            Notify(AdType.Interstitial);
            Notify(AdType.Rewarded);
        }

        private UniTask<AdsShowResult> ShowInterstitial(
            AdsShowRequest request,
            CancellationToken cancellationToken)
        {
            if (!HasValue(_config.InterstitialPlacementId))
            {
                return CompletedResult(
                    request.AdType,
                    AdsShowStatus.Skipped,
                    "Interstitial ad unit is not configured.");
            }

            if (_interstitialCompletion != null)
            {
                return CompletedResult(
                    request.AdType,
                    AdsShowStatus.Skipped,
                    "Interstitial show is already in progress.");
            }

            if (!IsReady(AdType.Interstitial))
            {
                LoadInterstitial();
                return CompletedResult(
                    request.AdType,
                    AdsShowStatus.Skipped,
                    "Interstitial is unavailable.");
            }

            _interstitialCompletion = new UniTaskCompletionSource<AdsShowResult>();
            var task = _interstitialCompletion.Task;
            cancellationToken.Register(() =>
                CompleteInterstitial(AdsShowStatus.Skipped, "Canceled by caller."));

            try
            {
                MaxSdk.ShowInterstitial(_config.InterstitialPlacementId, request.PlacementId);
            }
            catch (Exception exception)
            {
                CompleteInterstitial(AdsShowStatus.Failed, exception.Message);
            }

            return task;
        }

        private UniTask<AdsShowResult> ShowRewarded(
            AdsShowRequest request,
            CancellationToken cancellationToken)
        {
            if (!HasValue(_config.RewardedPlacementId))
            {
                return CompletedResult(
                    request.AdType,
                    AdsShowStatus.Skipped,
                    "Rewarded ad unit is not configured.");
            }

            if (_rewardedCompletion != null)
            {
                return CompletedResult(
                    request.AdType,
                    AdsShowStatus.Skipped,
                    "Rewarded show is already in progress.");
            }

            if (!IsReady(AdType.Rewarded))
            {
                LoadRewarded();
                return CompletedResult(
                    request.AdType,
                    AdsShowStatus.Skipped,
                    "Rewarded ad is unavailable.");
            }

            _rewardReceived = false;
            _rewardedCompletion = new UniTaskCompletionSource<AdsShowResult>();
            var task = _rewardedCompletion.Task;
            cancellationToken.Register(() =>
                CompleteRewarded(AdsShowStatus.Skipped, "Canceled by caller."));

            try
            {
                MaxSdk.ShowRewardedAd(_config.RewardedPlacementId, request.PlacementId);
            }
            catch (Exception exception)
            {
                CompleteRewarded(AdsShowStatus.Failed, exception.Message);
            }

            return task;
        }

        private void LoadInterstitial()
        {
            if (!_available || _interstitialLoading || !HasValue(_config?.InterstitialPlacementId))
            {
                return;
            }

            try
            {
                if (MaxSdk.IsInterstitialReady(_config.InterstitialPlacementId))
                {
                    return;
                }

                _interstitialLoading = true;
                MaxSdk.LoadInterstitial(_config.InterstitialPlacementId);
            }
            catch (Exception exception)
            {
                _interstitialLoading = false;
                WarnOnce($"MAX interstitial load failed: {exception.Message}");
            }
        }

        private void LoadRewarded()
        {
            if (!_available || _rewardedLoading || !HasValue(_config?.RewardedPlacementId))
            {
                return;
            }

            try
            {
                if (MaxSdk.IsRewardedAdReady(_config.RewardedPlacementId))
                {
                    return;
                }

                _rewardedLoading = true;
                MaxSdk.LoadRewardedAd(_config.RewardedPlacementId);
            }
            catch (Exception exception)
            {
                _rewardedLoading = false;
                WarnOnce($"MAX rewarded load failed: {exception.Message}");
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            MaxSdkCallbacks.OnSdkInitializedEvent += OnInitialized;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenue;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRevenue;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnInitialized;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnRevenue;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRevenue;
        }

        private void OnInterstitialLoaded(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _config.InterstitialPlacementId)
            {
                return;
            }

            _interstitialLoading = false;
            Notify(AdType.Interstitial);
        }

        private void OnInterstitialLoadFailed(string id, MaxSdkBase.ErrorInfo error)
        {
            if (id != _config.InterstitialPlacementId)
            {
                return;
            }

            _interstitialLoading = false;
            Notify(AdType.Interstitial);
        }

        private void OnInterstitialHidden(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _config.InterstitialPlacementId)
            {
                return;
            }

            CompleteInterstitial(AdsShowStatus.Shown, null);
            LoadInterstitial();
        }

        private void OnInterstitialDisplayFailed(
            string id,
            MaxSdkBase.ErrorInfo error,
            MaxSdkBase.AdInfo info)
        {
            if (id != _config.InterstitialPlacementId)
            {
                return;
            }

            CompleteInterstitial(AdsShowStatus.Failed, error?.ToString());
            LoadInterstitial();
        }

        private void OnRewardedLoaded(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _config.RewardedPlacementId)
            {
                return;
            }

            _rewardedLoading = false;
            Notify(AdType.Rewarded);
        }

        private void OnRewardedLoadFailed(string id, MaxSdkBase.ErrorInfo error)
        {
            if (id != _config.RewardedPlacementId)
            {
                return;
            }

            _rewardedLoading = false;
            Notify(AdType.Rewarded);
        }

        private void OnReward(string id, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
        {
            if (id == _config.RewardedPlacementId)
            {
                _rewardReceived = true;
            }
        }

        private void OnRewardedHidden(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _config.RewardedPlacementId)
            {
                return;
            }

            var status = _rewardReceived ? AdsShowStatus.Shown : AdsShowStatus.Skipped;
            var error = _rewardReceived ? null : "Reward was not granted.";
            CompleteRewarded(status, error);
            LoadRewarded();
        }

        private void OnRewardedDisplayFailed(
            string id,
            MaxSdkBase.ErrorInfo error,
            MaxSdkBase.AdInfo info)
        {
            if (id != _config.RewardedPlacementId)
            {
                return;
            }

            CompleteRewarded(AdsShowStatus.Failed, error?.ToString());
            LoadRewarded();
        }

        private void OnRevenue(string id, MaxSdkBase.AdInfo info)
        {
            if (_analytics == null ||
                info == null ||
                info.Revenue <= 0d ||
                double.IsNaN(info.Revenue) ||
                double.IsInfinity(info.Revenue))
            {
                return;
            }

            try
            {
                var adEvent = new AdEventData(
                    "ad_revenue",
                    AdapterId,
                    info.NetworkName,
                    id,
                    info.AdFormat,
                    info.Placement,
                    info.NetworkPlacement,
                    "USD",
                    info.Revenue,
                    null);
                _analytics.TrackAd(adEvent);
            }
            catch (Exception exception)
            {
                WarnOnce($"MAX ad revenue dispatch failed: {exception.Message}");
            }
        }

        private void CompleteInterstitial(AdsShowStatus status, string error)
        {
            var completion = _interstitialCompletion;
            if (completion == null)
            {
                return;
            }

            _interstitialCompletion = null;
            completion.TrySetResult(Result(AdType.Interstitial, status, error));
            Notify(AdType.Interstitial);
        }

        private void CompleteRewarded(AdsShowStatus status, string error)
        {
            var completion = _rewardedCompletion;
            if (completion == null)
            {
                return;
            }

            _rewardedCompletion = null;
            completion.TrySetResult(Result(AdType.Rewarded, status, error));
            Notify(AdType.Rewarded);
        }

        private UniTask<AdsShowResult> CompletedResult(
            AdType type,
            AdsShowStatus status,
            string error)
        {
            return UniTask.FromResult(Result(type, status, error));
        }

        private AdsShowResult Result(AdType type, AdsShowStatus status, string error)
        {
            return new AdsShowResult(status, type, AdapterId, false, error, 0);
        }

        private void Notify(AdType type)
        {
            AvailabilityChanged?.Invoke(type, null);
        }

        private void FailInitialization(string message)
        {
            _available = false;
            _initializationAttempted = true;
            WarnOnce(message);
        }

        private void WarnOnce(string message)
        {
            if (_warningLogged)
            {
                return;
            }

            _warningLogged = true;
            EvoDebug.LogWarning(message, SOURCE);
        }

        private bool HasAnyAdUnit()
        {
            return HasValue(_config.InterstitialPlacementId) ||
                   HasValue(_config.RewardedPlacementId) ||
                   HasValue(_config.BannerPlacementId);
        }

        private static bool HasValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
