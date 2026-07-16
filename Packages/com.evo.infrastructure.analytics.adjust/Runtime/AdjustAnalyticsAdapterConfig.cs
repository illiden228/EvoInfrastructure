using Evo.Infrastructure.Services.Analytics.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Analytics.Adjust
{
    public enum EvoAdjustEnvironment
    {
        Sandbox = 0,
        Production = 1
    }

    public enum EvoAdjustLogLevel
    {
        Verbose = 1,
        Debug = 2,
        Info = 3,
        Warn = 4,
        Error = 5,
        Assert = 6,
        Suppress = 7
    }

    [CreateAssetMenu(fileName = "AdjustAnalyticsAdapterConfig", menuName = "Project/Analytics/Adapters/Adjust Config")]
    public sealed class AdjustAnalyticsAdapterConfig : AnalyticsAdapterConfigBase
    {
        [SerializeField] private string purchaseToken;
        [SerializeField] private EvoAdjustEnvironment environment = EvoAdjustEnvironment.Production;
        [SerializeField] private EvoAdjustLogLevel logLevel = EvoAdjustLogLevel.Suppress;
        [SerializeField] private bool allowEditorTracking;

        public string PurchaseToken => purchaseToken;
        public EvoAdjustEnvironment Environment => environment;
        public EvoAdjustLogLevel LogLevel => logLevel;
        public bool AllowEditorTracking => allowEditorTracking;
    }
}
