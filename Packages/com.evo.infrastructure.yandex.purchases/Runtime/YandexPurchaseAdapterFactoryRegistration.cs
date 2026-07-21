using Evo.Infrastructure.DI;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    internal static class YandexPurchaseAdapterFactoryRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("yandex_purchases", RegisterAdapterFactory);
        }

        private static void RegisterAdapterFactory(EvoFeatureRegistry features)
        {
            features.Builder.RegisterInstance(new YandexPurchasesOptions());
            features.Builder.Register<YandexPurchaseAdapterFactory>(Lifetime.Singleton)
                .As<IPurchaseAdapterFactory>();
        }
    }
}
