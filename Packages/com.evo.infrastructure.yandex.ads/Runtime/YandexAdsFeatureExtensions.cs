using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Ads.Adapters;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexAdsFeatureExtensions
    {
        public static EvoFeatureRegistry UseYandexAds(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<IAdsAdapterFactory, YandexGamesAdsAdapterFactory>(Lifetime.Singleton);
#endif
            return features;
        }
    }
}
