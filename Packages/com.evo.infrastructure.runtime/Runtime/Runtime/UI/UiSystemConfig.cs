using System;
using System.Collections.Generic;
using System.IO;
using _Project.Scripts.Application.UI.Views;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using EvoDebug = _Project.Scripts.Infrastructure.Services.Debug.EvoDebug;
#endif

namespace _Project.Scripts.Application.UI
{
    public enum UiLayerBuildMode
    {
        SharedCanvas = 0,
        OwnCanvas = 1
    }

    [Serializable]
    public sealed class UiCanvasScalerSettings
    {
        public CanvasScaler.ScaleMode UiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        public Vector2 ReferenceResolution = new(1920f, 1080f);
        public CanvasScaler.ScreenMatchMode ScreenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        [Range(0f, 1f)] public float MatchWidthOrHeight = 0.5f;
        public float ScaleFactor = 1f;
        public float ReferencePixelsPerUnit = 100f;
    }

    [Serializable]
    public sealed class UiLayerDefinition
    {
        public string Name = "Hud";
        public UiLayerBuildMode BuildMode = UiLayerBuildMode.SharedCanvas;
        public int SortingOrder = 0;
        public bool OverrideCanvasScaler;
        public UiCanvasScalerSettings CanvasScaler = new();
    }

    [Serializable]
    public sealed class UiViewEntry
    {
#if ODIN_INSPECTOR
        [ValidateInput(nameof(IsIdUnique), "Duplicate Id", InfoMessageType.Error)]
#endif
        public string Id;
        public AssetReference ViewPrefab;
#if ODIN_INSPECTOR
        [ValidateInput(nameof(IsViewTypeUnique), "Duplicate ViewType", InfoMessageType.Error)]
        [ReadOnly]
#endif
        public string ViewTypeName;
#if ODIN_INSPECTOR
        [ValidateInput(nameof(IsViewModelSet), "Missing ViewModelType", InfoMessageType.Error)]
        [ReadOnly]
#endif
        public string ViewModelTypeName;
        public UiLayer Layer = UiLayer.Hud;
        public UiOpenMode OpenMode = UiOpenMode.Queue;
        public bool IsSceneView;
        public bool KeepAlive;
        public bool KeepHistory;

        [NonSerialized] internal UiSystemConfig Owner;

        public Type GetViewModelType()
        {
            if (string.IsNullOrEmpty(ViewModelTypeName))
            {
                return null;
            }

            return Type.GetType(ViewModelTypeName);
        }

        public Type GetViewType()
        {
            if (string.IsNullOrEmpty(ViewTypeName))
            {
                return null;
            }

            return Type.GetType(ViewTypeName);
        }

        private bool IsIdUnique()
        {
            return Owner == null || Owner.IsIdUnique(this);
        }

        private bool IsViewTypeUnique()
        {
            return Owner == null || Owner.IsViewTypeUnique(this);
        }

        private bool IsViewModelSet()
        {
            return !string.IsNullOrEmpty(ViewModelTypeName);
        }
    }

    [CreateAssetMenu(fileName = "UiSystemConfig", menuName = "Project/UI System Config")]
    public sealed class UiSystemConfig : ScriptableObject
    {
#if ODIN_INSPECTOR
        [Title("Auto Setup")]
#endif
        [SerializeField] private string viewsFolder = "Assets/_Project/Prefabs/UI";
        [SerializeField] private List<string> ignoredPostfixes = new() { "_Old" };

#if ODIN_INSPECTOR
        [Title("Layers")]
#endif
        [SerializeField] private UiCanvasScalerSettings sharedCanvasScaler = new();
        [SerializeField] private List<UiLayerDefinition> layers = new();

#if ODIN_INSPECTOR
        [Title("Views")]
        [Searchable]
#endif
        [SerializeField] private List<UiViewEntry> views = new();

