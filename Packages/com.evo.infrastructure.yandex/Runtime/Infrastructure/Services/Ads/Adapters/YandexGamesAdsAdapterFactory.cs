using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Services.Ads.Adapters
{
    public sealed class YandexGamesAdsAdapterFactory : IAdsAdapterFactory
    {
        private const string DEFAULT_ADAPTER_ID = "yandex";

        private readonly IConfigService _configService;

        public YandexGamesAdsAdapterFactory(IConfigService configService)
        {
            _configService = configService;
        }

        public string AdapterId => DEFAULT_ADAPTER_ID;

        public IAdsAdapter Create()
        {
            return new YandexGamesAdsAdapter(_configService);
        }
    }
}
