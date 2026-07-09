namespace Evo.Infrastructure.Services.Ads
{
    public interface IAdsPreloadAdapter
    {
        void Preload(AdType adType, string placementId = null);
    }
}
