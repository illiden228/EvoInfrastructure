using System;
using System.Collections;
using System.Collections.Generic;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public static class CatalogEditorRenderer
    {
        private const float RowHeight = 20f;
        private const float RowActionWidth = 70f;
        private const float RowIconButtonWidth = 28f;

        private static readonly Dictionary<int, State> States = new();

        public static bool Draw(ScriptableObject catalogAsset)
        {
            if (!CatalogEditorRegistry.TryCreateAdapter(catalogAsset, out var adapter))
            {
                return false;
            }

            return Draw(adapter);
        }

        public static bool Draw(ICatalogEditorAdapter adapter)
        {
            if (adapter?.CatalogAsset == null || adapter.Categories == null || adapter.Categories.Count == 0)
            {
                return false;
            }

            var state = GetState(adapter.CatalogAsset.GetInstanceID());
            state.Tab = Mathf.Clamp(state.Tab, 0, adapter.Categories.Count);
            var showTabs = adapter.Categories.Count > 1;
            if (!showTabs && state.Tab == adapter.Categories.Count)
            {
                state.Tab = 0;
            }

            DrawHeader(adapter);
            DrawValidation(adapter);

            if (showTabs)
            {
                DrawTabs(adapter, state);
                EditorGUILayout.Space(8f);
            }

            if (state.Tab == adapter.Categories.Count)
            {
                DrawSettings(adapter);
                return true;
            }

            DrawCategory(adapter, adapter.Categories[state.Tab], state);
            return true;
        }

        private static State GetState(int id)
        {
            if (!States.TryGetValue(id, out var state))
            {
                state = new State();
                States[id] = state;
            }

            return state;
        }

        private static void DrawHeader(ICatalogEditorAdapter adapter)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(string.IsNullOrWhiteSpace(adapter.Title) ? adapter.CatalogAsset.name : adapter.Title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(54f), GUILayout.Height(RowHeight)))
            {
                Selection.activeObject = adapter.CatalogAsset;
                EditorGUIUtility.PingObject(adapter.CatalogAsset);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTabs(ICatalogEditorAdapter adapter, State state)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));
            for (var i = 0; i < adapter.Categories.Count; i++)
            {
                var category = adapter.Categories[i];
                var name = string.IsNullOrWhiteSpace(category.Name) ? $"Tab {i + 1}" : category.Name;
                if (GUILayout.Toggle(state.Tab == i, name, EditorStyles.toolbarButton, GUILayout.Height(RowHeight)))
                {
                    state.Tab = i;
                }
            }

            if (GUILayout.Toggle(state.Tab == adapter.Categories.Count, EditorGUIUtility.IconContent("_Popup"), EditorStyles.toolbarButton, GUILayout.Width(28f), GUILayout.Height(RowHeight)))
            {
                state.Tab = adapter.Categories.Count;
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawValidation(ICatalogEditorAdapter adapter)
        {
            var validation = adapter.Validate() ?? new CatalogValidationResult();
            if (!validation.HasErrors && !validation.HasWarnings)
            {
                return;
            }

            var messages = new List<string>();
            for (var i = 0; i < validation.Errors.Count; i++)
            {
                messages.Add(validation.Errors[i]);
            }

            for (var i = 0; i < validation.Warnings.Count; i++)
            {
                messages.Add(validation.Warnings[i]);
            }

            EditorGUILayout.HelpBox(string.Join("\n", messages), validation.HasErrors ? MessageType.Error : MessageType.Warning);
        }

        private static void DrawCategory(ICatalogEditorAdapter adapter, CatalogCategoryDescriptor category, State state)
        {
            var directory = GetDirectory(adapter, category);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(RowHeight));
            GUILayout.Label(directory, EditorStyles.miniLabel, GUILayout.MinWidth(120f), GUILayout.Height(RowHeight));
            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(20f), GUILayout.Height(RowHeight));
            state.Search = GUILayout.TextField(state.Search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.Width(280f), GUILayout.Height(RowHeight));
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(28f), GUILayout.Height(RowHeight)))
            {
                ShowCreateMenu(adapter, category);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f);

            var items = GetItems(category, state.Search);
            if (items.Count == 0)
            {
                EditorGUILayout.HelpBox("No items in this category.", MessageType.Info);
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                DrawItemRow(adapter, category, items[i].Item, items[i].ListIndex, state);
            }
        }

        private static void DrawSettings(ICatalogEditorAdapter adapter)
        {
            EditorGUILayout.HelpBox("Editor-only directories used by this catalog window for creating new assets.", MessageType.Info);
            for (var i = 0; i < adapter.Categories.Count; i++)
            {
                var category = adapter.Categories[i];
                var key = category.SettingsKey(adapter.CatalogAsset);
                var fallback = GetDefaultDirectory(category);
                var current = CatalogEditorSettings.instance.GetDirectory(key, fallback);
                EditorGUI.BeginChangeCheck();
                var next = EditorGUILayout.TextField(category.Name, current);
                if (EditorGUI.EndChangeCheck())
                {
                    CatalogEditorSettings.instance.SetDirectory(key, next, fallback);
                }
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Reset Directories To Defaults", GUILayout.Width(220f)))
            {
                for (var i = 0; i < adapter.Categories.Count; i++)
                {
                    var category = adapter.Categories[i];
                    var fallback = GetDefaultDirectory(category);
                    CatalogEditorSettings.instance.SetDirectory(category.SettingsKey(adapter.CatalogAsset), fallback, fallback);
                }
            }
        }

        private static List<ItemEntry> GetItems(CatalogCategoryDescriptor category, string search)
        {
            var result = new List<ItemEntry>();
            var list = category.GetMutableList?.Invoke();
            if (list == null)
            {
                return result;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] is not UnityEngine.Object item || item == null)
                {
                    continue;
                }

                if ((category.Contains == null || category.Contains(item)) && MatchesSearch(category, item, i, search))
                {
                    result.Add(new ItemEntry(item, i));
                }
            }

            return result;
        }

        private static void DrawItemRow(ICatalogEditorAdapter adapter, CatalogCategoryDescriptor category, UnityEngine.Object item, int listIndex, State state)
        {
            var itemId = item.GetInstanceID();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

            GUILayout.Label((listIndex + 1).ToString(), EditorStyles.miniLabel, GUILayout.Width(28f), GUILayout.Height(RowHeight));

            state.Expanded.TryGetValue(itemId, out var expanded);
            var foldoutRect = GUILayoutUtility.GetRect(240f, RowHeight, GUILayout.Width(240f), GUILayout.Height(RowHeight));
            expanded = EditorGUI.Foldout(foldoutRect, expanded, category.KeyProvider?.GetDisplayName(item, listIndex) ?? item.name, true);
            state.Expanded[itemId] = expanded;

            var next = EditorGUILayout.ObjectField(item, category.ItemType, false, GUILayout.Height(RowHeight));
            if (next != null && next != item)
            {
                ReplaceItem(adapter, category, listIndex, next);
            }

            if (GUILayout.Button("Ping", GUILayout.Width(RowActionWidth), GUILayout.Height(RowHeight)))
            {
                Selection.activeObject = item;
                EditorGUIUtility.PingObject(item);
            }

            if (GUILayout.Button("Remove", GUILayout.Width(RowActionWidth), GUILayout.Height(RowHeight)))
            {
                RemoveItem(adapter, item);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(RowIconButtonWidth), GUILayout.Height(RowHeight)))
            {
                DeleteAsset(adapter, item);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (expanded)
            {
                DrawInlineInspector(state, item, itemId);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawInlineInspector(State state, UnityEngine.Object item, int itemId)
        {
            EditorGUILayout.Space(4f);
            if (!state.Editors.TryGetValue(itemId, out var editor) || editor == null)
            {
                editor = Editor.CreateEditor(item);
                state.Editors[itemId] = editor;
            }

            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 145f;
            EditorGUI.indentLevel++;
            try
            {
                editor.OnInspectorGUI();
            }
            finally
            {
                EditorGUI.indentLevel--;
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        private static void ShowCreateMenu(ICatalogEditorAdapter adapter, CatalogCategoryDescriptor category)
        {
            var types = GetCreatableTypes(category);
            if (types.Count == 0)
            {
                Debug.LogWarning($"{adapter.CatalogAsset.name}: no creatable item types were found for {category.ItemType?.Name ?? "category"}.");
                return;
            }

            if (types.Count == 1)
            {
                CreateAndAdd(adapter, category, types[0]);
                return;
            }

            var menu = new GenericMenu();
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                menu.AddItem(new GUIContent(type.Name), false, () => CreateAndAdd(adapter, category, type));
            }

            menu.ShowAsContext();
        }

        private static void CreateAndAdd(ICatalogEditorAdapter adapter, CatalogCategoryDescriptor category, Type itemType)
        {
            if (itemType == null || !typeof(ScriptableObject).IsAssignableFrom(itemType))
            {
                return;
            }

            var item = ScriptableObject.CreateInstance(itemType);
            if (item == null)
            {
                return;
            }

            var directory = GetDirectory(adapter, category);
            EnsureDirectory(directory);
            var baseName = category.BuildAssetBaseName?.Invoke(itemType);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = $"new_{itemType.Name}";
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{baseName}.asset");
            var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            item.name = fileName;
            category.KeyProvider?.TrySetKey(item, DefaultCatalogEditorAdapter.NormalizeId(fileName));

            AssetDatabase.CreateAsset(item, assetPath);
            category.GetMutableList?.Invoke()?.Add(item);
            EditorUtility.SetDirty(adapter.CatalogAsset);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        private static void ReplaceItem(ICatalogEditorAdapter adapter, CatalogCategoryDescriptor category, int listIndex, UnityEngine.Object newItem)
        {
            var list = category.GetMutableList?.Invoke();
            if (list == null || listIndex < 0 || listIndex >= list.Count)
            {
                return;
            }

            list[listIndex] = newItem;
            EditorUtility.SetDirty(adapter.CatalogAsset);
        }

        private static void RemoveItem(ICatalogEditorAdapter adapter, UnityEngine.Object item)
        {
            var categories = adapter.Categories;
            for (var i = 0; i < categories.Count; i++)
            {
                categories[i].GetMutableList?.Invoke()?.Remove(item);
            }

            EditorUtility.SetDirty(adapter.CatalogAsset);
        }

        private static void DeleteAsset(ICatalogEditorAdapter adapter, UnityEngine.Object item)
        {
            var path = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrWhiteSpace(path))
            {
                RemoveItem(adapter, item);
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Delete Catalog Asset",
                    $"Delete asset '{item.name}'?\n\n{path}\n\nThis removes references from this catalog and deletes the asset file.",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            RemoveItem(adapter, item);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool MatchesSearch(CatalogCategoryDescriptor category, UnityEngine.Object item, int index, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var value = search.Trim();
            var displayName = category.KeyProvider?.GetDisplayName(item, index) ?? string.Empty;
            return item.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
                   || displayName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<Type> GetCreatableTypes(CatalogCategoryDescriptor category)
        {
            return category.GetCreatableTypes?.Invoke(category.ItemType)
                   ?? DefaultCatalogEditorAdapter.GetCreatableTypes(category.ItemType);
        }

        private static string GetDirectory(ICatalogEditorAdapter adapter, CatalogCategoryDescriptor category)
        {
            return CatalogEditorSettings.instance.GetDirectory(category.SettingsKey(adapter.CatalogAsset), GetDefaultDirectory(category));
        }

        private static string GetDefaultDirectory(CatalogCategoryDescriptor category)
        {
            return string.IsNullOrWhiteSpace(category.DefaultCreateDirectory) ? "Assets" : category.DefaultCreateDirectory;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private readonly struct ItemEntry
        {
            public ItemEntry(UnityEngine.Object item, int listIndex)
            {
                Item = item;
                ListIndex = listIndex;
            }

            public UnityEngine.Object Item { get; }
            public int ListIndex { get; }
        }

        private sealed class State
        {
            public int Tab;
            public string Search = string.Empty;
            public Dictionary<int, bool> Expanded { get; } = new();
            public Dictionary<int, Editor> Editors { get; } = new();
        }
    }
}
