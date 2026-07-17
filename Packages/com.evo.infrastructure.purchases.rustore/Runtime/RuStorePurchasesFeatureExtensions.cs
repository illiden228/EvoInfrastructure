using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    public static class RuStorePurchasesFeatureExtensions
    {
        private const string FactoryId = "rustore";

        public static EvoFeatureRegistry UseRuStorePurchases(this EvoFeatureRegistry features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            if (!EvoOptionalFeatureRegistry.TryRegister(features, FactoryId))
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
