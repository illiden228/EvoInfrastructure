using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Config;
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
            builder.Register<IAdsService, AdsService>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null)
                .WithParameter<IAnalyticsService>(resolver =>
                    resolver.TryResolve<IAnalyticsService>(out var service) ? service : null);
            builder.Register<RewardedAdsCooldownService>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null)
                .WithParameter<IRewardedAdsCooldownState>(resolver =>
                    resolver.TryResolve<IRewardedAdsCooldownState>(out var state) ? state : null);
            builder.Register<InterstitialAdsCooldownService>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null)
                .WithParameter<IInterstitialAdsCooldownState>(resolver =>
                    resolver.TryResolve<IInterstitialAdsCooldownState>(out var state) ? state : null);
            builder.Register<IAdsActivityNotifier, AdsActivityNotifier>(Lifetime.Singleton);
            return features;
        }
    }
}
