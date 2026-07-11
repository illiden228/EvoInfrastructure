using System;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseTransaction
    {
        public PurchaseTransaction(string transactionId, string productId, string storeProductId, string adapterId,
            string receipt = null, string purchaseToken = null, string orderId = null, decimal price = 0,
            string currencyCode = null, DateTimeOffset purchaseTime = default, bool isRestored = false)
        {
            TransactionId = transactionId;
            ProductId = productId;
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
        public string ProductId { get; }
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

