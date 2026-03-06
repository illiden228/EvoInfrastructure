using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using EvoDebug = _Project.Scripts.Infrastructure.Services.Debug.EvoDebug;
using UnityEngine;
using _Project.Scripts.Infrastructure.Services.Config;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class ConfigHubWindow : OdinMenuEditorWindow
    {
        private const string CATALOG_FOLDER = "Assets/_Project/Configs";
        private const string CATALOG_ASSET_NAME = "ConfigCatalog.asset";

        private static List<Type> _configTypesCache;

        private ScriptableConfigCatalog _catalog;
        private List<ConfigView> _configs = new();
        private Dictionary<Type, List<UnityEngine.Object>> _duplicates = new();
        private readonly Dictionary<Type, string> _categoryCache = new();
        private List<Type> _missingConfigTypes = new();
        private ConfigSettingsView _settingsView;
        private bool _configsDirty = true;
        private bool _missingConfigTypesDirty = true;

        [MenuItem("EvoTools/Configs", false, 1)]
        private static void Open()
        {
            GetWindow<ConfigHubWindow>(EvoToolsLocalization.Get("config_hub.window_title", "Configs"));
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            EnsureCatalog();
            RefreshViewCacheIfNeeded();

            var tree = new OdinMenuTree(true)
            {
                Config =
                {
                    DrawSearchToolbar = true
                }
            };

            var settingsLabel = EvoToolsLocalization.Get("config_hub.category.settings", "Config Settings");
            tree.Add(settingsLabel, _settingsView ??= new ConfigSettingsView(this, _catalog, _duplicates));

            foreach (var view in _configs)
            {
                var category = string.IsNullOrEmpty(view.Category)
                    ? EvoToolsLocalization.Get("config_hub.category.general", "General")
                    : view.Category;

                var categoryLabel = $"[{category}]";
                var path = $"{categoryLabel}/{view.Name}";
                tree.Add(path, view.Asset);
            }

            return tree;
        }

        private void RefreshViewCacheIfNeeded()
        {
            if (_configsDirty)
            {
                _configs = CollectConfigs(_catalog);
                _settingsView = new ConfigSettingsView(this, _catalog, _duplicates);
                _configsDirty = false;
            }

            if (_missingConfigTypesDirty)
            {
                _missingConfigTypes = BuildMissingConfigTypes();
                _missingConfigTypesDirty = false;
                _settingsView?.SetMissingConfigsState(_missingConfigTypes.Count > 0);
            }
        }

        private List<ConfigView> CollectConfigs(ScriptableConfigCatalog catalog)
        {
            var list = new List<ConfigView>();
            if (catalog == null)
            {
                EvoDebug.LogWarning(
                    EvoToolsLocalization.Get("config_hub.warning.no_catalog", "Config catalog not found."),
                    nameof(ConfigHubWindow));
                return list;
            }

            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var asset = entries[i].Asset;
                if (asset == null)
                {
                    continue;
                }

                var type = asset.GetType();
                var category = ResolveCategory(asset, type);
                list.Add(new ConfigView(asset, category));
            }

            return list
                .GroupBy(c => c.Asset)
                .Select(g => g.First())
                .ToList();
        }

        private string ResolveCategory(UnityEngine.Object asset, Type type)
        {
            if (type != null && _categoryCache.TryGetValue(type, out var cachedCategory))
            {
                return cachedCategory;
            }

            string category;
            if (asset is IConfigCategoryProvider provider && !string.IsNullOrEmpty(provider.Category))
            {
                category = provider.Category;
            }
            else
            {
                var attr = type.GetCustomAttributes(typeof(GameConfigAttribute), false)
                    .FirstOrDefault() as GameConfigAttribute;
                category = attr != null && !string.IsNullOrEmpty(attr.Category)
                    ? attr.Category
                    : EvoToolsLocalization.Get("config_hub.category.general", "General");
            }

            if (type != null)
            {
                _categoryCache[type] = category;
            }

            return category;
        }

        private static ScriptableConfigCatalog EnsureCatalogAsset()
        {
            var assetPath = $"{CATALOG_FOLDER}/{CATALOG_ASSET_NAME}";
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableConfigCatalog>(assetPath);
            if (catalog != null)
            {
                return catalog;
            }

            EnsureFolderExists(CATALOG_FOLDER);
            catalog = CreateInstance<ScriptableConfigCatalog>();
            AssetDatabase.CreateAsset(catalog, assetPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private void EnsureCatalog()
        {
            _catalog = EnsureCatalogAsset();
            _duplicates = CollectDuplicates(_catalog);
            _settingsView?.SetDuplicates(_duplicates);
        }

        private static void EnsureFolderExists(string folder)
        {
            var normalized = folder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var segments = normalized.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private sealed class ConfigView
        {
            public readonly UnityEngine.Object Asset;
            public readonly string Category;
            public readonly string Name;

            public ConfigView(UnityEngine.Object asset, string category)
            {
                Asset = asset;
                Category = category;
                Name = asset != null ? asset.name : "Unknown";
            }
        }

        private sealed class ConfigSettingsView
        {
            private readonly ConfigHubWindow _owner;
            private readonly ScriptableConfigCatalog _catalog;
            private Dictionary<Type, List<UnityEngine.Object>> _duplicates;
            private string _duplicateWarning = string.Empty;
            private bool _hasMissingConfigs;

            public ConfigSettingsView(
                ConfigHubWindow owner,
                ScriptableConfigCatalog catalog,
                Dictionary<Type, List<UnityEngine.Object>> duplicates)
            {
                _owner = owner;
                _catalog = catalog;
                SetDuplicates(duplicates);
            }

            [Title("@EvoToolsLocalization.Get(\"config_hub.title.settings\", \"Config Settings\")")]
            [InfoBox("@EvoToolsLocalization.Get(\"config_hub.info.settings\", \"Configure folders used for automatic config discovery.\")", InfoMessageType.Info)]
            [InfoBox("@DuplicateWarning", InfoMessageType.Warning, VisibleIf = "@HasDuplicates")]
            [ShowInInspector]
            private ScriptableConfigCatalog Catalog => _catalog;

            [PropertySpace(SpaceBefore = 6)]
            [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"config_hub.button.show_catalog\", \"Show Catalog\")")]
            private void ShowCatalog()
            {
                if (_owner == null)
                {
                    return;
                }

                var catalog = _owner.GetCatalog();
                if (catalog != null)
                {
                    Selection.activeObject = catalog;
                    EditorGUIUtility.PingObject(catalog);
                }
            }

            [PropertySpace(SpaceBefore = 8)]
            [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"config_hub.button.rebuild\", \"Rebuild\")")]
            private void Rebuild()
            {
                if (_catalog == null)
                {
                    EvoDebug.LogWarning(
                        EvoToolsLocalization.Get("config_hub.warning.no_catalog", "Config catalog not found."),
                        nameof(ConfigHubWindow));
                    return;
                }

                _catalog.RebuildFromFolders();
                EditorUtility.SetDirty(_catalog);
                AssetDatabase.SaveAssets();
                _owner?.RefreshCatalogState(true);
            }

            [PropertySpace(SpaceBefore = 6)]
            [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"config_hub.button.create_missing\", \"Create Missing Configs\")")]
            [EnableIf("@HasMissingConfigs")]
            private void CreateMissing()
            {
                if (_owner == null)
                {
                    return;
                }

                _owner.CreateMissingConfigs();
                _owner.RefreshCatalogState(true);
            }

            private bool HasDuplicates => _duplicates != null && _duplicates.Count > 0;
            private string DuplicateWarning => _duplicateWarning;
            private bool HasMissingConfigs => _hasMissingConfigs;

            public void SetDuplicates(Dictionary<Type, List<UnityEngine.Object>> duplicates)
            {
                _duplicates = duplicates ?? new Dictionary<Type, List<UnityEngine.Object>>();
                _duplicateWarning = BuildDuplicateWarning(_duplicates);
            }

            public void SetMissingConfigsState(bool hasMissingConfigs)
            {
                _hasMissingConfigs = hasMissingConfigs;
            }

            private static string BuildDuplicateWarning(Dictionary<Type, List<UnityEngine.Object>> duplicates)
            {
                if (duplicates == null || duplicates.Count == 0)
                {
                    return string.Empty;
                }

                var lines = new List<string>();
                foreach (var entry in duplicates)
                {
                    lines.Add($"{entry.Key.Name}: {entry.Value.Count}");
                }

                return EvoToolsLocalization.Get("config_hub.warning.duplicates", "Duplicate config types detected:") +
                       "\n" + string.Join("\n", lines);
            }
        }

        private static Dictionary<Type, List<UnityEngine.Object>> CollectDuplicates(ScriptableConfigCatalog catalog)
        {
            if (catalog == null)
            {
                return new Dictionary<Type, List<UnityEngine.Object>>();
            }
            return catalog.FindDuplicatesInFolders();
        }

        private ScriptableConfigCatalog GetCatalog()
        {
            return _catalog;
        }

        private List<Type> GetMissingConfigTypes()
        {
            RefreshViewCacheIfNeeded();
            return _missingConfigTypes;
        }

        private List<Type> BuildMissingConfigTypes()
        {
            var allTypes = FindConfigTypes();
            if (_catalog == null)
            {
                return new List<Type>(allTypes);
            }

            var existing = new HashSet<Type>();
            var entries = _catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Asset != null)
                {
                    existing.Add(entries[i].Asset.GetType());
                }
            }

            var missing = new List<Type>();
            for (var i = 0; i < allTypes.Count; i++)
            {
                if (!existing.Contains(allTypes[i]))
                {
                    missing.Add(allTypes[i]);
                }
            }

            return missing;
        }

        private void CreateMissingConfigs()
        {
            var missing = GetMissingConfigTypes();
            for (var i = 0; i < missing.Count; i++)
            {
                CreateConfigAsset(missing[i], false);
            }
        }

        private static List<Type> FindConfigTypes()
        {
            if (_configTypesCache != null)
            {
                return _configTypesCache;
            }

            var result = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (var t = 0; t < types.Length; t++)
                {
                    var type = types[t];
                    if (type == null ||
                        type.IsAbstract ||
                        !typeof(ScriptableObject).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (typeof(IGameConfig).IsAssignableFrom(type) ||
                        Attribute.IsDefined(type, typeof(GameConfigAttribute)))
                    {
                        result.Add(type);
                    }
                }
            }

            _configTypesCache = result
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();
            return _configTypesCache;
        }

        private void RefreshCatalogState(bool rebuildMenu)
        {
            _duplicates = CollectDuplicates(_catalog);
            _missingConfigTypesDirty = true;
            _settingsView?.SetDuplicates(_duplicates);

            if (rebuildMenu)
            {
                _configsDirty = true;
                ForceMenuTreeRebuild();
            }
        }

        private void CreateConfigAsset(Type type, bool select)
        {
            if (type == null)
            {
                return;
            }

            var existing = FindExistingConfigAsset(type);
            if (existing != null)
            {
                if (_catalog != null)
                {
                    _catalog.Upsert(existing);
                    EditorUtility.SetDirty(_catalog);
                    AssetDatabase.SaveAssets();
                }

                RefreshCatalogState(false);
                if (select)
                {
                    Selection.activeObject = existing;
                }

                return;
            }

            var folder = GetTargetFolder();
            EnsureFolderExists(folder);

            var defaultName = $"{type.Name}.asset";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{defaultName}");
            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            if (_catalog != null)
            {
                _catalog.Upsert(asset);
                EditorUtility.SetDirty(_catalog);
                AssetDatabase.SaveAssets();
            }

            RefreshCatalogState(false);
            if (select)
            {
                Selection.activeObject = asset;
            }
        }

        private static UnityEngine.Object FindExistingConfigAsset(Type type)
        {
            if (type == null)
            {
                return null;
            }

            var guids = AssetDatabase.FindAssets($"t:{type.Name}");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            UnityEngine.Object found = null;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset == null)
                {
                    continue;
                }

                if (found == null)
                {
                    found = asset;
                }
                else
                {
                    EvoDebug.LogWarning(
                        $"ConfigHub: multiple assets found for '{type.Name}'. Using the first one.",
                        nameof(ConfigHubWindow));
                    break;
                }
            }

            return found;
        }

        private string GetTargetFolder()
        {
            if (_catalog != null && _catalog.AutoFolders != null && _catalog.AutoFolders.Count > 0)
            {
                var folder = _catalog.AutoFolders[0];
                if (!string.IsNullOrEmpty(folder))
                {
                    return folder.Replace('\\', '/').TrimEnd('/');
                }
            }

            return CATALOG_FOLDER;
        }
    }
}
