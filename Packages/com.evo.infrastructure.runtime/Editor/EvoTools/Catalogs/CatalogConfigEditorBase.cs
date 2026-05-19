using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public abstract class CatalogConfigEditorBase : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target is ScriptableObject catalogAsset && CatalogEditorRenderer.Draw(catalogAsset))
            {
                return;
            }

            DrawDefaultInspector();
        }
    }
}
