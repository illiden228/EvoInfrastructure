using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Ads
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

    public interface IAdsPreloadAdapter
    {
        void Preload(AdType adType, string placementId = null);
    }

    public interface IAdsAvailabilityAdapter
    {
        bool IsLoading(AdType adType, string placementId = null);
    }

    public interface IAdsAvailabilityEvents
    {
        event System.Action<AdType, string> AvailabilityChanged;
    }
}
