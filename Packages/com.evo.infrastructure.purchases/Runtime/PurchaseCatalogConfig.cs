using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;
using UnityEngine.Serialization;

namespace Evo.Infrastructure.Services.Purchases
{
    [GameConfig("Purchases")]
    [CreateAssetMenu(fileName = "PurchaseCatalogConfig", menuName = "Project/Purchases/Catalog")]
    public sealed class PurchaseCatalogConfig : ScriptableObject, IGameConfig
    {
        [FormerlySerializedAs("offers")]
        [SerializeField] private List<PurchaseProductDefinition> products = new();
        public IReadOnlyList<PurchaseProductDefinition> Products => products;
    }
}

