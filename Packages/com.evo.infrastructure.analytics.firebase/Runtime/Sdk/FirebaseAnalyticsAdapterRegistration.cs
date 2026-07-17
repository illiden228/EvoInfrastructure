using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.Firebase
{
    internal static class FirebaseAnalyticsAdapterRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("firebase", RegisterAdapter);
        }

        private static void RegisterAdapter(EvoFeatureRegistry features)
        {
            features.Builder.Register<FirebaseAnalyticsAdapter>(Lifetime.Singleton)
                .As<IAnalyticsAdapter>();
        }
    }
}
