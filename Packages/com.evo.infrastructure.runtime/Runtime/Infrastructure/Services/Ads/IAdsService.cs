using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Evo.Infrastructure.Services.Ads
{
    public interface IAdsService
    {
        bool CanShow(AdType adType, string placementId = null);
        AdsAvailability GetAvailability(AdType adType, string placementId = null);
        Observable<AdsAvailability> ObserveAvailability(AdType adType, string placementId = null);
        void Preload(AdType adType, string placementId = null);
        UniTask<AdsShowResult> ShowAsync(AdsShowRequest request, CancellationToken cancellationToken = default);
        void ShowBanner(string placementId = null);
        void HideBanner(string placementId = null);
    }
}
