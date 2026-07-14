using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Purchases;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    public static class UnityIapPurchaseFeatureExtensions
    {
        public static EvoFeatureRegistry UseUnityIapPurchases(this EvoFeatureRegistry features)
        {
            features.Builder.Register<UnityIapPurchaseAdapterFactory>(Lifetime.Singleton)
                .As<IPurchaseAdapterFactory>();
            return features;
        }
    }
}
