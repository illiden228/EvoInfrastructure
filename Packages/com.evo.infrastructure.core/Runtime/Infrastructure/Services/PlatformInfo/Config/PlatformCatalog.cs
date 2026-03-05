using System;
using System.Collections.Generic;
using System.Text;
using _Project.Scripts.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace _Project.Scripts.Infrastructure.Services.PlatformInfo.Config
{
    [CreateAssetMenu(fileName = "PlatformCatalog", menuName = "Project/Platform/Platform Catalog")]
    [GameConfig("Platform")]
    public sealed class PlatformCatalog : ScriptableObject, IGameConfig
    {
        private const string DEFAULT_PLATFORM_ASSETS_FOLDER = "Assets/_Project/Configs/Platform";
        private const string ASSET_LIST_PATH = "_Project/Configs/Platform";

#if ODIN_INSPECTOR
        [Title("Storage")]
        [PropertyOrder(100)]
#endif
        [SerializeField] private string platformAssetsFolder = DEFAULT_PLATFORM_ASSETS_FOLDER;

#if ODIN_INSPECTOR
        [PropertyOrder(-900)]
        [InfoBox("$InvalidPlatformsSummary", InfoMessageType.Error, nameof(HasInvalidPlatforms))]
        [Searchable]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
        [AssetList(Path = ASSET_LIST_PATH, AutoPopulate = false)]
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        [OnValueChanged(nameof(SyncEditorFromPlatforms), true)]
#endif
        [SerializeField] private List<PlatformDefinition> platforms = new();

#if ODIN_INSPECTOR
        [Title("Platforms Editor")]
        [PropertyOrder(-800)]
        [LabelText("Platforms (Editor)")]
        [Searchable]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, NumberOfItemsPerPage = 20, ListElementLabelName = "name")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [OnValueChanged(nameof(SyncPlatformsFromEditor), true)]
#endif
        [SerializeField] private List<PlatformDefinition> platformsEditor = new();

#if ODIN_INSPECTOR
        [Title("Selection")]
        [PropertyOrder(70)]
#endif
        [SerializeField] private string defaultPlatformId = "default";
        [CatalogDropdown(CatalogDropdownKind.PlatformId)]
        [SerializeField] private string currentPlatformId;

        public string PlatformAssetsFolder => platformAssetsFolder;
        public string DefaultPlatformId => defaultPlatformId;
        public string CurrentPlatformId => currentPlatformId;
        public IReadOnlyList<PlatformDefinition> Entries => platforms;

        private bool isSyncing;

        private bool HasInvalidPlatforms => GetInvalidPlatformCount() > 0;

        private string InvalidPlatformsSummary
        {
            get
            {
                if (!HasInvalidPlatforms)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                builder.Append("Invalid platforms: ");
                var first = true;
                for (var i = 0; i < platforms.Count; i++)
                {
                    var platform = platforms[i];
                    if (platform != null && !string.IsNullOrWhiteSpace(platform.PlatformId))
                    {
                        continue;
                    }

                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(platform == null ? "<null>" : platform.name);
                    first = false;
                }

                builder.Append(". Assign PlatformId in each platform asset.");
                return builder.ToString();
            }
        }

        public bool TryGetByDefine(string define, out PlatformDefinition entry)
        {
            if (string.IsNullOrWhiteSpace(define))
            {
                entry = null;
                return false;
            }

            for (var i = 0; i < platforms.Count; i++)
            {
                var platform = platforms[i];
                if (platform == null || platform.Defines == null || platform.Defines.Count == 0)
                {
                    continue;
                }

                if (ContainsDefine(platform.Defines, define))
                {
                    entry = platform;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        private int GetInvalidPlatformCount()
        {
            if (platforms == null || platforms.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < platforms.Count; i++)
            {
                var platform = platforms[i];
                if (platform == null || string.IsNullOrWhiteSpace(platform.PlatformId))
                {
                    count++;
                }
            }

            return count;
        }

        private void OnEnable()
        {
            SyncEditorFromPlatforms();
        }

        private void SyncEditorFromPlatforms()
        {
            if (isSyncing)
            {
                return;
            }

            isSyncing = true;
            platformsEditor.Clear();
            for (var i = 0; i < platforms.Count; i++)
            {
                platformsEditor.Add(platforms[i]);
            }
            isSyncing = false;
        }

        private void SyncPlatformsFromEditor()
        {
            if (isSyncing)
            {
                return;
            }

            isSyncing = true;
            platforms.Clear();
            for (var i = 0; i < platformsEditor.Count; i++)
            {
                platforms.Add(platformsEditor[i]);
            }
            isSyncing = false;
        }

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [Title("Platforms")]
        [PropertyOrder(-1000)]
        [Button(ButtonSizes.Medium, Name = "Create Platform")]
#endif
        private void CreatePlatform()
        {
            PlatformDefinitionPromptWindow.Open(this);
        }

        [ContextMenu("Sync From Defines")]
        public void SyncFromDefines()
        {
            var defines = GetActiveDefines();
            ApplyDefines(defines);
        }

        public void ApplyDefines(string defineSymbols)
        {
            var defineSet = SplitDefines(defineSymbols);
            for (var i = 0; i < platforms.Count; i++)
            {
                var platform = platforms[i];
                if (platform == null || platform.Defines == null || platform.Defines.Count == 0)
                {
                    continue;
                }

                if (ContainsAnyDefine(platform.Defines, defineSet))
                {
                    currentPlatformId = platform.PlatformId;
                    EditorUtility.SetDirty(this);
                    return;
                }
            }

            currentPlatformId = defaultPlatformId;
            EditorUtility.SetDirty(this);
        }

        private static string GetActiveDefines()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            return PlayerSettings.GetScriptingDefineSymbols(namedTarget);
        }

        private sealed class PlatformDefinitionPromptWindow : EditorWindow
        {
            private PlatformCatalog _owner;
            private string _assetName = string.Empty;

            public static void Open(PlatformCatalog owner)
            {
                if (owner == null)
                {
                    return;
                }

                var window = CreateInstance<PlatformDefinitionPromptWindow>();
                window._owner = owner;
                window.titleContent = new GUIContent("Create Platform");
                window.minSize = new Vector2(340f, 120f);
                window.maxSize = new Vector2(520f, 160f);
                window.ShowUtility();
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField("Asset Name", EditorStyles.boldLabel);
                _assetName = EditorGUILayout.TextField(_assetName);
                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create"))
                {
                    _owner?.CreatePlatformWithName(_assetName, allowDefaultName: false);
                    Close();
                }

                if (GUILayout.Button("Without Name"))
                {
                    _owner?.CreatePlatformWithName(_assetName, allowDefaultName: true);
                    Close();
                }

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void CreatePlatformWithName(string typedName, bool allowDefaultName)
        {
            var baseName = typedName?.Trim();
            if (string.IsNullOrEmpty(baseName))
            {
                if (!allowDefaultName)
                {
                    return;
                }

                baseName = "Platform";
            }

            var folder = platformAssetsFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = DEFAULT_PLATFORM_ASSETS_FOLDER;
            }

            EnsureFolderExists(folder);

            if (!AssetDatabase.IsValidFolder(folder))
            {
                folder = DEFAULT_PLATFORM_ASSETS_FOLDER;
                EnsureFolderExists(folder);
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                folder = "Assets/_Project/Configs";
                EnsureFolderExists(folder);
            }

            var asset = CreateInstance<PlatformDefinition>();
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!platforms.Contains(asset))
            {
                platforms.Add(asset);
            }

            SyncEditorFromPlatforms();
            EditorUtility.SetDirty(this);
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
#endif

        private static HashSet<string> SplitDefines(string defineSymbols)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(defineSymbols))
            {
                return set;
            }

            var parts = defineSymbols.Split(';');
            for (var i = 0; i < parts.Length; i++)
            {
                var define = parts[i]?.Trim();
                if (!string.IsNullOrEmpty(define))
                {
                    set.Add(define);
                }
            }

            return set;
        }

        private static bool ContainsDefine(IReadOnlyList<string> defines, string define)
        {
            if (defines == null || defines.Count == 0 || string.IsNullOrWhiteSpace(define))
            {
                return false;
            }

            for (var i = 0; i < defines.Count; i++)
            {
                if (string.Equals(defines[i], define, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAnyDefine(IReadOnlyList<string> defines, HashSet<string> defineSet)
        {
            if (defines == null || defines.Count == 0 || defineSet == null || defineSet.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < defines.Count; i++)
            {
                var define = defines[i];
                if (!string.IsNullOrWhiteSpace(define) && defineSet.Contains(define))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
