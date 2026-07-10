using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools
{
    public static class SdkAdapterConfigMigration
    {
        private const string BACKUP_ROOT = "Assets/_Project/ConfigMigrationBackups";
        private static readonly Migration[] Migrations =
        {
            new(
                "Game.Runtime.Analytics.FirebaseAnalyticsAdapterConfig",
                "Evo.Infrastructure.Services.Analytics.Firebase.FirebaseAnalyticsAdapterConfig, " +
                "Evo.Infrastructure.Analytics.Firebase"),
            new(
                "Game.Runtime.Analytics.AppMetricaAnalyticsAdapterConfig",
                "Evo.Infrastructure.Services.Analytics.AppMetrica.AppMetricaAnalyticsAdapterConfig, " +
                "Evo.Infrastructure.Analytics.AppMetrica"),
            new(
                "Game.Runtime.Analytics.AdjustAnalyticsAdapterConfig",
                "Evo.Infrastructure.Services.Analytics.Adjust.AdjustAnalyticsAdapterConfig, " +
                "Evo.Infrastructure.Analytics.Adjust"),
            new(
                "Game.Runtime.Ads.AppLovinAdsAdapterConfig",
                "Evo.Infrastructure.Services.Ads.AppLovin.AppLovinAdsAdapterConfig, " +
                "Evo.Infrastructure.Ads.AppLovin")
        };

        [MenuItem("EvoTools/Configs/Migrate SDK Adapter Configs")]
        public static void MigrateSdkAdapterConfigs()
        {
            var migrated = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            EnsureBackupFolder();
            foreach (var migration in Migrations)
            {
                Migrate(migration, migrated);
            }

            ReplaceCatalogReferences(migrated);
            RebuildConfigCatalogs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[Evo Config Migration] Migrated {migrated.Count} SDK adapter config asset(s). " +
                "Legacy copies use the '.legacy.asset' suffix.");
        }

        [MenuItem("EvoTools/Configs/Rebuild Config Catalogs")]
        public static void RebuildConfigCatalogs()
        {
            var changedPaths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableConfigCatalog"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                var method = asset?.GetType().GetMethod(
                    "RebuildFromFolders",
                    BindingFlags.Instance | BindingFlags.Public);
                if (method != null && method.Invoke(asset, null) is bool changed && changed)
                {
                    EditorUtility.SetDirty(asset);
                    changedPaths.Add(path);
                }
            }

            if (changedPaths.Count > 0)
            {
                AssetDatabase.ForceReserializeAssets(
                    changedPaths,
                    ForceReserializeAssetsOptions.ReserializeAssets);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Evo Config Migration] Rebuilt config catalogs with runtime " +
                $"AssemblyQualifiedName values. Changed: {changedPaths.Count}.");
        }

        [MenuItem("EvoTools/Configs/Force Reserialize SDK Adapter Configs")]
        public static void ForceReserializeSdkAdapterConfigs()
        {
            var paths = new List<string>();
            foreach (var migration in Migrations)
            {
                var type = Type.GetType(migration.TargetAssemblyQualifiedName, false);
                if (type == null)
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null && asset.GetType() == type)
                    {
                        paths.Add(path);
                    }
                }
            }

            if (paths.Count > 0)
            {
                AssetDatabase.ForceReserializeAssets(
                    paths,
                    ForceReserializeAssetsOptions.ReserializeAssets);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Evo Config Migration] Force-reserialized {paths.Count} SDK adapter config asset(s).");
        }

        private static void Migrate(
            Migration migration,
            IDictionary<UnityEngine.Object, UnityEngine.Object> migrated)
        {
            var targetType = Type.GetType(migration.TargetAssemblyQualifiedName, false);
            if (targetType == null)
            {
                Debug.LogWarning(
                    $"[Evo Config Migration] Target type unavailable: " +
                    migration.TargetAssemblyQualifiedName);
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var source = AssetDatabase.LoadMainAssetAtPath(path);
                if (source == null || source.GetType().FullName != migration.LegacyFullName)
                {
                    continue;
                }

                var legacyPath = $"{BACKUP_ROOT}/{Path.GetFileNameWithoutExtension(path)}.legacy.asset";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(legacyPath)))
                {
                    Debug.LogWarning(
                        $"[Evo Config Migration] Legacy backup already exists: {legacyPath}");
                    continue;
                }

                var json = EditorJsonUtility.ToJson(source);
                var moveError = AssetDatabase.MoveAsset(path, legacyPath);
                if (!string.IsNullOrEmpty(moveError))
                {
                    Debug.LogError(
                        $"[Evo Config Migration] Could not back up '{path}': {moveError}");
                    continue;
                }

                var target = ScriptableObject.CreateInstance(targetType);
                target.name = source.name;
                EditorJsonUtility.FromJsonOverwrite(json, target);
                AssetDatabase.CreateAsset(target, path);
                migrated[AssetDatabase.LoadMainAssetAtPath(legacyPath)] = target;
                EditorUtility.SetDirty(target);
            }
        }

        private static void EnsureBackupFolder()
        {
            if (AssetDatabase.IsValidFolder(BACKUP_ROOT))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }

            AssetDatabase.CreateFolder("Assets/_Project", "ConfigMigrationBackups");
        }

        private static void ReplaceCatalogReferences(
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
        {
            if (replacements.Count == 0)
            {
                return;
            }

            var catalogFilters = new[]
            {
                "t:AnalyticsAdapterCatalog",
                "t:AdsAdapterCatalog",
                "t:ScriptableConfigCatalog"
            };

            foreach (var filter in catalogFilters)
            {
                foreach (var guid in AssetDatabase.FindAssets(filter))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset == null)
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(asset);
                    var iterator = serialized.GetIterator();
                    var changed = false;
                    if (iterator.Next(true))
                    {
                        do
                        {
                            if (iterator.propertyType != SerializedPropertyType.ObjectReference ||
                                iterator.objectReferenceValue == null ||
                                !replacements.TryGetValue(
                                    iterator.objectReferenceValue,
                                    out var replacement))
                            {
                                continue;
                            }

                            iterator.objectReferenceValue = replacement;
                            changed = true;
                        } while (iterator.Next(true));
                    }

                    if (changed)
                    {
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(asset);
                    }
                }
            }
        }

        private readonly struct Migration
        {
            public readonly string LegacyFullName;
            public readonly string TargetAssemblyQualifiedName;
            public Migration(string legacyFullName, string targetAssemblyQualifiedName)
            {
                LegacyFullName = legacyFullName;
                TargetAssemblyQualifiedName = targetAssemblyQualifiedName;
            }
        }
    }
}
