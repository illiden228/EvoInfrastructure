using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.Adjust
{
    internal static class AdjustAnalyticsAdapterRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("adjust", RegisterAdapter);
        }

        private static void RegisterAdapter(EvoFeatureRegistry features)
        {
            features.Builder.Register<AdjustAnalyticsAdapter>(Lifetime.Singleton)
                .As<IAnalyticsAdapter>();
        }
    }
}