        public string ViewsFolder => viewsFolder;
        public IReadOnlyList<string> IgnoredPostfixes => ignoredPostfixes;
        public UiCanvasScalerSettings SharedCanvasScaler => sharedCanvasScaler;
        public IReadOnlyList<UiLayerDefinition> Layers => layers;
        public IReadOnlyList<UiViewEntry> Views => views;

#if UNITY_EDITOR
        private void OnEnable()
        {
            AssignOwners();
        }

        private void OnValidate()
        {
            AssignOwners();
        }
#endif

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [Button(ButtonSizes.Medium, Name = "Rebuild Views")]
#endif
        private void RebuildViews()
        {
            if (string.IsNullOrEmpty(viewsFolder))
            {
                return;
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { viewsFolder });
            var newViews = new List<UiViewEntry>();
            var sceneViews = new List<UiViewEntry>();
            var prefabGuidSet = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && views[i].IsSceneView)
                {
                    sceneViews.Add(views[i]);
                }
            }

            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(path);
                if (IsIgnored(name))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || prefabGuidSet.Contains(guid))
                {
                    continue;
                }
                prefabGuidSet.Add(guid);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                var view = GetRootView(prefab);
                if (view == null)
                {
                    continue;
                }

                var viewType = view.GetType();
                var viewModelType = GetViewModelTypeFromView(viewType);
                if (viewModelType == null)
                {
                    continue;
                }

                var entry = new UiViewEntry
                {
                    Id = name,
                    ViewPrefab = new AssetReference(guid),
                    ViewTypeName = viewType.AssemblyQualifiedName,
                    ViewModelTypeName = viewModelType.AssemblyQualifiedName,
                    Layer = UiLayer.Window,
                    OpenMode = UiOpenMode.Queue
                };

                newViews.Add(entry);
            }

            newViews.AddRange(sceneViews);

            WarnDuplicates(newViews);

            views = newViews;
            AssignOwners();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

#if ODIN_INSPECTOR
        [Button(ButtonSizes.Medium, Name = "Add Views")]
#endif
        private void AddViews()
        {
            if (string.IsNullOrEmpty(viewsFolder))
            {
                return;
            }

            if (views == null)
            {
                views = new List<UiViewEntry>();
            }

            var existingGuids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < views.Count; i++)
            {
                var entry = views[i];
                var guid = entry?.ViewPrefab?.AssetGUID;
                if (!string.IsNullOrEmpty(guid))
                {
                    existingGuids.Add(guid);
                }
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { viewsFolder });
            var added = 0;
            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(path);
                if (IsIgnored(name))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || existingGuids.Contains(guid))
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                var view = GetRootView(prefab);
                if (view == null)
                {
                    continue;
                }

                var viewType = view.GetType();
                var viewModelType = GetViewModelTypeFromView(viewType);
                if (viewModelType == null)
                {
                    continue;
                }

                views.Add(new UiViewEntry
                {
                    Id = name,
                    ViewPrefab = new AssetReference(guid),
                    ViewTypeName = viewType.AssemblyQualifiedName,
                    ViewModelTypeName = viewModelType.AssemblyQualifiedName,
                    Layer = UiLayer.Window,
                    OpenMode = UiOpenMode.Queue
                });
                existingGuids.Add(guid);
                added++;
            }

            if (added == 0)
            {
                return;
            }

            WarnDuplicates(views);
            AssignOwners();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void RebuildViewsFromMenu()
        {
            RebuildViews();
        }

#if ODIN_INSPECTOR
        [Button(ButtonSizes.Medium, Name = "Generate UiLayer Enum")]
