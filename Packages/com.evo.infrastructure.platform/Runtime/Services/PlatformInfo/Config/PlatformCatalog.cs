using System;
using System.Collections.Generic;
using System.Text;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using Evo.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Evo.Infrastructure.Services.PlatformInfo.Config
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
#endif
        [SerializeField] private List<PlatformDefinition> platforms = new();

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

        public CatalogValidationResult ValidateCatalog()
        {
            var result = new CatalogValidationResult();
            if (platforms == null)
            {
                result.AddError("Platforms list is null.");
                return result;
            }

            var ids = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < platforms.Count; i++)
            {
                var platform = platforms[i];
                if (platform == null)
                {
                    result.AddError($"Null platform at index {i}.");
                    continue;
                }

                var platformId = platform.PlatformId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(platformId))
                {
                    result.AddError($"Empty platform id at index {i} ({platform.name}).");
                    continue;
                }

                if (!ids.TryGetValue(platformId, out var indices))
                {
                    indices = new List<int>();
                    ids[platformId] = indices;
                }

                indices.Add(i);
            }

            foreach (var pair in ids)
            {
                if (pair.Value.Count > 1)
                {
                    result.AddError($"Duplicate platform id '{pair.Key}' at indices: {string.Join(", ", pair.Value)}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(currentPlatformId) && !ContainsPlatformId(currentPlatformId))
            {
                result.AddWarning($"Current platform id '{currentPlatformId}' is not present in platforms.");
            }

            if (!string.IsNullOrWhiteSpace(defaultPlatformId) && !ContainsPlatformId(defaultPlatformId))
            {
                result.AddWarning($"Default platform id '{defaultPlatformId}' is not present in platforms.");
            }

            return result;
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

        private bool ContainsPlatformId(string platformId)
        {
            if (string.IsNullOrWhiteSpace(platformId) || platforms == null)
            {
                return false;
            }

            for (var i = 0; i < platforms.Count; i++)
            {
                var platform = platforms[i];
                if (platform != null && string.Equals(platform.PlatformId, platformId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        public void SetCurrentPlatformId(string platformId)
        {
            currentPlatformId = platformId ?? string.Empty;
            EditorUtility.SetDirty(this);
        }

#endif
    }
}
