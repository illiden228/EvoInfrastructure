using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.PlatformInfo;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics
{
    public static class AnalyticsFeatureExtensions
    {
        public static EvoFeatureRegistry UseAnalytics(this EvoFeatureRegistry features)
        {
            features.Builder.Register<AnalyticsService>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null)
                .WithParameter<IPlatformInfoService>(resolver =>
                    resolver.TryResolve<IPlatformInfoService>(out var service) ? service : null)
                .As<IAnalyticsService>()
                .As<IAnalyticsInitialization>();
            return features;
        }
    }
}
