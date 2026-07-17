using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Analytics.Adjust
{
    public static class AdjustAnalyticsFeatureExtensions
    {
        private const string ADAPTER_ID = "adjust";

        public static EvoFeatureRegistry UseAdjustAnalytics(this EvoFeatureRegistry features)
        {
            if (!EvoOptionalFeatureRegistry.TryRegister(features, ADAPTER_ID))
            {
                EvoDebug.LogWarning(
                    "Adjust adapter is unavailable. Install com.adjust.sdk and let Unity recompile.",
                    nameof(AdjustAnalyticsFeatureExtensions));
            }

            return features;
        }
    }
}
