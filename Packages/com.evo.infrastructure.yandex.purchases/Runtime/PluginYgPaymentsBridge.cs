using System;
using System.Collections.Generic;
using System.Globalization;
#if Payments_yg
using YG;
#endif

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    internal sealed class PluginYgPaymentsBridge : IYandexPaymentsBridge
    {
        public PluginYgPaymentsBridge()
        {
#if Payments_yg
            YG2.onGetPayments += OnCatalogReceived;
            YG2.onPurchaseSuccess += OnPurchaseSucceeded;
            YG2.onPurchaseFailed += OnPurchaseFailed;
#endif
        }

        public bool IsAvailable
        {
            get
            {
#if Payments_yg
                return true;
#else
                return false;
#endif
            }
        }

        public IReadOnlyList<YandexStoreProduct> Products
        {
            get
            {
#if Payments_yg
                var purchases = YG2.purchases;
                if (purchases == null || purchases.Length == 0)
                {
                    return Array.Empty<YandexStoreProduct>();
                }

                var result = new List<YandexStoreProduct>(purchases.Length);
                for (var i = 0; i < purchases.Length; i++)
                {
                    var purchase = purchases[i];
                    if (purchase == null || string.IsNullOrWhiteSpace(purchase.id))
                    {
                        continue;
                    }

                    result.Add(new YandexStoreProduct(
                        purchase.id,
                        purchase.title,
                        purchase.description,
                        purchase.imageURI,
                        purchase.price,
                        ParseDecimal(purchase.priceValue),
                        purchase.priceCurrencyCode,
                        purchase.consumed));
                }

                return result;
#else
                return Array.Empty<YandexStoreProduct>();
#endif
            }
        }

        public event Action CatalogReceived;
        public event Action<string> PurchaseSucceeded;
        public event Action<string> PurchaseFailed;

        public void Buy(string storeProductId)
        {
#if Payments_yg
            YG2.BuyPayments(storeProductId);
#else
            throw new InvalidOperationException("PluginYG2 Payments is unavailable.");
#endif
        }

        public bool Consume(string storeProductId)
        {
#if Payments_yg
            YG2.ConsumePurchaseByID(storeProductId, false);
            return true;
#else
            return false;
#endif
        }

        public void Dispose()
        {
#if Payments_yg
            YG2.onGetPayments -= OnCatalogReceived;
            YG2.onPurchaseSuccess -= OnPurchaseSucceeded;
            YG2.onPurchaseFailed -= OnPurchaseFailed;
#endif
        }

        private void OnCatalogReceived() => CatalogReceived?.Invoke();
        private void OnPurchaseSucceeded(string id) => PurchaseSucceeded?.Invoke(id);
        private void OnPurchaseFailed(string id) => PurchaseFailed?.Invoke(id);

        private static decimal ParseDecimal(string value)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
        }
    }
}
