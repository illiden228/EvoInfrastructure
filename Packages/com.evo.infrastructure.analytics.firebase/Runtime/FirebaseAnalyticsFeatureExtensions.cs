using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Debug;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.Firebase
{
    public static class FirebaseAnalyticsFeatureExtensions
    {
        private const string ADAPTER_TYPE =
            "Evo.Infrastructure.Services.Analytics.Firebase.FirebaseAnalyticsAdapter, " +
            "Evo.Infrastructure.Analytics.Firebase.Sdk";

        public static EvoFeatureRegistry UseFirebaseAnalytics(this EvoFeatureRegistry features)
        {
            var type = Type.GetType(ADAPTER_TYPE, false);
            if (type != null)
            {
                features.Builder.Register(type, Lifetime.Singleton).As<IAnalyticsAdapter>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "Firebase Analytics adapter is unavailable. Install Firebase App/Analytics and enable EVO_FIREBASE_ANALYTICS_SDK.",
                    nameof(FirebaseAnalyticsFeatureExtensions));
            }

            return features;
        }
    }
}
