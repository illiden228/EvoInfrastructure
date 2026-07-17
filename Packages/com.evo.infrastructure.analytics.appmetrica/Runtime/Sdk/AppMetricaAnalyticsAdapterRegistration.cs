using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.AppMetrica
{
    internal static class AppMetricaAnalyticsAdapterRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("appmetrica", RegisterAdapter);
        }

        private static void RegisterAdapter(EvoFeatureRegistry features)
        {
            features.Builder.Register<AppMetricaAnalyticsAdapter>(Lifetime.Singleton)
                .As<IAnalyticsAdapter>();
        }
    }
}
