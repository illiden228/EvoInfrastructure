using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.Analytics;
using VContainer;

namespace Evo.Infrastructure.Services.Ads.AppLovin
{
    public static class AppLovinAdsFeatureExtensions
    {
        private const string FACTORY_TYPE =
            "Evo.Infrastructure.Services.Ads.AppLovin.AppLovinAdsAdapterFactory, " +
            "Evo.Infrastructure.Ads.AppLovin.Sdk";

        public static EvoFeatureRegistry UseAppLovinAds(this EvoFeatureRegistry features)
        {
            var type = Type.GetType(FACTORY_TYPE, false);
            if (type != null)
            {
                features.Builder.Register(type, Lifetime.Singleton)
                    .WithParameter<IAnalyticsService>(resolver =>
                        resolver.TryResolve<IAnalyticsService>(out var service) ? service : null)
                    .As<IAdsAdapterFactory>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "AppLovin adapter is unavailable. Install com.applovin.mediation.ads and let Unity recompile.",
                    nameof(AppLovinAdsFeatureExtensions));
            }

            return features;
        }
    }
}
