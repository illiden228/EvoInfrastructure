using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Debug;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    public static class RuStorePurchasesFeatureExtensions
    {
        private const string FactoryTypeName =
            "Evo.Infrastructure.Services.Purchases.RuStore.RuStorePurchaseAdapterFactory, " +
            "Evo.Infrastructure.Purchases.RuStore.Sdk";

        public static EvoFeatureRegistry UseRuStorePurchases(this EvoFeatureRegistry features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            var factoryType = Type.GetType(FactoryTypeName, false);
            if (factoryType != null)
            {
                features.Builder.Register(factoryType, Lifetime.Singleton).As<IPurchaseAdapterFactory>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "RuStore purchases are unavailable. Install a Unity-compatible RuStore Pay SDK " +
                    "package 'ru.rustore.pay' (10.2 or newer) and let Unity recompile.",
                    nameof(RuStorePurchasesFeatureExtensions));
            }

            return features;
        }
    }
}
