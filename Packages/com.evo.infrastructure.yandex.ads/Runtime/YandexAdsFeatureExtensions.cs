using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Ads.Adapters;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexAdsFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("yandex_ads", features => { UseYandexAds(features); });

        public static EvoFeatureRegistry UseYandexAds(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<IAdsAdapterFactory, YandexGamesAdsAdapterFactory>(Lifetime.Singleton);
#endif
            return features;
        }
    }
}
