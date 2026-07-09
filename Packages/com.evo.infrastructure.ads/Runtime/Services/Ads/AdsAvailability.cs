namespace Evo.Infrastructure.Services.Ads
{
    public readonly struct AdsAvailability
    {
        public readonly AdType AdType;
        public readonly string PlacementId;
        public readonly bool IsConfigured;
        public readonly bool IsReady;
        public readonly bool IsLoading;
        public readonly bool IsShowing;
        public readonly string AdapterId;
        public readonly string Error;

        public bool CanShow => IsConfigured && IsReady && !IsShowing;

        public AdsAvailability(
            AdType adType,
            string placementId,
            bool isConfigured,
            bool isReady,
            bool isLoading,
            bool isShowing,
            string adapterId,
            string error = null)
        {
            AdType = adType;
            PlacementId = placementId;
            IsConfigured = isConfigured;
            IsReady = isReady;
            IsLoading = isLoading;
            IsShowing = isShowing;
            AdapterId = adapterId;
            Error = error;
        }
    }
}
