namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    public sealed class YandexPurchaseAdapterFactory : IPurchaseAdapterFactory
    {
        public const string DefaultAdapterId = "yandex";

        private readonly YandexPurchasesOptions _options;

        public YandexPurchaseAdapterFactory(YandexPurchasesOptions options)
        {
            _options = options ?? new YandexPurchasesOptions();
        }

        public string AdapterId => DefaultAdapterId;

        public IPurchaseAdapter Create()
        {
            return new YandexPurchaseAdapter(_options, new PluginYgPaymentsBridge());
        }
    }
}
