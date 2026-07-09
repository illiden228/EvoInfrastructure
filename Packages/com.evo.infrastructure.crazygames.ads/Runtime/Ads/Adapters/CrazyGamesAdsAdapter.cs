using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.CrazyGames;

namespace Evo.Infrastructure.Services.Ads.Adapters
{
    public sealed class CrazyGamesAdsAdapter : IAdsAdapter
    {
        private const string DEFAULT_ADAPTER_ID = "crazy";

        public string AdapterId => DEFAULT_ADAPTER_ID;
        public int Priority => 10;
        public bool IsInitialized => CrazyGamesSdk.IsInitialized;

        public bool IsReady(AdType adType, string placementId = null)
        {
            if (!CrazyGamesSdk.TryEnsureReady())
            {
                return false;
            }

            return adType == AdType.Interstitial || adType == AdType.Rewarded;
        }

        public UniTask<AdsShowResult> ShowAsync(AdsShowRequest request, CancellationToken cancellationToken = default)
        {
            return request.AdType switch
            {
                AdType.Interstitial => ShowCrazyAdAsync(request, cancellationToken),
                AdType.Rewarded => ShowCrazyAdAsync(request, cancellationToken),
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
        }

        public void HideBanner(string placementId = null)
        {
        }

        private UniTask<AdsShowResult> ShowCrazyAdAsync(AdsShowRequest request, CancellationToken cancellationToken)
        {
            if (!CrazyGamesSdk.TryEnsureReady())
            {
                return UniTask.FromResult(new AdsShowResult(
                    AdsShowStatus.Skipped,
                    request.AdType,
                    AdapterId,
                    false,
                    "CrazySDK is not initialized.",
                    0));
            }

            return ShowCrazyAdInternalAsync(request, cancellationToken);
        }

        private async UniTask<AdsShowResult> ShowCrazyAdInternalAsync(AdsShowRequest request, CancellationToken cancellationToken)
        {
            var result = request.AdType == AdType.Rewarded
                ? await CrazyGamesSdk.ShowRewardedAdAsync(cancellationToken)
                : await CrazyGamesSdk.ShowInterstitialAdAsync(cancellationToken);

            return new AdsShowResult(
                result.Shown ? AdsShowStatus.Shown : AdsShowStatus.Failed,
                request.AdType,
                AdapterId,
                false,
                result.Error,
                0);
        }
    }
}
