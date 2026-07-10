using Evo.Infrastructure.Services.Analytics.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Analytics.AppMetrica
{
    [CreateAssetMenu(fileName = "AppMetricaAnalyticsAdapterConfig", menuName = "Project/Analytics/Adapters/AppMetrica Config")]
    public sealed class AppMetricaAnalyticsAdapterConfig : AnalyticsAdapterConfigBase
    {
        [SerializeField] private bool enableSdkLogs;
        [SerializeField] private bool logReportedEvents;
        [SerializeField] private bool flushEventsImmediately;

        public bool EnableSdkLogs => enableSdkLogs;
        public bool LogReportedEvents => logReportedEvents;
        public bool FlushEventsImmediately => flushEventsImmediately;
    }
}
