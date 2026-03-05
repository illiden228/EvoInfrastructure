using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Config;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Analytics.Config
{
    [CreateAssetMenu(fileName = "YandexGamesAnalyticsAdapterConfig", menuName = "Project/Analytics/Adapters/Yandex Games Config")]
    public sealed class YandexGamesAnalyticsAdapterConfig : AnalyticsAdapterConfigBase
    {
        [Header("Default SDK Events")]
        [SerializeField] private bool useDefaultPurchaseEvent;
        [SerializeField] private bool useDefaultAdEvent;
        [CatalogDropdown(CatalogDropdownKind.PlatformId)]
        [SerializeField] private List<string> purchaseDefaultPlatforms;
        [CatalogDropdown(CatalogDropdownKind.PlatformId)]
        [SerializeField] private List<string> adDefaultPlatforms;

        public bool UseDefaultPurchaseEvent => useDefaultPurchaseEvent;
        public bool UseDefaultAdEvent => useDefaultAdEvent;
        public IReadOnlyList<string> PurchaseDefaultPlatforms => purchaseDefaultPlatforms;
        public IReadOnlyList<string> AdDefaultPlatforms => adDefaultPlatforms;

    }
}
