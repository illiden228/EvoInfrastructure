using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Debug;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.Adjust
{
    public static class AdjustAnalyticsFeatureExtensions
    {
        private const string ADAPTER_TYPE =
            "Evo.Infrastructure.Services.Analytics.Adjust.AdjustAnalyticsAdapter, " +
            "Evo.Infrastructure.Analytics.Adjust.Sdk";

        public static EvoFeatureRegistry UseAdjustAnalytics(this EvoFeatureRegistry features)
        {
            var type = Type.GetType(ADAPTER_TYPE, false);
            if (type != null)
            {
                features.Builder.Register(type, Lifetime.Singleton).As<IAnalyticsAdapter>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "Adjust adapter is unavailable. Install com.adjust.sdk and let Unity recompile.",
                    nameof(AdjustAnalyticsFeatureExtensions));
            }

            return features;
        }
    }
}
