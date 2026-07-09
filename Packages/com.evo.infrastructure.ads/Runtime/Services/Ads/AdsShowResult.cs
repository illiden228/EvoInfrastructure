namespace Evo.Infrastructure.Services.Ads
{
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
}
