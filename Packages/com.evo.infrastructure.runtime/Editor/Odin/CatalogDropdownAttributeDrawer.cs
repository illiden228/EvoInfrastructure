using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Analytics.Config;
using _Project.Scripts.Infrastructure.Services.Ads.Config;
using _Project.Scripts.Infrastructure.Services.Config;
using _Project.Scripts.Infrastructure.Services.PlatformInfo.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor.Odin
{
    public sealed class CatalogDropdownAttributeDrawer : OdinAttributeDrawer<CatalogDropdownAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var items = CatalogDropdownData.BuildItems(Attribute.Kind, ValueEntry.SmartValue);
            var currentValue = ValueEntry.SmartValue ?? string.Empty;

            var index = CatalogDropdownData.FindIndex(items, currentValue);
            var labels = CatalogDropdownData.BuildLabels(items);

            EditorGUI.BeginChangeCheck();
            var nextIndex = EditorGUILayout.Popup(label, index, labels);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = items[nextIndex].Value;
            }
        }
    }

    internal static class CatalogDropdownData
    {
        private const string EMPTY_LABEL = "<empty>";

        public static List<ValueDropdownItem<string>> BuildItems(CatalogDropdownKind kind, string currentValue)
        {
            var items = new List<ValueDropdownItem<string>>();
            items.Add(new ValueDropdownItem<string>(EMPTY_LABEL, string.Empty));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                seen.Add(currentValue);
            }

            switch (kind)
            {
                case CatalogDropdownKind.PlatformId:
                    AppendPlatformIds(items, seen);
                    break;
                case CatalogDropdownKind.AnalyticsAdapterId:
                    AppendAnalyticsAdapterIds(items, seen);
                    break;
                case CatalogDropdownKind.AnalyticsEventKey:
                    AppendAnalyticsEventKeys(items, seen, AnalyticsKeyKind.Event);
                    break;
                case CatalogDropdownKind.AnalyticsParameterKey:
                    AppendAnalyticsEventKeys(items, seen, AnalyticsKeyKind.Parameter);
                    break;
                case CatalogDropdownKind.AdsAdapterId:
                    AppendAdsAdapterIds(items, seen);
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrWhiteSpace(currentValue) && !ContainsValue(items, currentValue))
            {
                items.Insert(1, new ValueDropdownItem<string>($"{currentValue} (custom)", currentValue));
            }

            return items;
        }

        public static int FindIndex(IReadOnlyList<ValueDropdownItem<string>> items, string value)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return 0;
        }

        public static GUIContent[] BuildLabels(IReadOnlyList<ValueDropdownItem<string>> items)
        {
            if (items == null || items.Count == 0)
            {
                return Array.Empty<GUIContent>();
            }

            var labels = new GUIContent[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                labels[i] = new GUIContent(items[i].Text);
            }

            return labels;
        }

        private static void AppendPlatformIds(
            List<ValueDropdownItem<string>> items,
            HashSet<string> seen)
        {
            var catalog = FindCatalog<PlatformCatalog>();
            if (catalog == null || catalog.Entries == null)
            {
                return;
            }

            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PlatformId))
                {
                    continue;
                }

                if (!seen.Add(entry.PlatformId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(entry.DisplayName)
                    ? entry.PlatformId
                    : $"{entry.DisplayName} ({entry.PlatformId})";
                items.Add(new ValueDropdownItem<string>(label, entry.PlatformId));
            }
        }

        private static void AppendAnalyticsAdapterIds(
            List<ValueDropdownItem<string>> items,
            HashSet<string> seen)
        {
            var catalog = FindCatalog<AnalyticsAdapterCatalog>();
            if (catalog == null || catalog.Adapters == null)
            {
                return;
            }

            var entries = catalog.Adapters;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.AdapterId))
                {
                    continue;
                }

                if (!seen.Add(entry.AdapterId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(entry.name)
                    ? entry.AdapterId
                    : $"{entry.name} ({entry.AdapterId})";
                items.Add(new ValueDropdownItem<string>(label, entry.AdapterId));
            }
        }

        private static void AppendAdsAdapterIds(
            List<ValueDropdownItem<string>> items,
            HashSet<string> seen)
        {
            var catalog = FindCatalog<AdsAdapterCatalog>();
            if (catalog == null || catalog.Adapters == null)
            {
                return;
            }

            var entries = catalog.Adapters;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.AdapterId))
                {
                    continue;
                }

                if (!seen.Add(entry.AdapterId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(entry.name)
                    ? entry.AdapterId
                    : $"{entry.name} ({entry.AdapterId})";
                items.Add(new ValueDropdownItem<string>(label, entry.AdapterId));
            }
        }

        private static void AppendAnalyticsEventKeys(
            List<ValueDropdownItem<string>> items,
            HashSet<string> seen,
            AnalyticsKeyKind kind)
        {
            var catalog = FindCatalog<AnalyticsEventCatalog>();
            if (catalog == null || catalog.Entries == null)
            {
                return;
            }

            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Kind != kind || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                if (!seen.Add(entry.Key))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(entry.Name) ? entry.Key : $"{entry.Name} ({entry.Key})";
                items.Add(new ValueDropdownItem<string>(label, entry.Key));
            }
        }

        private static bool ContainsValue(IReadOnlyList<ValueDropdownItem<string>> items, string value)
        {
            if (items == null)
            {
                return false;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static T FindCatalog<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
