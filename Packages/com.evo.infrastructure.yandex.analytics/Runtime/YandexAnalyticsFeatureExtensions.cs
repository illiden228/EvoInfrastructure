using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Analytics.Adapters;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexAnalyticsFeatureExtensions
    {
        public static EvoFeatureRegistry UseYandexAnalytics(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<IAnalyticsAdapter, YandexGamesAnalyticsAdapter>(Lifetime.Singleton);
#endif
            return features;
        }
    }
}
