using System;
using System.Collections.Generic;
using UnityEditor;
using _Project.Scripts.Infrastructure.Services.Config;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class ConfigCatalogAutoRebuild : AssetPostprocessor
    {
        private static bool _isUpdating;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_isUpdating)
            {
                return;
            }

            var changed = CollectChanged(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            if (changed.Count == 0)
            {
                return;
            }

            var catalogs = FindCatalogs();
            if (catalogs.Count == 0)
            {
                return;
            }

            _isUpdating = true;
            try
            {
                foreach (var catalog in catalogs)
                {
                    if (catalog == null || catalog.AutoFolders == null || catalog.AutoFolders.Count == 0)
                    {
                        continue;
                    }

                    if (IsOnlyCatalogChanges(changed, catalog))
                    {
                        continue;
                    }

                    if (!IsAnyUnderFolders(changed, catalog.AutoFolders))
                    {
                        continue;
                    }

                    catalog.RebuildFromFolders();
                    EditorUtility.SetDirty(catalog);
                }

                AssetDatabase.SaveAssets();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private static HashSet<string> CollectChanged(
            string[] imported,
            string[] deleted,
            string[] moved,
            string[] movedFrom)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddRange(set, imported);
            AddRange(set, deleted);
            AddRange(set, moved);
            AddRange(set, movedFrom);
            return set;
        }

        private static void AddRange(HashSet<string> set, string[] values)
        {
            if (values == null)
            {
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    set.Add(values[i].Replace('\\', '/'));
                }
            }
        }

        private static bool IsAnyUnderFolders(HashSet<string> changed, IReadOnlyList<string> folders)
        {
            for (var i = 0; i < folders.Count; i++)
            {
                var folder = folders[i];
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }

                var normalized = folder.Replace('\\', '/').TrimEnd('/');
                foreach (var path in changed)
                {
                    if (path.StartsWith(normalized + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsOnlyCatalogChanges(HashSet<string> changed, ScriptableConfigCatalog catalog)
        {
            var catalogPath = UnityEditor.AssetDatabase.GetAssetPath(catalog);
            if (string.IsNullOrEmpty(catalogPath))
            {
                return false;
            }

            if (changed.Count == 1 && changed.Contains(catalogPath.Replace('\\', '/')))
            {
                return true;
            }

            return false;
        }

        private static List<ScriptableConfigCatalog> FindCatalogs()
        {
            var list = new List<ScriptableConfigCatalog>();
            var guids = AssetDatabase.FindAssets("t:ScriptableConfigCatalog");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<ScriptableConfigCatalog>(path);
                if (catalog != null)
                {
                    list.Add(catalog);
                }
            }

            return list;
        }
    }
}
