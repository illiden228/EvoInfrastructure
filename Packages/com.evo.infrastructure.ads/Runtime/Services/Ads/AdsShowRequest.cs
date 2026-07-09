namespace Evo.Infrastructure.Services.Ads
{
    public readonly struct AdsShowRequest
    {
        public readonly AdType AdType;
        public readonly string PlacementId;
        public readonly int? TimeoutMsOverride;

        public AdsShowRequest(AdType adType, string placementId = null, int? timeoutMsOverride = null)
        {
            AdType = adType;
            PlacementId = placementId;
            TimeoutMsOverride = timeoutMsOverride;
        }
    }
}
