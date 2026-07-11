using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [GameConfig("Purchases")]
    [CreateAssetMenu(fileName = "PurchaseRoutingConfig", menuName = "Project/Purchases/Routing")]
    public sealed class PurchaseRoutingConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private List<PurchaseAdapterBinding> adapters = new();
        public IReadOnlyList<PurchaseAdapterBinding> Adapters => adapters;
    }
}

