using Evo.Infrastructure.DI;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    internal static class UnityIapPurchaseAdapterFactoryRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("unity_iap", RegisterAdapterFactory);
        }

        private static void RegisterAdapterFactory(EvoFeatureRegistry features)
        {
            features.Builder.Register<UnityIapPurchaseAdapterFactory>(Lifetime.Singleton)
                .As<IPurchaseAdapterFactory>();
        }
    }
}
