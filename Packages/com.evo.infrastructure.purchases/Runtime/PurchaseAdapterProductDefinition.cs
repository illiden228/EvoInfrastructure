using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseAdapterProductDefinition
    {
        public PurchaseAdapterProductDefinition(string productId, string storeProductId, PurchaseProductType productType)
        {
            ProductId = productId;
            StoreProductId = storeProductId;
            ProductType = productType;
        }
        public string ProductId { get; }
        public string StoreProductId { get; }
        public PurchaseProductType ProductType { get; }
    }
}

