using System;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseTransaction
    {
        public PurchaseTransaction(string transactionId, string offerId, string storeProductId, string adapterId,
            string receipt = null, string purchaseToken = null, string orderId = null, decimal price = 0,
            string currencyCode = null, DateTimeOffset purchaseTime = default, bool isRestored = false)
        {
            TransactionId = transactionId;
            OfferId = offerId;
            StoreProductId = storeProductId;
            AdapterId = adapterId;
            Receipt = receipt;
            PurchaseToken = purchaseToken;
            OrderId = orderId;
            Price = price;
            CurrencyCode = currencyCode;
            PurchaseTime = purchaseTime;
            IsRestored = isRestored;
        }
        public string TransactionId { get; }
        public string OfferId { get; }
        public string StoreProductId { get; }
        public string AdapterId { get; }
        public string Receipt { get; }
        public string PurchaseToken { get; }
        public string OrderId { get; }
        public decimal Price { get; }
        public string CurrencyCode { get; }
        public DateTimeOffset PurchaseTime { get; }
        public bool IsRestored { get; }
    }
}

