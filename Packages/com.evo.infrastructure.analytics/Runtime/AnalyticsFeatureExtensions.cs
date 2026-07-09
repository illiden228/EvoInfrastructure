using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics
{
    public static class AnalyticsFeatureExtensions
    {
        public static EvoFeatureRegistry UseAnalytics(this EvoFeatureRegistry features)
        {
            features.Builder.Register<AnalyticsService>(Lifetime.Singleton)
                .As<IAnalyticsService>()
                .As<IAnalyticsInitialization>();
            return features;
        }
    }
}
