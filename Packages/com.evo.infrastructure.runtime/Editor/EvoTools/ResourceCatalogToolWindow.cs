using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using EvoDebug = _Project.Scripts.Infrastructure.Services.Debug.EvoDebug;
using UnityEditor;
using UnityEngine;
using _Project.Scripts.Infrastructure.Services.ResourceCatalog;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class ResourceCatalogToolWindow : OdinEditorWindow
    {
        private const string DEFAULT_CATALOG_FOLDER = "Assets/_Project/Configs";

        public enum ImportType
        {
            Sprite = 0,
            GameObject = 1,
            AudioClip = 2
        }

        public enum KeyMode
        {
            AssetName = 0,
            RelativePath = 1
        }

        [MenuItem("EvoTools/Resource Catalog Builder", false, 50)]
        private static void Open()
        {
            GetWindow<ResourceCatalogToolWindow>(EvoToolsLocalization.Get("resource_catalog.window_title", "Resource Catalog Builder"));
        }

        [Title("@EvoToolsLocalization.Get(\"resource_catalog.title.catalog\", \"Catalog\")")]
        [InlineEditor(Expanded = true)]
        public ResourceCatalog Catalog;

        [FolderPath(AbsolutePath = false, RequireExistingPath = false)]
        [LabelText("@EvoToolsLocalization.Get(\"resource_catalog.catalog_folder\", \"Catalog Folder\")")]
        public string CatalogFolder = DEFAULT_CATALOG_FOLDER;

        [ShowIf(nameof(IsCatalogMissing))]
        [InfoBox("@EvoToolsLocalization.Get(\"resource_catalog.catalog_missing\", \"Catalog not found. You can create one in the default folder.\")", InfoMessageType.Warning)]
        [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"resource_catalog.button.create\", \"Create Catalog\")")]
        private void CreateCatalog()
        {
            var folder = string.IsNullOrEmpty(CatalogFolder) ? DEFAULT_CATALOG_FOLDER : CatalogFolder;
            EnsureFolderExists(folder);

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/ResourceCatalog.asset");
            var asset = CreateInstance<ResourceCatalog>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            Catalog = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [Button(ButtonSizes.Small, Name = "@EvoToolsLocalization.Get(\"resource_catalog.button.find\", \"Find Catalog\")")]
        private void FindCatalog()
        {
            Catalog = FindCatalogInFolder();
            if (Catalog != null)
            {
                EditorGUIUtility.PingObject(Catalog);
            }
        }

        [Title("@EvoToolsLocalization.Get(\"resource_catalog.title.import\", \"Import\")")]
        public ImportType Import = ImportType.Sprite;

        [FolderPath(AbsolutePath = false, RequireExistingPath = true)]
        [LabelText("@EvoToolsLocalization.Get(\"resource_catalog.assets_folder\", \"Assets Folder\")")]
        [Required]
        public string AssetsFolder = "Assets";

        [LabelText("@EvoToolsLocalization.Get(\"resource_catalog.include_subfolders\", \"Include Subfolders\")")]
        public bool IncludeSubfolders = true;

        [Title("@EvoToolsLocalization.Get(\"resource_catalog.title.keys\", \"Keys\")")]
        public KeyMode KeyNaming = KeyMode.RelativePath;

        [LabelText("@EvoToolsLocalization.Get(\"resource_catalog.key_prefix\", \"Key Prefix\")")]
        public string KeyPrefix = "";

        [Title("@EvoToolsLocalization.Get(\"resource_catalog.title.options\", \"Options\")")]
        public bool ClearBeforeImport = false;

        [LabelText("@EvoToolsLocalization.Get(\"resource_catalog.overwrite_existing\", \"Overwrite Existing\")")]
        public bool OverwriteExisting = true;

        [InfoBox("@EvoToolsLocalization.Get(\"resource_catalog.info.sprite_import\", \"Sprite import creates direct sprite entries (not atlas-based).\")", InfoMessageType.Info)]
        [Button(ButtonSizes.Large, Name = "@EvoToolsLocalization.Get(\"resource_catalog.button.import\", \"Import\")")]
        private void ImportAssets()
        {
            if (Catalog == null)
            {
                EvoDebug.LogError(
                    EvoToolsLocalization.Get("resource_catalog.error.no_catalog", "ResourceCatalog is not assigned."),
                    nameof(ResourceCatalogToolWindow));
                return;
            }

            if (string.IsNullOrEmpty(AssetsFolder) || !AssetDatabase.IsValidFolder(AssetsFolder))
            {
                EvoDebug.LogError(
                    EvoToolsLocalization.Get("resource_catalog.error.invalid_folder", "Assets folder is invalid."),
                    nameof(ResourceCatalogToolWindow));
                return;
            }

            var filter = Import switch
            {
                ImportType.Sprite => "t:Sprite",
                ImportType.GameObject => "t:GameObject",
                ImportType.AudioClip => "t:AudioClip",
                _ => "t:Sprite"
            };
            var searchInFolders = new[] { AssetsFolder };
            var guids = AssetDatabase.FindAssets(filter, searchInFolders);

            Undo.RecordObject(Catalog, "Import Assets to Resource Catalog");

            if (ClearBeforeImport)
            {
                Catalog.ClearEntries();
            }

            var added = 0;
            var updated = 0;
            var skipped = 0;

            var existingKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in Catalog.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    existingKeys.Add(entry.Key);
                }
            }

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsValidAssetForImport(assetPath))
                {
                    continue;
                }

                var key = BuildKey(assetPath);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var entry = BuildEntry(key);

                if (existingKeys.Contains(key))
                {
                    if (OverwriteExisting)
                    {
                        Catalog.UpsertEntry(entry);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    Catalog.UpsertEntry(entry);
                    existingKeys.Add(key);
                    added++;
                }
            }

            EditorUtility.SetDirty(Catalog);
            AssetDatabase.SaveAssets();

            EvoDebug.Log(
                string.Format(
                    EvoToolsLocalization.Get("resource_catalog.import_finished", "ResourceCatalog import finished. Added: {0}, Updated: {1}, Skipped: {2}. Folder: {3}"),
                    added,
                    updated,
                    skipped,
                    AssetsFolder),
                nameof(ResourceCatalogToolWindow));
        }

        private bool IsValidAssetForImport(string assetPath)
        {
            if (Import == ImportType.Sprite)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                return sprite != null;
            }

            if (Import == ImportType.AudioClip)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                return clip != null;
            }

            var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return gameObject != null;
        }

        private ResourceCatalogEntry BuildEntry(string key)
        {
            switch (Import)
            {
                case ImportType.GameObject:
                    return new ResourceCatalogEntry
                    {
                        Key = key,
                        Type = ResourceType.GameObject,
                        SpriteType = SpriteEntryType.Direct,
                        AssetKey = key,
                        AtlasKey = string.Empty,
                        SpriteName = string.Empty
                    };
                case ImportType.AudioClip:
                    return new ResourceCatalogEntry
                    {
                        Key = key,
                        Type = ResourceType.AudioClip,
                        SpriteType = SpriteEntryType.Direct,
                        AssetKey = key,
                        AtlasKey = string.Empty,
                        SpriteName = string.Empty
                    };
                case ImportType.Sprite:
                default:
                    return new ResourceCatalogEntry
                    {
                        Key = key,
                        Type = ResourceType.Sprite,
                        SpriteType = SpriteEntryType.Direct,
                        AssetKey = key,
                        AtlasKey = string.Empty,
                        SpriteName = string.Empty
                    };
            }
        }

        private string BuildKey(string assetPath)
        {
            string baseKey;
            switch (KeyNaming)
            {
                case KeyMode.AssetName:
                    baseKey = Path.GetFileNameWithoutExtension(assetPath);
                    break;
                case KeyMode.RelativePath:
                default:
                    var folder = AssetsFolder.Replace('\\', '/').TrimEnd('/');
                    var normalizedPath = assetPath.Replace('\\', '/');
                    if (!normalizedPath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    baseKey = normalizedPath.Substring(folder.Length + 1);
                    baseKey = Path.ChangeExtension(baseKey, null);
                    break;
            }

            var prefix = (KeyPrefix ?? string.Empty).Trim();
            if (prefix.Length > 0)
            {
                return $"{prefix.TrimEnd('/')}/{baseKey}".Replace('\\', '/');
            }

            return baseKey.Replace('\\', '/');
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (string.IsNullOrEmpty(CatalogFolder))
            {
                CatalogFolder = DEFAULT_CATALOG_FOLDER;
            }

            if (Catalog == null)
            {
                Catalog = FindCatalogInFolder();
            }
        }

        private bool IsCatalogMissing()
        {
            return Catalog == null;
        }

        private ResourceCatalog FindCatalogInFolder()
        {
            var folder = string.IsNullOrEmpty(CatalogFolder) ? DEFAULT_CATALOG_FOLDER : CatalogFolder;
            var searchInFolders = new[] { folder };
            var guids = AssetDatabase.FindAssets("t:ResourceCatalog", searchInFolders);
            if (guids.Length == 0)
            {
                return null;
            }

            if (guids.Length > 1)
            {
                EvoDebug.LogWarning(
                    string.Format(
                        EvoToolsLocalization.Get("resource_catalog.warning.multiple_catalogs", "Multiple ResourceCatalog assets found in '{0}'. Using the first one."),
                        folder),
                    nameof(ResourceCatalogToolWindow));
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ResourceCatalog>(assetPath);
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
    }
}
