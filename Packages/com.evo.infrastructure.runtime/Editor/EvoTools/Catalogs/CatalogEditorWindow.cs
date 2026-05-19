using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public sealed class CatalogEditorWindow : EditorWindow
    {
        [SerializeField] private ScriptableObject catalog;
        [SerializeField] private Vector2 scrollPosition;

        private UnityEditor.Editor _fallbackEditor;

        public static void Open(ScriptableObject catalogAsset)
        {
            if (catalogAsset == null)
            {
                return;
            }

            var window = CreateWindow<CatalogEditorWindow>();
            window.catalog = catalogAsset;
            window.titleContent = new GUIContent($"{catalogAsset.name} Catalog");
            window.minSize = new Vector2(520f, 420f);
            window.position = BuildCenteredWindowRect(new Vector2(860f, 720f));
            window.Show();
            window.Focus();
        }

        private void OnDisable()
        {
            if (_fallbackEditor != null)
            {
                DestroyImmediate(_fallbackEditor);
                _fallbackEditor = null;
            }
        }

        private void OnGUI()
        {
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("Catalog asset is not assigned.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (CatalogEditorRenderer.Draw(catalog))
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            UnityEditor.Editor.CreateCachedEditor(catalog, null, ref _fallbackEditor);
            if (_fallbackEditor == null)
            {
                EditorGUILayout.HelpBox("Failed to create editor for catalog asset.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            _fallbackEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private static Rect BuildCenteredWindowRect(Vector2 size)
        {
            var main = GetMainEditorWindowRect();
            var width = Mathf.Max(size.x, 520f);
            var height = Mathf.Max(size.y, 420f);
            var x = main.x + (main.width - width) * 0.5f;
            var y = main.y + (main.height - height) * 0.5f;
            return new Rect(x, y, width, height);
        }

        private static Rect GetMainEditorWindowRect()
        {
            var containerWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ContainerWindow");
            if (containerWindowType == null)
            {
                return new Rect(100f, 100f, 1280f, 720f);
            }

            var showModeField = containerWindowType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
            var positionProperty = containerWindowType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
            var windows = Resources.FindObjectsOfTypeAll(containerWindowType);

            for (var i = 0; i < windows.Length; i++)
            {
                if (showModeField?.GetValue(windows[i]) is int showMode && showMode == 4)
                {
                    if (positionProperty?.GetValue(windows[i], null) is Rect rect)
                    {
                        return rect;
                    }
                }
            }

            return new Rect(100f, 100f, 1280f, 720f);
        }
    }
}
