using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [GameConfig("Purchases")]
    [CreateAssetMenu(fileName = "PurchaseCatalogConfig", menuName = "Project/Purchases/Catalog")]
    public sealed class PurchaseCatalogConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private List<PurchaseOfferDefinition> offers = new();
        public IReadOnlyList<PurchaseOfferDefinition> Offers => offers;
    }
}

