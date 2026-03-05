using System;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.Ads.Config;
using _Project.Scripts.Infrastructure.Services.Config;
using _Project.Scripts.Infrastructure.Services.Debug;
using Cysharp.Threading.Tasks;
#if YandexGamesPlatform_yg
using YG;
#endif

namespace _Project.Scripts.Infrastructure.Services.Ads.Adapters
{
    public sealed class YandexGamesAdsAdapter : IAdsAdapter
    {
        private const string DEFAULT_ADAPTER_ID = "yandex";
        private const string YANDEX_ADS_ADAPTER_SOURCE = nameof(YandexGamesAdsAdapter);

        private readonly string _adapterId;
        private readonly string _interstitialPlacementId;
        private readonly string _rewardedPlacementId;

        public YandexGamesAdsAdapter(IConfigService configService = null)
        {
            _adapterId = DEFAULT_ADAPTER_ID;
            if (TryGetConfig(configService, out var config))
            {
                if (!string.IsNullOrWhiteSpace(config.AdapterId))
                {
                    _adapterId = config.AdapterId;
                }

                _interstitialPlacementId = config.InterstitialPlacementId;
                _rewardedPlacementId = config.RewardedPlacementId;
            }
        }

        public string AdapterId => _adapterId;
        public int Priority => 10;
        public bool IsInitialized
        {
            get
            {
#if YandexGamesPlatform_yg
                return YG2.isSDKEnabled;
#else
                return false;
#endif
            }
        }

        public bool IsReady(AdType adType, string placementId = null)
        {
            if (!IsInitialized)
            {
                return false;
            }

            return adType switch
            {
#if InterstitialAdv_yg
                AdType.Interstitial => true,
#endif
#if RewardedAdv_yg
                AdType.Rewarded => true,
#endif
#if StickyAdv_yg
                AdType.Banner => true,
#endif
                _ => false
            };
        }

        public UniTask<AdsShowResult> ShowAsync(AdsShowRequest request, CancellationToken cancellationToken = default)
        {
            return request.AdType switch
            {
                AdType.Interstitial => ShowInterstitialAsync(request, cancellationToken),
                AdType.Rewarded => ShowRewardedAsync(request, cancellationToken),
                _ => UniTask.FromResult(new AdsShowResult(
                    AdsShowStatus.Skipped,
                    request.AdType,
                    AdapterId,
                    false,
                    $"AdType '{request.AdType}' is not supported in ShowAsync.",
                    0))
            };
        }

        public void ShowBanner(string placementId = null)
        {
#if StickyAdv_yg
            if (IsInitialized)
            {
                YG2.StickyAdActivity(true);
            }
#endif
        }

        public void HideBanner(string placementId = null)
        {
#if StickyAdv_yg
            if (IsInitialized)
            {
                YG2.StickyAdActivity(false);
            }
#endif
        }

        private UniTask<AdsShowResult> ShowInterstitialAsync(AdsShowRequest request, CancellationToken cancellationToken)
        {
#if InterstitialAdv_yg
            if (!IsReady(AdType.Interstitial, request.PlacementId))
            {
                return UniTask.FromResult(new AdsShowResult(
                    AdsShowStatus.Skipped,
                    AdType.Interstitial,
                    AdapterId,
                    false,
                    "Interstitial is not ready.",
                    0));
            }

            var completion = new UniTaskCompletionSource<AdsShowResult>();
            var completed = false;
            CancellationTokenRegistration cancellationRegistration = default;

            void Finish(AdsShowStatus status, string error = null)
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                YG2.onCloseInterAdv -= OnClosed;
                YG2.onErrorInterAdv -= OnError;
                YG2.onAdvNotification -= OnAdvNotification;
                cancellationRegistration.Dispose();
                completion.TrySetResult(new AdsShowResult(status, AdType.Interstitial, AdapterId, false, error, 0));
            }

            void OnAdvNotification()
            {
                EvoDebug.Log("YG ad notification: ad opened.", YANDEX_ADS_ADAPTER_SOURCE);
            }

