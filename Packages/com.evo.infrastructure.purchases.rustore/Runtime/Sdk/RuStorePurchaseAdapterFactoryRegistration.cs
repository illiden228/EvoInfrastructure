using Evo.Infrastructure.DI;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    internal static class RuStorePurchaseAdapterFactoryRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("rustore", RegisterAdapterFactory);
        }

        private static void RegisterAdapterFactory(EvoFeatureRegistry features)
        {
            features.Builder.Register<RuStorePurchaseAdapterFactory>(Lifetime.Singleton)
                .As<IPurchaseAdapterFactory>();
        }
    }
}
