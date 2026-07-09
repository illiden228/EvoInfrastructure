namespace Evo.Infrastructure.Services.Ads
{
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
