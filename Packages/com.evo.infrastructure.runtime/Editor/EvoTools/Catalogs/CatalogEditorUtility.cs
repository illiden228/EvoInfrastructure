using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public static class CatalogEditorUtility
    {
        public static bool OpenCatalogForItem(UnityEngine.Object item)
        {
            if (item == null)
            {
                return false;
            }

            ScriptableObject fallbackAsset = null;
            var catalogGuids = AssetDatabase.FindAssets("t:ScriptableObject");
            for (var i = 0; i < catalogGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(catalogGuids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                fallbackAsset ??= asset;
                if (!CatalogEditorRegistry.TryCreateAdapter(asset, out var adapter))
                {
                    continue;
                }

                if (!ContainsItem(adapter, item))
                {
                    continue;
                }

                var targetCatalog = asset;
                EditorApplication.delayCall += () => CatalogEditorWindow.Open(targetCatalog);
                return true;
            }

            if (item is ScriptableObject itemAsset)
            {
                EditorApplication.delayCall += () => CatalogEditorWindow.Open(itemAsset);
                return false;
            }

            if (fallbackAsset != null)
            {
                EditorApplication.delayCall += () => CatalogEditorWindow.Open(fallbackAsset);
            }

            return false;
        }

        private static bool ContainsItem(ICatalogEditorAdapter adapter, UnityEngine.Object item)
        {
            var categories = adapter.Categories;
            if (categories == null)
            {
                return false;
            }

            for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
            {
                var list = categories[categoryIndex].GetMutableList?.Invoke();
                if (list == null)
                {
                    continue;
                }

                for (var itemIndex = 0; itemIndex < list.Count; itemIndex++)
                {
                    if (list[itemIndex] is UnityEngine.Object catalogItem && catalogItem == item)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
