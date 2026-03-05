using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Services.Ads
{
    public interface IAdsAdapter
    {
        string AdapterId { get; }
        int Priority { get; }
        bool IsInitialized { get; }
        bool IsReady(AdType adType, string placementId = null);
        UniTask<AdsShowResult> ShowAsync(AdsShowRequest request, CancellationToken cancellationToken = default);
        void ShowBanner(string placementId = null);
        void HideBanner(string placementId = null);
    }
}
