using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Purchases;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    public static class YandexPurchasesFeatureExtensions
    {
        public static EvoFeatureRegistry UseYandexPurchases(
            this EvoFeatureRegistry features,
            YandexPurchasesOptions options = null)
        {
            features.Builder.RegisterInstance(options ?? new YandexPurchasesOptions());
            features.Builder.Register<YandexPurchaseAdapterFactory>(Lifetime.Singleton)
                .As<IPurchaseAdapterFactory>();
            return features;
        }
    }
}
