using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Ads.AppLovin
{
    internal static class AppLovinAdsAdapterFactoryRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("applovin", RegisterAdapterFactory);
        }

        private static void RegisterAdapterFactory(EvoFeatureRegistry features)
        {
            features.Builder.Register<AppLovinAdsAdapterFactory>(Lifetime.Singleton)
                .WithParameter<IAnalyticsService>(resolver =>
                    resolver.TryResolve<IAnalyticsService>(out var service) ? service : null)
                .As<IAdsAdapterFactory>();
        }
    }
}
