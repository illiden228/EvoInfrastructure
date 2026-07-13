using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [Serializable]
    public sealed class PurchaseProductDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private bool enabled = true;
        [SerializeField] private PurchaseProductType productType;
        [InspectorName("Fulfillment Handler Key (Optional)")]
        [Tooltip("Selects the IPurchaseFulfillmentHandler that grants this product. Empty uses the logical product ID.")]
        [SerializeField] private string fulfillmentKey = string.Empty;
        [SerializeField] private string defaultStoreProductId = string.Empty;
        [SerializeField] private List<PurchaseGrantDefinition> grants = new();
        [SerializeField] private List<PurchaseTargetOverride> overrides = new();

        public string Id => id?.Trim() ?? string.Empty;
        public bool Enabled => enabled;
        public PurchaseProductType ProductType => productType;
        public string FulfillmentKey => string.IsNullOrWhiteSpace(fulfillmentKey) ? Id : fulfillmentKey.Trim();
        public string DefaultStoreProductId => defaultStoreProductId?.Trim() ?? string.Empty;
        public IReadOnlyList<PurchaseGrantDefinition> Grants => grants;
        public IReadOnlyList<PurchaseTargetOverride> Overrides => overrides;
    }
}

