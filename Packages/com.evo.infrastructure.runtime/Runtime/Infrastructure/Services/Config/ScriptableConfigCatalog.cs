using System;
using System.Collections.Generic;
using UnityEngine;
using EvoDebug = _Project.Scripts.Infrastructure.Services.Debug.EvoDebug;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    [CreateAssetMenu(fileName = "ConfigCatalog", menuName = "Project/Config Catalog")]
    public sealed class ScriptableConfigCatalog : ScriptableObject
    {
        [SerializeField] private List<ScriptableConfigEntry> entries = new();
#if UNITY_EDITOR
        [SerializeField] private List<string> autoFolders = new() { "Assets/_Project/Configs" };
        [SerializeField] private bool includeSubfolders = true;
#endif

        public IReadOnlyList<ScriptableConfigEntry> Entries => entries;

#if UNITY_EDITOR
        public IReadOnlyList<string> AutoFolders => autoFolders;
        public bool IncludeSubfolders => includeSubfolders;

        public void Upsert(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            var typeName = asset.GetType().AssemblyQualifiedName;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].TypeName == typeName)
                {
                    entries[i] = new ScriptableConfigEntry(typeName, asset);
                    return;
                }
            }

            entries.Add(new ScriptableConfigEntry(typeName, asset));
        }

        public void RebuildFromFolders()
        {
            var found = ScriptableConfigCatalogEditorUtil.FindConfigsInFolders(autoFolders, includeSubfolders);
            var duplicates = ScriptableConfigCatalogEditorUtil.FindDuplicateTypes(found);
            entries.Clear();
            for (var i = 0; i < found.Count; i++)
            {
                Upsert(found[i]);
            }

            if (duplicates.Count > 0)
            {
                foreach (var entry in duplicates)
                {
                    UnityEngine.Debug.LogWarning($"ConfigCatalog: duplicate configs for type '{entry.Key.FullName}'. " +
                                                $"Using the last found asset. Count: {entry.Value.Count}");
                }
            }
        }

        public Dictionary<Type, List<UnityEngine.Object>> FindDuplicatesInFolders()
        {
            var found = ScriptableConfigCatalogEditorUtil.FindConfigsInFolders(autoFolders, includeSubfolders);
            return ScriptableConfigCatalogEditorUtil.FindDuplicateTypes(found);
        }

        public void Clear()
        {
            entries.Clear();
        }
#endif
    }

    [Serializable]
    public struct ScriptableConfigEntry
    {
        public string TypeName;
        public UnityEngine.Object Asset;

        public ScriptableConfigEntry(string typeName, UnityEngine.Object asset)
        {
            TypeName = typeName;
            Asset = asset;
        }
    }

#if UNITY_EDITOR
    internal static class ScriptableConfigCatalogEditorUtil
    {
        public static List<UnityEngine.Object> FindConfigsInFolders(IReadOnlyList<string> folders, bool includeSubfolders)
        {
            var results = new List<UnityEngine.Object>();
            if (folders == null || folders.Count == 0)
            {
                return results;
            }

            var searchFolders = new List<string>();
            for (var i = 0; i < folders.Count; i++)
            {
                var folder = folders[i];
                if (!string.IsNullOrEmpty(folder))
                {
                    searchFolders.Add(folder);
                }
            }

            if (searchFolders.Count == 0)
            {
                return results;
            }

            var guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", searchFolders.ToArray());
            for (var i = 0; i < guids.Length; i++)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!includeSubfolders && !IsDirectChildOfAnyFolder(path, searchFolders))
                {
                    continue;
                }

                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue;
                }

                if (asset is IGameConfig || HasGameConfigAttribute(asset.GetType()))
                {
                    results.Add(asset);
                }
            }

            return results;
        }

        public static Dictionary<Type, List<UnityEngine.Object>> FindDuplicateTypes(IReadOnlyList<UnityEngine.Object> assets)
        {
            var duplicates = new Dictionary<Type, List<UnityEngine.Object>>();
            if (assets == null)
            {
                return duplicates;
            }

            var grouped = new Dictionary<Type, List<UnityEngine.Object>>();
            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                var type = asset.GetType();
                if (!grouped.TryGetValue(type, out var list))
                {
                    list = new List<UnityEngine.Object>();
                    grouped[type] = list;
                }

                list.Add(asset);
            }

            foreach (var pair in grouped)
            {
                if (pair.Value.Count > 1)
                {
                    duplicates[pair.Key] = pair.Value;
                }
            }

            return duplicates;
        }

        private static bool IsDirectChildOfAnyFolder(string path, List<string> folders)
        {
            var normalized = path.Replace('\\', '/');
            for (var i = 0; i < folders.Count; i++)
            {
                var folder = folders[i].Replace('\\', '/').TrimEnd('/');
                if (normalized.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    var relative = normalized.Substring(folder.Length + 1);
                    return !relative.Contains("/");
                }
            }

            return false;
        }

        private static bool HasGameConfigAttribute(Type type)
        {
            return Attribute.IsDefined(type, typeof(GameConfigAttribute));
        }
    }
#endif
}
