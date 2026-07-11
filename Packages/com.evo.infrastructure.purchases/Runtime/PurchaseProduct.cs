using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases
{
    public sealed class PurchaseProduct
    {
        public PurchaseProduct(string id, string storeProductId, string fulfillmentKey,
            PurchaseProductType productType, bool enabled, IReadOnlyList<PurchaseGrantDefinition> grants)
        {
            Id = id;
            StoreProductId = storeProductId;
            FulfillmentKey = fulfillmentKey;
            ProductType = productType;
            IsEnabled = enabled;
            Grants = grants ?? Array.Empty<PurchaseGrantDefinition>();
        }

        public string Id { get; }
        public string StoreProductId { get; }
        public string FulfillmentKey { get; }
        public PurchaseProductType ProductType { get; }
        public bool IsEnabled { get; }
        public bool IsAvailable { get; internal set; }
        public string LocalizedTitle { get; internal set; }
        public string LocalizedDescription { get; internal set; }
        public string LocalizedPrice { get; internal set; }
        public decimal Price { get; internal set; }
        public string CurrencyCode { get; internal set; }
        public string ImageUrl { get; internal set; }
        public IReadOnlyList<PurchaseGrantDefinition> Grants { get; }
    }
}

