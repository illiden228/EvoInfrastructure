using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Ads.Config
{
    [CreateAssetMenu(fileName = "AdsRoutingConfig", menuName = "Project/Ads/Ads Routing Config")]
    [GameConfig("Ads")]
    public sealed class AdsRoutingConfig : ScriptableObject, IGameConfig
    {
        [Serializable]
        public struct AdapterBinding
        {
            [CatalogDropdown(CatalogDropdownKind.AdsAdapterId)]
            public string AdapterId;
            public int Priority;
        }

        [Serializable]
        public struct PlatformPrimaryAdapter
        {
            [CatalogDropdown(CatalogDropdownKind.PlatformId)]
            public string PlatformId;
            [CatalogDropdown(CatalogDropdownKind.AdsAdapterId)]
            public string AdapterId;
        }

        [Header("Platform Primary Adapter")]
        [CatalogDropdown(CatalogDropdownKind.AdsAdapterId)]
        [SerializeField] private string defaultAdapterId = "applovin";
#if ODIN_INSPECTOR
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
        [SerializeField] private List<PlatformPrimaryAdapter> platformPrimary = new()
        {
            new PlatformPrimaryAdapter { PlatformId = "yandex", AdapterId = "yandex" },
            new PlatformPrimaryAdapter { PlatformId = "amazon", AdapterId = "appodeal" }
        };

        [Header("Adapter Bindings")]
#if ODIN_INSPECTOR
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
        [SerializeField] private List<AdapterBinding> bindings = new()
        {
            new AdapterBinding { AdapterId = "applovin", Priority = 0 },
            new AdapterBinding { AdapterId = "yandex", Priority = 10 },
            new AdapterBinding { AdapterId = "appodeal", Priority = 20 }
        };

        public string GetPrimaryAdapterId(string platformId)
        {
            for (var i = 0; i < platformPrimary.Count; i++)
            {
                if (string.Equals(platformPrimary[i].PlatformId, platformId, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(platformPrimary[i].AdapterId))
                {
                    return platformPrimary[i].AdapterId;
                }
            }

            return defaultAdapterId;
        }

        public IReadOnlyList<string> BuildAdapterOrder(string platformId)
        {
            var result = new List<string>();
            var primaryId = GetPrimaryAdapterId(platformId);
            AddIfValid(result, primaryId);

            var orderedFallback = bindings
                .OrderBy(x => x.Priority);

            foreach (var binding in orderedFallback)
            {
                AddIfValid(result, binding.AdapterId);
            }

            return result;
        }

        private static void AddIfValid(List<string> result, string adapterId)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
            {
                return;
            }

            for (var i = 0; i < result.Count; i++)
            {
                if (string.Equals(result[i], adapterId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            result.Add(adapterId);
        }

    }
}
