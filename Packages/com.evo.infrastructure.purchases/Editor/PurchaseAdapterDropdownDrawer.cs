using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases.Editor
{
    [CustomPropertyDrawer(typeof(PurchaseAdapterDropdownAttribute))]
    public sealed class PurchaseAdapterDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var adapterIds = BuildAdapterIds(property.stringValue);
            var labels = new GUIContent[adapterIds.Count];
            var selectedIndex = 0;
            for (var index = 0; index < adapterIds.Count; index++)
            {
                var adapterId = adapterIds[index];
                labels[index] = new GUIContent(adapterId.Length == 0 ? "<any adapter>" : adapterId);
                if (string.Equals(adapterId, property.stringValue, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = index;
                }
            }

            EditorGUI.BeginProperty(position, label, property);
            var nextIndex = EditorGUI.Popup(position, label, selectedIndex, labels);
            if (nextIndex >= 0 && nextIndex < adapterIds.Count)
            {
                property.stringValue = adapterIds[nextIndex];
            }
            EditorGUI.EndProperty();
        }

        public static IReadOnlyList<string> BuildAdapterIds(string currentValue)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { string.Empty };
            var normalizedCurrentValue = currentValue?.Trim() ?? string.Empty;
            if (normalizedCurrentValue.Length > 0)
            {
                ids.Add(normalizedCurrentValue);
            }

            var guids = AssetDatabase.FindAssets($"t:{nameof(PurchaseRoutingConfig)}");
            for (var guidIndex = 0; guidIndex < guids.Length; guidIndex++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[guidIndex]);
                var routing = AssetDatabase.LoadAssetAtPath<PurchaseRoutingConfig>(path);
                if (routing?.Adapters == null)
                {
                    continue;
                }

                for (var bindingIndex = 0; bindingIndex < routing.Adapters.Count; bindingIndex++)
                {
                    var adapterId = routing.Adapters[bindingIndex]?.AdapterId;
                    if (!string.IsNullOrWhiteSpace(adapterId))
                    {
                        ids.Add(adapterId);
                    }
                }
            }

            var result = new List<string>(ids);
            result.Sort((left, right) =>
            {
                if (left.Length == 0)
                {
                    return right.Length == 0 ? 0 : -1;
                }

                if (right.Length == 0)
                {
                    return 1;
                }

                return StringComparer.OrdinalIgnoreCase.Compare(left, right);
            });
            return result;
        }
    }
}
