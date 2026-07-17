using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Ads.Adapters;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesAdsFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("crazygames_ads", features => { UseCrazyGamesAds(features); });

        public static EvoFeatureRegistry UseCrazyGamesAds(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IAdsAdapterFactory, CrazyGamesAdsAdapterFactory>(Lifetime.Singleton);
            return features;
        }
    }
}
