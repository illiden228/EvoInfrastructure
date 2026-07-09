using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    internal static class CatalogEditorMenu
    {
        private const string OpenMenuPath = "EvoTools/Open Catalog Editor";

        [MenuItem(OpenMenuPath, false, 51)]
        private static void OpenSelectedCatalog()
        {
            if (Selection.activeObject is ScriptableObject catalog)
            {
                CatalogEditorWindow.Open(catalog);
            }
        }

        [MenuItem(OpenMenuPath, true, 51)]
        private static bool CanOpenSelectedCatalog()
        {
            return Selection.activeObject is ScriptableObject catalog
                   && CatalogEditorRegistry.TryCreateAdapter(catalog, out _);
        }
    }
}
