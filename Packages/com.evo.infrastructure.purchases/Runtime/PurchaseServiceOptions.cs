using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    public sealed class PurchaseServiceOptions
    {
        public bool AutoRestorePendingPurchases { get; set; } = true;
        public float InitializationTimeoutSeconds { get; set; } = 30f;
        public float PurchaseTimeoutSeconds { get; set; } = 120f;
        public float RestoreTimeoutSeconds { get; set; } = 30f;
    }
}

