using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Services.Ads.AppLovin
{
    public sealed class AppLovinAdsAdapterFactory : IAdsAdapterFactory
    {
        public const string ID = "applovin";
        private readonly IConfigService _configs;
        private readonly IAnalyticsService _analytics;
        public AppLovinAdsAdapterFactory(IConfigService configs, IAnalyticsService analytics = null)
        {
            _configs = configs;
            _analytics = analytics;
        }

        public string AdapterId => ID;

        public IAdsAdapter Create() => new AppLovinAdsAdapter(_configs, _analytics);
    }
}
