using System;
using System.Collections.Generic;
using System.Text;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using Evo.Infrastructure.Services.Config;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Evo.Infrastructure.Services.Ads.Config
{
    [CreateAssetMenu(fileName = "AdsAdapterCatalog", menuName = "Project/Ads/Adapter Catalog")]
    [GameConfig("Ads")]
    public sealed class AdsAdapterCatalog : ScriptableObject, IGameConfig
    {
        private const string DEFAULT_ADAPTER_ASSETS_FOLDER = "Assets/_Project/Configs/Ads";
        private const string ASSET_LIST_PATH = "_Project/Configs/Ads";

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
#endif
        [SerializeField] private List<AdsAdapterConfigBase> adapters = new();

        public IReadOnlyList<AdsAdapterConfigBase> Adapters => adapters;
        public string AdapterAssetsFolder => adapterAssetsFolder;

        public bool TryGet<T>(out T config) where T : AdsAdapterConfigBase
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

        public CatalogValidationResult ValidateCatalog()
        {
            var result = new CatalogValidationResult();
            if (adapters == null)
            {
                result.AddError("Adapters list is null.");
                return result;
            }

            var ids = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < adapters.Count; i++)
            {
                var adapter = adapters[i];
                if (adapter == null)
                {
                    result.AddError($"Null adapter at index {i}.");
                    continue;
                }

                var adapterId = adapter.AdapterId?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(adapterId))
                {
                    result.AddError($"Empty adapter id at index {i} ({adapter.name}).");
                    continue;
                }

                if (!ids.TryGetValue(adapterId, out var indices))
                {
                    indices = new List<int>();
                    ids[adapterId] = indices;
                }

                indices.Add(i);
            }

            foreach (var pair in ids)
            {
                if (pair.Value.Count > 1)
                {
                    result.AddError($"Duplicate adapter id '{pair.Key}' at indices: {string.Join(", ", pair.Value)}.");
                }
            }

            return result;
        }

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

    }
}
