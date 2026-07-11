using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseAdapterProductDefinition
    {
        public PurchaseAdapterProductDefinition(string offerId, string storeProductId, PurchaseProductType productType)
        {
            OfferId = offerId;
            StoreProductId = storeProductId;
            ProductType = productType;
        }
        public string OfferId { get; }
        public string StoreProductId { get; }
        public PurchaseProductType ProductType { get; }
    }
}

