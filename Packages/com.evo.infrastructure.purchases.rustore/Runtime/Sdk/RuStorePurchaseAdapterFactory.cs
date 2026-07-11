using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    public sealed class RuStorePurchaseAdapterFactory : IPurchaseAdapterFactory
    {
        private readonly IConfigService _configs;

        public RuStorePurchaseAdapterFactory(IConfigService configs) => _configs = configs;

        public string AdapterId => RuStorePurchaseAdapter.AdapterIdValue;
        public IPurchaseAdapter Create() => new RuStorePurchaseAdapter(_configs);
    }
}
