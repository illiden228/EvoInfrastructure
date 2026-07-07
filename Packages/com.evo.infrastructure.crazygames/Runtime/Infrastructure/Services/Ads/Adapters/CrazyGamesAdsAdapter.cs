using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.CrazyGames;
#if CRAZY
using CrazyGames;
#endif

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
#if CRAZY
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
                cancellationRegistration.Dispose();
                completion.TrySetResult(new AdsShowResult(status, request.AdType, AdapterId, false, error, 0));
            }

            try
            {
                var crazyAdType = request.AdType == AdType.Rewarded
                    ? CrazyAdType.Rewarded
                    : CrazyAdType.Midgame;
                CrazySDK.Ad.RequestAd(
                    crazyAdType,
                    adStarted: () => { },
                    adError: sdkError =>
                    {
                        var error = sdkError != null ? $"{sdkError.code}: {sdkError.message}" : "Unknown ad error.";
                        Finish(AdsShowStatus.Failed, error);
                    },
                    adFinished: () => Finish(AdsShowStatus.Shown));
            }
            catch (System.Exception ex)
            {
                Finish(AdsShowStatus.Failed, ex.Message);
            }

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() => Finish(AdsShowStatus.Skipped, "Canceled by token."));
            }

            return completion.Task;
#else
            return UniTask.FromResult(new AdsShowResult(
                AdsShowStatus.Skipped,
                request.AdType,
                AdapterId,
                false,
                "CRAZY define is disabled.",
                0));
#endif
        }
    }
}
