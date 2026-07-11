using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.Purchases;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    public static class UnityIapPurchaseFeatureExtensions
    {
        private const string FactoryTypeName =
            "Evo.Infrastructure.Services.Purchases.UnityIap.UnityIapPurchaseAdapterFactory, " +
            "Evo.Infrastructure.Purchases.UnityIap.Sdk";

        public static EvoFeatureRegistry UseUnityIapPurchases(this EvoFeatureRegistry features)
        {
            var factoryType = Type.GetType(FactoryTypeName, false);
            if (factoryType != null)
            {
                features.Builder.Register(factoryType, Lifetime.Singleton).As<IPurchaseAdapterFactory>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "Unity IAP adapter is unavailable. Install com.unity.purchasing 5.0 or newer and let Unity recompile.",
                    nameof(UnityIapPurchaseFeatureExtensions));
            }

            return features;
        }
    }
}
