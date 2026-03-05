using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using _Project.Scripts.Infrastructure.Services.PlatformInfo.Config;
#endif

namespace _Project.Scripts.Infrastructure.Services.Analytics.Config
{
    [CreateAssetMenu(fileName = "AnalyticsEventMappingConfig", menuName = "Project/Analytics/Event Mapping Config")]
    [GameConfig("Analytics")]
    public sealed class AnalyticsEventMappingConfig : ScriptableObject, IGameConfig
    {
        [Serializable]
        public struct ParameterMapping
        {
            [CatalogDropdown(CatalogDropdownKind.AnalyticsParameterKey)]
            public string SourceKey;
            [CatalogDropdown(CatalogDropdownKind.AnalyticsParameterKey)]
            public string TargetKey;
        }

        [Serializable]
        public struct PlatformOverride
        {
            [CatalogDropdown(CatalogDropdownKind.PlatformId)]
            public string PlatformId;
            [CatalogDropdown(CatalogDropdownKind.AnalyticsEventKey)]
            public string EventKey;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
            public List<ParameterMapping> ParameterMappings;
        }

        [Serializable]
        public struct AdapterOverride
        {
            [CatalogDropdown(CatalogDropdownKind.AnalyticsAdapterId)]
            public string AdapterId;
            [CatalogDropdown(CatalogDropdownKind.AnalyticsEventKey)]
            public string EventKey;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
            public List<ParameterMapping> ParameterMappings;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
            public List<PlatformOverride> PlatformOverrides;
        }

        [Serializable]
        public struct EventMappingEntry
        {
            [CatalogDropdown(CatalogDropdownKind.AnalyticsEventKey)]
            public string CanonicalEventKey;
            [CatalogDropdown(CatalogDropdownKind.AnalyticsEventKey)]
            public string DefaultEventKey;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
            public List<ParameterMapping> ParameterMappings;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
            public List<PlatformOverride> PlatformOverrides;
#if ODIN_INSPECTOR
            [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
#endif
            public List<AdapterOverride> AdapterOverrides;
        }

#if ODIN_INSPECTOR
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, ListElementLabelName = "CanonicalEventKey")]
#endif
        [SerializeField] private List<EventMappingEntry> eventMappings = new();

        public IReadOnlyList<EventMappingEntry> EventMappings => eventMappings;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateAgainstCatalog();
        }

        private void ValidateAgainstCatalog()
        {
            var catalog = FindCatalog();
            if (catalog == null)
            {
                UnityEngine.Debug.LogWarning("AnalyticsEventMappingConfig: AnalyticsEventCatalog not found. Validation skipped.", this);
                return;
            }

            var eventKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parameterKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var platformIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                if (entry.Kind == AnalyticsKeyKind.Event)
                {
                    eventKeys.Add(entry.Key);
                }
                else
                {
                    parameterKeys.Add(entry.Key);
                }
            }

            var platformCatalog = FindPlatformCatalog();
            if (platformCatalog != null)
            {
                var platformEntries = platformCatalog.Entries;
                for (var i = 0; i < platformEntries.Count; i++)
                {
                    var entry = platformEntries[i];
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.PlatformId))
                    {
                        platformIds.Add(entry.PlatformId);
                    }
                }
            }

            for (var i = 0; i < eventMappings.Count; i++)
            {
                var mapping = eventMappings[i];
                ValidateEventKey(eventKeys, mapping.CanonicalEventKey, $"CanonicalEventKey at index {i}");
                ValidateEventKey(eventKeys, mapping.DefaultEventKey, $"DefaultEventKey at index {i}");
                ValidateParameterMappings(parameterKeys, mapping.ParameterMappings, $"ParameterMappings at index {i}");
                ValidatePlatformOverrides(eventKeys, parameterKeys, platformIds, mapping.PlatformOverrides, $"PlatformOverrides at index {i}");
                ValidateAdapterOverrides(eventKeys, parameterKeys, mapping.AdapterOverrides, $"AdapterOverrides at index {i}");
            }
        }

        private AnalyticsEventCatalog FindCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:AnalyticsEventCatalog");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            if (guids.Length > 1)
            {
                UnityEngine.Debug.LogWarning("AnalyticsEventMappingConfig: multiple AnalyticsEventCatalog assets found. Using the first one.", this);
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AnalyticsEventCatalog>(path);
        }

        private static AnalyticsEventCatalog FindCatalogStatic()
        {
            var guids = AssetDatabase.FindAssets("t:AnalyticsEventCatalog");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AnalyticsEventCatalog>(path);
        }

        private PlatformCatalog FindPlatformCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:PlatformCatalog");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PlatformCatalog>(path);
        }


        private void ValidateEventKey(HashSet<string> allowed, string key, string label)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!allowed.Contains(key))
            {
                UnityEngine.Debug.LogWarning($"AnalyticsEventMappingConfig: {label} uses unknown key '{key}'. Add it to AnalyticsEventCatalog.", this);
            }
        }

        private void ValidateParameterMappings(
            HashSet<string> allowed,
            List<ParameterMapping> mappings,
            string label)
        {
            if (mappings == null || mappings.Count == 0)
            {
                return;
            }

            for (var i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                ValidateParameterKey(allowed, mapping.SourceKey, $"{label} SourceKey at {i}");
                ValidateParameterKey(allowed, mapping.TargetKey, $"{label} TargetKey at {i}");
            }
        }

        private void ValidatePlatformOverrides(
            HashSet<string> eventKeys,
            HashSet<string> parameterKeys,
            HashSet<string> platformIds,
            List<PlatformOverride> overrides,
            string label)
        {
            if (overrides == null || overrides.Count == 0)
            {
                return;
            }

            for (var i = 0; i < overrides.Count; i++)
            {
                var entry = overrides[i];
                ValidatePlatformId(platformIds, entry.PlatformId, $"{label} PlatformId at {i}");
                ValidateEventKey(eventKeys, entry.EventKey, $"{label} EventKey at {i}");
                ValidateParameterMappings(parameterKeys, entry.ParameterMappings, $"{label} ParameterMappings at {i}");
            }
        }

        private void ValidateAdapterOverrides(
            HashSet<string> eventKeys,
            HashSet<string> parameterKeys,
            List<AdapterOverride> overrides,
            string label)
        {
            if (overrides == null || overrides.Count == 0)
            {
                return;
            }

            for (var i = 0; i < overrides.Count; i++)
            {
                var entry = overrides[i];
                ValidateEventKey(eventKeys, entry.EventKey, $"{label} EventKey at {i}");
                ValidateParameterMappings(parameterKeys, entry.ParameterMappings, $"{label} ParameterMappings at {i}");
                ValidatePlatformOverrides(eventKeys, parameterKeys, null, entry.PlatformOverrides, $"{label} PlatformOverrides at {i}");
            }
        }

        private void ValidateParameterKey(HashSet<string> allowed, string key, string label)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!allowed.Contains(key))
            {
                UnityEngine.Debug.LogWarning($"AnalyticsEventMappingConfig: {label} uses unknown key '{key}'. Add it to AnalyticsEventCatalog.", this);
            }
        }

        private void ValidatePlatformId(HashSet<string> allowed, string platformId, string label)
        {
            if (string.IsNullOrWhiteSpace(platformId) || allowed == null || allowed.Count == 0)
            {
                return;
            }

            if (!allowed.Contains(platformId))
            {
                UnityEngine.Debug.LogWarning($"AnalyticsEventMappingConfig: {label} uses unknown platform '{platformId}'. Add it to PlatformCatalog.", this);
            }
        }
#endif
    }

}
