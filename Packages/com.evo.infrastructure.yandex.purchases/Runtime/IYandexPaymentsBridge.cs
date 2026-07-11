using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    internal interface IYandexPaymentsBridge : IDisposable
    {
        bool IsAvailable { get; }
        IReadOnlyList<YandexStoreProduct> Products { get; }
        event Action CatalogReceived;
        event Action<string> PurchaseSucceeded;
        event Action<string> PurchaseFailed;
        void Buy(string storeProductId);
        bool Consume(string storeProductId);
    }

}
