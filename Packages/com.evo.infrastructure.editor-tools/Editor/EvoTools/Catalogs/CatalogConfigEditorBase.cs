using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public abstract class CatalogConfigEditorBase : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target is ScriptableObject catalogAsset &&
                CatalogEditorRegistry.TryCreateAdapter(catalogAsset, out var adapter) &&
                adapter?.Categories != null &&
                adapter.Categories.Count > 0)
            {
                serializedObject.Update();
                DrawExtraSerializedProperties(CollectCatalogCollectionPropertyNames(adapter));
                CatalogEditorRenderer.Draw(adapter);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawDefaultInspector();
        }

        protected virtual bool ShouldDrawExtraProperty(SerializedProperty property)
        {
            return true;
        }

        private void DrawExtraSerializedProperties(HashSet<string> catalogCollectionPropertyNames)
        {
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            var drewAny = false;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!ShouldDrawExtraPropertyInternal(iterator, catalogCollectionPropertyNames))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, includeChildren: true);
                drewAny = true;
            }

            if (drewAny)
            {
                EditorGUILayout.Space(8f);
            }
        }

        private bool ShouldDrawExtraPropertyInternal(
            SerializedProperty property,
            HashSet<string> catalogCollectionPropertyNames)
        {
            if (property == null || property.propertyPath == "m_Script")
            {
                return false;
            }

            if (catalogCollectionPropertyNames != null && catalogCollectionPropertyNames.Contains(property.propertyPath))
            {
                return false;
            }

            return ShouldDrawExtraProperty(property);
        }

        private static HashSet<string> CollectCatalogCollectionPropertyNames(ICatalogEditorAdapter adapter)
        {
            var result = new HashSet<string>();
            var categories = adapter?.Categories;
            if (categories == null)
            {
                return result;
            }

            for (var i = 0; i < categories.Count; i++)
            {
                var id = categories[i]?.Id;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }
    }
}
