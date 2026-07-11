using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseStoreProduct
    {
        public PurchaseStoreProduct(string storeProductId, bool available, string title = null,
            string description = null, string localizedPrice = null, decimal price = 0, string currencyCode = null,
            string imageUrl = null)
        {
            StoreProductId = storeProductId;
            IsAvailable = available;
            Title = title;
            Description = description;
            LocalizedPrice = localizedPrice;
            Price = price;
            CurrencyCode = currencyCode;
            ImageUrl = imageUrl;
        }
        public string StoreProductId { get; }
        public bool IsAvailable { get; }
        public string Title { get; }
        public string Description { get; }
        public string LocalizedPrice { get; }
        public decimal Price { get; }
        public string CurrencyCode { get; }
        public string ImageUrl { get; }
    }
}

