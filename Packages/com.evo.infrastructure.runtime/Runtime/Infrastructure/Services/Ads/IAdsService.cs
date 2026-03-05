using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Services.Ads
{
    public interface IAdsService
    {
        bool CanShow(AdType adType, string placementId = null);
        UniTask<AdsShowResult> ShowAsync(AdsShowRequest request, CancellationToken cancellationToken = default);
        void ShowBanner(string placementId = null);
        void HideBanner(string placementId = null);
    }
}
