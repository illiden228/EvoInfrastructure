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
#endif

namespace _Project.Scripts.Infrastructure.Services.Analytics.Config
{
    [CreateAssetMenu(fileName = "AnalyticsAdapterCatalog", menuName = "Project/Analytics/Adapter Catalog")]
    [GameConfig("Analytics")]
    public sealed class AnalyticsAdapterCatalog : ScriptableObject, IGameConfig
    {
        private const string DEFAULT_ADAPTER_ASSETS_FOLDER = "Assets/_Project/Configs/Analytics";
        private const string ASSET_LIST_PATH = "_Project/Configs/Analytics";

#if ODIN_INSPECTOR
        [Title("Storage")]
        [PropertyOrder(100)]
#endif
        [SerializeField] private string adapterAssetsFolder = DEFAULT_ADAPTER_ASSETS_FOLDER;

#if ODIN_INSPECTOR
        [PropertyOrder(-900)]
        [InfoBox("$InvalidAdaptersSummary", InfoMessageType.Error, nameof(HasInvalidAdapters))]
        [Searchable]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true)]
        [AssetList(Path = ASSET_LIST_PATH, AutoPopulate = false)]
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        [OnValueChanged(nameof(SyncEditorFromAdapters), true)]
#endif
        [SerializeField] private List<AnalyticsAdapterConfigBase> adapters = new();

#if ODIN_INSPECTOR
        [Title("Adapters Editor")]
        [PropertyOrder(-800)]
        [LabelText("Adapters (Editor)")]
        [Searchable]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = false, ShowIndexLabels = true, NumberOfItemsPerPage = 20, ListElementLabelName = "name")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [OnValueChanged(nameof(SyncAdaptersFromEditor), true)]
#endif
        [SerializeField] private List<AnalyticsAdapterConfigBase> adaptersEditor = new();

        public IReadOnlyList<AnalyticsAdapterConfigBase> Adapters => adapters;
        public string AdapterAssetsFolder => adapterAssetsFolder;

        public bool TryGet<T>(out T config) where T : AnalyticsAdapterConfigBase
        {
            if (adapters != null)
            {
                for (var i = 0; i < adapters.Count; i++)
                {
                    if (adapters[i] is T typed)
                    {
                        config = typed;
                        return true;
                    }
                }
            }

            config = null;
            return false;
        }

        private bool isSyncing;

        private bool HasInvalidAdapters => GetInvalidAdapterCount() > 0;

        private string InvalidAdaptersSummary
        {
            get
            {
                if (!HasInvalidAdapters)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                builder.Append("Invalid adapters: ");
                var first = true;
                for (var i = 0; i < adapters.Count; i++)
                {
                    var adapter = adapters[i];
                    if (adapter != null && !string.IsNullOrWhiteSpace(adapter.AdapterId))
                    {
                        continue;
                    }

                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(adapter == null ? "<null>" : adapter.name);
                    first = false;
                }

                builder.Append(". Assign AdapterId in each config.");
                return builder.ToString();
            }
        }

        private int GetInvalidAdapterCount()
        {
            if (adapters == null || adapters.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < adapters.Count; i++)
            {
                var adapter = adapters[i];
                if (adapter == null || string.IsNullOrWhiteSpace(adapter.AdapterId))
                {
                    count++;
                }
            }

            return count;
        }

        private void OnEnable()
        {
            SyncEditorFromAdapters();
        }

        private void SyncEditorFromAdapters()
        {
            if (isSyncing)
            {
                return;
            }

            isSyncing = true;
            adaptersEditor.Clear();
            for (var i = 0; i < adapters.Count; i++)
            {
                adaptersEditor.Add(adapters[i]);
            }
            isSyncing = false;
        }

        private void SyncAdaptersFromEditor()
        {
            if (isSyncing)
            {
                return;
            }

            isSyncing = true;
            adapters.Clear();
            for (var i = 0; i < adaptersEditor.Count; i++)
            {
                adapters.Add(adaptersEditor[i]);
            }
            isSyncing = false;
        }

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [Title("Adapters")]
        [PropertyOrder(-1000)]
        [Button(ButtonSizes.Medium, Name = "Create Adapter Config")]
#endif
        private void CreateAdapterConfig()
        {
            AnalyticsAdapterConfigPromptWindow.Open(this);
        }

        private void CreateAdapterConfig(Type adapterType, string typedName, bool allowDefaultName)
        {
            if (adapterType == null || !typeof(AnalyticsAdapterConfigBase).IsAssignableFrom(adapterType))
            {
                return;
            }

            var baseName = typedName?.Trim();
            if (string.IsNullOrEmpty(baseName))
            {
                if (!allowDefaultName)
                {
                    return;
                }

                baseName = adapterType.Name;
            }

            var folder = adapterAssetsFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = DEFAULT_ADAPTER_ASSETS_FOLDER;
            }

            EnsureFolderExists(folder);

            if (!AssetDatabase.IsValidFolder(folder))
            {
                folder = DEFAULT_ADAPTER_ASSETS_FOLDER;
                EnsureFolderExists(folder);
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                folder = "Assets/_Project/Configs";
                EnsureFolderExists(folder);
            }

            var asset = CreateInstance(adapterType) as AnalyticsAdapterConfigBase;
            if (asset == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!adapters.Contains(asset))
            {
                adapters.Add(asset);
            }

            SyncEditorFromAdapters();
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

        private sealed class AnalyticsAdapterConfigPromptWindow : EditorWindow
        {
            private AnalyticsAdapterCatalog _owner;
            private string _configName = string.Empty;
            private int _selectedIndex;
            private List<Type> _adapterTypes = new();

            public static void Open(AnalyticsAdapterCatalog owner)
            {
                if (owner == null)
                {
                    return;
                }

                var window = CreateInstance<AnalyticsAdapterConfigPromptWindow>();
                window._owner = owner;
                window.titleContent = new GUIContent("Create Adapter Config");
                window.minSize = new Vector2(360f, 140f);
                window.maxSize = new Vector2(520f, 180f);
                window.ShowUtility();
            }

            private void OnEnable()
            {
                _adapterTypes.Clear();
                foreach (var type in TypeCache.GetTypesDerivedFrom<AnalyticsAdapterConfigBase>())
                {
                    if (type == null || type.IsAbstract)
                    {
                        continue;
                    }

                    _adapterTypes.Add(type);
                }

                _adapterTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField("Adapter Type", EditorStyles.boldLabel);
                var typeNames = _adapterTypes.ConvertAll(t => t.Name).ToArray();
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _adapterTypes.Count - 1));
                _selectedIndex = EditorGUILayout.Popup(_selectedIndex, typeNames);

                GUILayout.Space(8f);
                EditorGUILayout.LabelField("Asset Name", EditorStyles.boldLabel);
                _configName = EditorGUILayout.TextField(_configName);
                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create"))
                {
                    var type = _adapterTypes.Count > 0 ? _adapterTypes[_selectedIndex] : null;
                    _owner?.CreateAdapterConfig(type, _configName, allowDefaultName: false);
                    Close();
                }

                if (GUILayout.Button("Without Name"))
                {
                    var type = _adapterTypes.Count > 0 ? _adapterTypes[_selectedIndex] : null;
                    _owner?.CreateAdapterConfig(type, _configName, allowDefaultName: true);
                    Close();
                }

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
#endif
    }
}
