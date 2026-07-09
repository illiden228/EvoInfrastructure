namespace Evo.Infrastructure.Services.Ads
{
    public interface IAdsAvailabilityAdapter
    {
        bool IsLoading(AdType adType, string placementId = null);
    }
}
