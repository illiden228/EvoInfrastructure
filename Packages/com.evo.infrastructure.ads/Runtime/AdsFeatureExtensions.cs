using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Ads;
using VContainer;

namespace Evo.Infrastructure.Services.Ads
{
    public static class AdsFeatureExtensions
    {
        public static EvoFeatureRegistry UseAds(
            this EvoFeatureRegistry features,
            AdsServiceOptions options = default)
        {
            var builder = features.Builder;
            builder.RegisterInstance(options.Equals(default(AdsServiceOptions))
                ? new AdsServiceOptions()
                : options);
            builder.Register<IAdsService, AdsService>(Lifetime.Singleton);
            builder.Register<RewardedAdsCooldownService>(Lifetime.Singleton);
            builder.Register<InterstitialAdsCooldownService>(Lifetime.Singleton);
            builder.Register<IAdsActivityNotifier, AdsActivityNotifier>(Lifetime.Singleton);
            return features;
        }
    }
}
