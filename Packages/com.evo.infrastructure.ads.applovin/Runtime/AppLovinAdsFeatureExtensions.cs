using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Ads.AppLovin
{
    public static class AppLovinAdsFeatureExtensions
    {
        private const string FACTORY_ID = "applovin";

        public static EvoFeatureRegistry UseAppLovinAds(this EvoFeatureRegistry features)
        {
            if (!EvoOptionalFeatureRegistry.TryRegister(features, FACTORY_ID))
            {
                EvoDebug.LogWarning(
                    "AppLovin adapter is unavailable. Install com.applovin.mediation.ads and let Unity recompile.",
                    nameof(AppLovinAdsFeatureExtensions));
            }

            return features;
        }
    }
}