#endif
        private void GenerateUiLayerEnum()
        {
            var path = "Assets/_Project/Scripts/Runtime/UI/UiLayer.cs";
            var builder = new StringBuilder();
            builder.AppendLine("namespace _Project.Scripts.Application.UI");
            builder.AppendLine("{");
            builder.AppendLine("    public enum UiLayer");
            builder.AppendLine("    {");

            if (layers == null || layers.Count == 0)
            {
                builder.AppendLine("        Hud = 0");
            }
            else
            {
                var index = 0;
                for (var i = 0; i < layers.Count; i++)
                {
                    var name = SanitizeEnumName(layers[i]?.Name, i);
                    builder.AppendLine($"        {name} = {index},");
                    index++;
                }

                builder.AppendLine("        Unknown = 999");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            File.WriteAllText(path, builder.ToString());
            AssetDatabase.ImportAsset(path);
        }

        private bool IsIgnored(string name)
        {
            if (ignoredPostfixes == null || ignoredPostfixes.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < ignoredPostfixes.Count; i++)
            {
                var postfix = ignoredPostfixes[i];
                if (!string.IsNullOrEmpty(postfix) && name.EndsWith(postfix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string SanitizeEnumName(string rawName, int index)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                return $"Layer{index}";
            }

            var builder = new StringBuilder(rawName.Length);
            for (var i = 0; i < rawName.Length; i++)
            {
                var c = rawName[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    builder.Append(c);
                }
            }

            if (builder.Length == 0 || char.IsDigit(builder[0]))
            {
                builder.Insert(0, "Layer");
            }

            return builder.ToString();
        }

        private static Type GetViewModelTypeFromView(Type viewType)
        {
            while (viewType != null)
            {
                if (viewType.IsGenericType && viewType.GetGenericTypeDefinition() == typeof(Views.UiView<>))
                {
                    var args = viewType.GetGenericArguments();
                    if (args.Length == 1)
                    {
                        return args[0];
                    }
                }

                viewType = viewType.BaseType;
            }

            return null;
        }

        private static UiViewBase GetRootView(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            var views = prefab.GetComponentsInChildren<Views.UiViewBase>(true);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    continue;
                }

                var parentView = view.transform.parent != null
                    ? view.transform.parent.GetComponentInParent<Views.UiViewBase>()
                    : null;
                if (parentView == null)
                {
                    return view;
                }
            }

            return null;
        }

        private static void WarnDuplicates(IReadOnlyList<UiViewEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            var viewTypeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var idCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(entry.ViewTypeName))
                {
                    viewTypeCounts.TryGetValue(entry.ViewTypeName, out var count);
                    viewTypeCounts[entry.ViewTypeName] = count + 1;
                }

                if (!string.IsNullOrEmpty(entry.Id))
                {
                    idCounts.TryGetValue(entry.Id, out var count);
                    idCounts[entry.Id] = count + 1;
                }
            }

            foreach (var kvp in viewTypeCounts)
            {
                if (kvp.Value > 1)
                {
                    EvoDebug.LogWarning($"Duplicate ViewType found: {kvp.Key} (count {kvp.Value}).", nameof(UiSystemConfig));
                }
            }

            foreach (var kvp in idCounts)
            {
                if (kvp.Value > 1)
                {
                    EvoDebug.LogWarning($"Duplicate View Id found: {kvp.Key} (count {kvp.Value}).", nameof(UiSystemConfig));
                }
            }
        }
#endif

        private void AssignOwners()
        {
            if (views == null)
            {
                return;
            }

            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null)
                {
                    views[i].Owner = this;
                }
            }
        }

        internal bool IsIdUnique(UiViewEntry entry)
        {
            if (entry == null || views == null || string.IsNullOrEmpty(entry.Id))
            {
                return true;
            }

            var count = 0;
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && string.Equals(views[i].Id, entry.Id, StringComparison.Ordinal))
                {
                    count++;
                    if (count > 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal bool IsViewTypeUnique(UiViewEntry entry)
        {
            if (entry == null || views == null || string.IsNullOrEmpty(entry.ViewTypeName))
            {
                return true;
            }

            var count = 0;
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && string.Equals(views[i].ViewTypeName, entry.ViewTypeName, StringComparison.Ordinal))
                {
                    count++;
                    if (count > 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
