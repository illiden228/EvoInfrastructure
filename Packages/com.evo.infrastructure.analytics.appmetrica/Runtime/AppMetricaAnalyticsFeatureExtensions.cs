using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Debug;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.AppMetrica
{
    public static class AppMetricaAnalyticsFeatureExtensions
    {
        private const string ADAPTER_TYPE =
            "Evo.Infrastructure.Services.Analytics.AppMetrica.AppMetricaAnalyticsAdapter, " +
            "Evo.Infrastructure.Analytics.AppMetrica.Sdk";

        public static EvoFeatureRegistry UseAppMetricaAnalytics(this EvoFeatureRegistry features)
        {
            var type = Type.GetType(ADAPTER_TYPE, false);
            if (type != null)
            {
                features.Builder.Register(type, Lifetime.Singleton).As<IAnalyticsAdapter>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "AppMetrica adapter is unavailable. Install io.appmetrica.analytics and let Unity recompile.",
                    nameof(AppMetricaAnalyticsFeatureExtensions));
            }

            return features;
        }
    }
}
