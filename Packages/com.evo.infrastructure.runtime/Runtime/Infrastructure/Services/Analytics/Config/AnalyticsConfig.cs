using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Analytics.Config
{
    [CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "Project/Analytics/Analytics Config")]
    [GameConfig("Analytics")]
    public sealed class AnalyticsConfig : ScriptableObject, IGameConfig
    {
        [Serializable]
        public struct AdapterBinding
        {
            [CatalogDropdown(CatalogDropdownKind.AnalyticsAdapterId)]
            public string AdapterId;
            public int Priority;

#if ODIN_INSPECTOR
            [ShowInInspector, ReadOnly, HideLabel]
#endif
            private string DisplayName => string.IsNullOrWhiteSpace(AdapterId) ? "<empty>" : AdapterId;

        }

        [Serializable]
        public struct PlatformAdapterBindings
        {
            [CatalogDropdown(CatalogDropdownKind.PlatformId)]
            public string PlatformId;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, ListElementLabelName = "DisplayName")]
#endif
            public List<AdapterBinding> AdapterBindings;

#if ODIN_INSPECTOR
            [ShowInInspector, ReadOnly, HideLabel]
#endif
            private string DisplayName => string.IsNullOrWhiteSpace(PlatformId) ? "<empty>" : PlatformId;

        }

        [SerializeField] private bool analyticsEnabled = true;
        [SerializeField] private bool logSkippedEvents = true;
#if ODIN_INSPECTOR
        [Title("Global Adapters")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, ListElementLabelName = "DisplayName")]
#endif
        [SerializeField] private List<AdapterBinding> adapterBindings = new();
#if ODIN_INSPECTOR
        [Title("Platform Overrides")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, ListElementLabelName = "DisplayName")]
#endif
        [SerializeField] private List<PlatformAdapterBindings> platformAdapterBindings = new();

        public bool AnalyticsEnabled => analyticsEnabled;
        public bool LogSkippedEvents => logSkippedEvents;
        public IReadOnlyList<AdapterBinding> AdapterBindings => adapterBindings;
        public IReadOnlyList<PlatformAdapterBindings> PlatformBindings => platformAdapterBindings;

        public int GetPriority(string adapterId, IReadOnlyList<AdapterBinding> bindings = null)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
            {
                return int.MaxValue;
            }

            var list = bindings ?? adapterBindings;
            for (var i = 0; i < list.Count; i++)
            {
                var binding = list[i];
                if (string.Equals(binding.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase))
                {
                    return binding.Priority;
                }
            }

            return int.MaxValue;
        }

        public void SortByPriority(List<IAnalyticsAdapter> adapters, IReadOnlyList<AdapterBinding> bindings = null)
        {
            if (adapters == null || adapters.Count < 2)
            {
                return;
            }

            adapters.Sort((left, right) =>
            {
                var leftPriority = GetPriority(left != null ? left.AdapterId : null, bindings);
                var rightPriority = GetPriority(right != null ? right.AdapterId : null, bindings);
                return leftPriority.CompareTo(rightPriority);
            });
        }

        public IReadOnlyList<AdapterBinding> ResolveBindingsForPlatform(string platformId)
        {
            if (string.IsNullOrWhiteSpace(platformId) || platformAdapterBindings.Count == 0)
            {
                return adapterBindings;
            }

            for (var i = 0; i < platformAdapterBindings.Count; i++)
            {
                var entry = platformAdapterBindings[i];
                if (!string.Equals(entry.PlatformId, platformId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.AdapterBindings != null && entry.AdapterBindings.Count > 0)
                {
                    return entry.AdapterBindings;
                }
            }

            return adapterBindings;
        }
    }

}
