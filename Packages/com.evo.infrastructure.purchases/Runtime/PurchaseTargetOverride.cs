using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [Serializable]
    public sealed class PurchaseTargetOverride
    {
        [SerializeField] private string adapterId = string.Empty;
        [SerializeField] private PurchasePlatformMask platforms = PurchasePlatformMask.All;
        [SerializeField] private int priority;
        [SerializeField] private string storeProductId = string.Empty;
        [SerializeField] private bool overrideEnabled;
        [SerializeField] private bool enabled = true;
        [SerializeField] private bool overrideProductType;
        [SerializeField] private PurchaseProductType productType;
        [SerializeField] private bool overrideGrants;
        [SerializeField] private List<PurchaseGrantDefinition> grants = new();

        public string AdapterId => adapterId?.Trim() ?? string.Empty;
        public PurchasePlatformMask Platforms => platforms;
        public int Priority => priority;
        public string StoreProductId => storeProductId?.Trim() ?? string.Empty;
        public bool OverrideEnabled => overrideEnabled;
        public bool Enabled => enabled;
        public bool OverrideProductType => overrideProductType;
        public PurchaseProductType ProductType => productType;
        public bool OverrideGrants => overrideGrants;
        public IReadOnlyList<PurchaseGrantDefinition> Grants => grants;
    }
}

