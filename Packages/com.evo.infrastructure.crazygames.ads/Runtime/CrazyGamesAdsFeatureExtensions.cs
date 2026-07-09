using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Ads.Adapters;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesAdsFeatureExtensions
    {
        public static EvoFeatureRegistry UseCrazyGamesAds(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IAdsAdapterFactory, CrazyGamesAdsAdapterFactory>(Lifetime.Singleton);
            return features;
        }
    }
}
