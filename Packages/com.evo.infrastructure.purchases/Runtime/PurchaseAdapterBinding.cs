using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [Serializable]
    public sealed class PurchaseAdapterBinding
    {
        [SerializeField] private string adapterId = string.Empty;
        [SerializeField] private bool enabled = true;
        [SerializeField] private PurchasePlatformMask platforms = PurchasePlatformMask.All;
        [SerializeField] private int priority;
        public string AdapterId => adapterId?.Trim() ?? string.Empty;
        public bool Enabled => enabled;
        public PurchasePlatformMask Platforms => platforms;
        public int Priority => priority;
    }
}

