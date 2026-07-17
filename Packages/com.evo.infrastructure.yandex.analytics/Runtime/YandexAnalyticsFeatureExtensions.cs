using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Analytics.Adapters;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.PlatformInfo;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexAnalyticsFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("yandex_analytics", features => { UseYandexAnalytics(features); });

        public static EvoFeatureRegistry UseYandexAnalytics(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<IAnalyticsAdapter, YandexGamesAnalyticsAdapter>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null)
                .WithParameter<IPlatformInfoService>(resolver =>
                    resolver.TryResolve<IPlatformInfoService>(out var service) ? service : null);
#endif
            return features;
        }
    }
}
