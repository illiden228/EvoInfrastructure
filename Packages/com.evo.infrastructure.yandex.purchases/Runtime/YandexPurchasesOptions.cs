using System;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    public sealed class YandexPurchasesOptions
    {
        public TimeSpan CatalogTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan PurchaseTimeout { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan ConsumeTimeout { get; set; } = TimeSpan.FromSeconds(15);
    }
}