            void OnClosed()
            {
                EvoDebug.Log("YG interstitial closed.", YANDEX_ADS_ADAPTER_SOURCE);
                Finish(AdsShowStatus.Shown);
            }
            void OnError() => Finish(AdsShowStatus.Failed, "YG interstitial error callback.");

            YG2.onCloseInterAdv += OnClosed;
            YG2.onErrorInterAdv += OnError;
            YG2.onAdvNotification += OnAdvNotification;

            var placement = string.IsNullOrWhiteSpace(request.PlacementId) ? _interstitialPlacementId : request.PlacementId;
            _ = placement; // placement mapping is reserved for analytics/context in YG flow
            YG2.InterstitialAdvShow();
            EvoDebug.Log("YG interstitial show requested.", YANDEX_ADS_ADAPTER_SOURCE);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() => Finish(AdsShowStatus.Skipped, "Canceled by token."));
            }

            return completion.Task;
#else
            return UniTask.FromResult(new AdsShowResult(
                AdsShowStatus.Skipped,
                AdType.Interstitial,
                AdapterId,
                false,
                "InterstitialAdv_yg define is disabled.",
                0));
#endif
        }

        private UniTask<AdsShowResult> ShowRewardedAsync(AdsShowRequest request, CancellationToken cancellationToken)
        {
#if RewardedAdv_yg
            if (!IsReady(AdType.Rewarded, request.PlacementId))
            {
                return UniTask.FromResult(new AdsShowResult(
                    AdsShowStatus.Skipped,
                    AdType.Rewarded,
                    AdapterId,
                    false,
                    "Rewarded is not ready.",
                    0));
            }

            var completion = new UniTaskCompletionSource<AdsShowResult>();
            var completed = false;
            var rewardReceived = false;
            CancellationTokenRegistration cancellationRegistration = default;

            void Finish(AdsShowStatus status, string error = null)
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                YG2.onCloseRewardedAdv -= OnClosed;
                YG2.onRewardAdv -= OnReward;
                YG2.onErrorRewardedAdv -= OnError;
                YG2.onAdvNotification -= OnAdvNotification;
                cancellationRegistration.Dispose();
                completion.TrySetResult(new AdsShowResult(status, AdType.Rewarded, AdapterId, false, error, 0));
            }

            void OnAdvNotification()
            {
                EvoDebug.Log("YG ad notification: ad opened.", YANDEX_ADS_ADAPTER_SOURCE);
            }

            void OnClosed()
            {
                if (!rewardReceived)
                {
                    Finish(AdsShowStatus.Failed, "Rewarded closed without reward.");
                }
            }

            void OnReward(string rewardId)
            {
                EvoDebug.Log("YG rewarded granted.", YANDEX_ADS_ADAPTER_SOURCE);
                rewardReceived = true;
                Finish(AdsShowStatus.Shown);
            }

            void OnError() => Finish(AdsShowStatus.Failed, "YG rewarded error callback.");

            YG2.onCloseRewardedAdv += OnClosed;
            YG2.onRewardAdv += OnReward;
            YG2.onErrorRewardedAdv += OnError;
            YG2.onAdvNotification += OnAdvNotification;

            var rewardPlacement = string.IsNullOrWhiteSpace(request.PlacementId) ? _rewardedPlacementId : request.PlacementId;
            if (string.IsNullOrWhiteSpace(rewardPlacement))
            {
                rewardPlacement = "rewarded";
            }

            YG2.RewardedAdvShow(rewardPlacement);
            EvoDebug.Log("YG rewarded show requested.", YANDEX_ADS_ADAPTER_SOURCE);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() => Finish(AdsShowStatus.Skipped, "Canceled by token."));
            }

            return completion.Task;
#else
            return UniTask.FromResult(new AdsShowResult(
                AdsShowStatus.Skipped,
                AdType.Rewarded,
                AdapterId,
                false,
                "RewardedAdv_yg define is disabled.",
                0));
#endif
        }

        private static bool TryGetConfig(IConfigService configService, out YandexAdsAdapterConfig config)
        {
            if (configService != null &&
                configService.TryGet<AdsAdapterCatalog>(out var catalog) &&
                catalog != null &&
                catalog.TryGet(out config))
            {
                return true;
            }

            config = null;
            return false;
        }
    }
}
