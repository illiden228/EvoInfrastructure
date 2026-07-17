using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Analytics.AppMetrica
{
    public static class AppMetricaAnalyticsFeatureExtensions
    {
        private const string ADAPTER_ID = "appmetrica";

        public static EvoFeatureRegistry UseAppMetricaAnalytics(this EvoFeatureRegistry features)
        {
            if (!EvoOptionalFeatureRegistry.TryRegister(features, ADAPTER_ID))
            {
                EvoDebug.LogWarning(
                    "AppMetrica adapter is unavailable. Install io.appmetrica.analytics and let Unity recompile.",
                    nameof(AppMetricaAnalyticsFeatureExtensions));
            }

            return features;
        }
    }
}
