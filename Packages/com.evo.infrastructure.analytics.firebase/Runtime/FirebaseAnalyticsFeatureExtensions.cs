using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Analytics.Firebase
{
    public static class FirebaseAnalyticsFeatureExtensions
    {
        private const string ADAPTER_ID = "firebase";

        public static EvoFeatureRegistry UseFirebaseAnalytics(this EvoFeatureRegistry features)
        {
            if (!EvoOptionalFeatureRegistry.TryRegister(features, ADAPTER_ID))
            {
                EvoDebug.LogWarning(
                    "Firebase Analytics adapter is unavailable. Install Firebase App/Analytics and enable EVO_FIREBASE_ANALYTICS_SDK.",
                    nameof(FirebaseAnalyticsFeatureExtensions));
            }

            return features;
        }
    }
}
