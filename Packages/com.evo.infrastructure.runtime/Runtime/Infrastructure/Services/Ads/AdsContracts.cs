namespace _Project.Scripts.Infrastructure.Services.Ads
{
    public enum AdType
    {
        Interstitial = 0,
        Rewarded = 1,
        Banner = 2
    }

    public enum AdsShowStatus
    {
        Shown = 0,
        Failed = 1,
        Skipped = 2,
        Timeout = 3
    }

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

    public readonly struct AdsShowResult
    {
        public readonly AdsShowStatus Status;
        public readonly AdType AdType;
        public readonly string AdapterId;
        public readonly bool IsFallbackUsed;
        public readonly string Error;
        public readonly int DurationMs;

        public AdsShowResult(
            AdsShowStatus status,
            AdType adType,
            string adapterId,
            bool isFallbackUsed,
            string error,
            int durationMs)
        {
            Status = status;
            AdType = adType;
            AdapterId = adapterId;
            IsFallbackUsed = isFallbackUsed;
            Error = error;
            DurationMs = durationMs;
        }
    }

    public readonly struct AdsServiceOptions
    {
        public readonly int DefaultShowTimeoutMs;
        public readonly int MaxFallbackAttempts;

        public AdsServiceOptions(int defaultShowTimeoutMs = 8000, int maxFallbackAttempts = 1)
        {
            DefaultShowTimeoutMs = defaultShowTimeoutMs > 0 ? defaultShowTimeoutMs : 8000;
            MaxFallbackAttempts = maxFallbackAttempts < 0 ? 0 : maxFallbackAttempts;
        }
    }
}
