using System;
using System.Collections;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Ads.Config;
using Evo.Infrastructure.Services.Analytics.Config;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    [InitializeOnLoad]
    internal sealed class InfrastructureCatalogEditorAdapterProvider : ICatalogEditorAdapterProvider
    {
        static InfrastructureCatalogEditorAdapterProvider()
        {
            CatalogEditorRegistry.RegisterProvider(new InfrastructureCatalogEditorAdapterProvider());
        }

        public bool TryCreateAdapter(ScriptableObject catalogAsset, out ICatalogEditorAdapter adapter)
        {
            adapter = catalogAsset switch
            {
                AdsAdapterCatalog catalog => CreateSingleListAdapter(
                    catalog,
                    "ads_adapters",
                    "Ads Adapters",
                    typeof(AdsAdapterConfigBase),
                    () => catalog.Adapters as IList,
                    catalog.AdapterAssetsFolder,
                    type => type?.Name ?? "AdsAdapterConfig",
                    catalog.ValidateCatalog),
                AnalyticsAdapterCatalog catalog => CreateSingleListAdapter(
                    catalog,
                    "analytics_adapters",
                    "Analytics Adapters",
                    typeof(AnalyticsAdapterConfigBase),
                    () => catalog.Adapters as IList,
                    catalog.AdapterAssetsFolder,
                    type => type?.Name ?? "AnalyticsAdapterConfig",
                    catalog.ValidateCatalog),
                PlatformCatalog catalog => CreateSingleListAdapter(
                    catalog,
                    "platforms",
                    "Platforms",
                    typeof(PlatformDefinition),
                    () => catalog.Entries as IList,
                    catalog.PlatformAssetsFolder,
                    _ => "Platform",
                    catalog.ValidateCatalog),
                _ => null
            };

            return adapter != null;
        }

        private static ICatalogEditorAdapter CreateSingleListAdapter(
            ScriptableObject catalogAsset,
            string id,
            string name,
            Type itemType,
            Func<IList> getMutableList,
            string defaultDirectory,
            Func<Type, string> buildAssetBaseName,
            Func<CatalogValidationResult> validate)
        {
            if (getMutableList?.Invoke() == null)
            {
                return null;
            }

            var category = new CatalogCategoryDescriptor
            {
                Id = id,
                Name = name,
                ItemType = itemType,
                DefaultCreateDirectory = string.IsNullOrWhiteSpace(defaultDirectory) ? "Assets/_Project/Configs" : defaultDirectory,
                Contains = _ => true,
                GetMutableList = getMutableList,
                BuildAssetBaseName = buildAssetBaseName,
                BuildSuggestedId = (assetName, _) => DefaultCatalogEditorAdapter.NormalizeId(assetName),
                GetCreatableTypes = DefaultCatalogEditorAdapter.GetCreatableTypes,
                KeyProvider = new SerializedCatalogItemKeyProvider()
            };

            return new SingleListCatalogEditorAdapter(catalogAsset, category, validate);
        }

        private sealed class SingleListCatalogEditorAdapter : ICatalogEditorAdapter
        {
            private readonly IReadOnlyList<CatalogCategoryDescriptor> _categories;
            private readonly Func<CatalogValidationResult> _validate;

            public SingleListCatalogEditorAdapter(
                ScriptableObject catalogAsset,
                CatalogCategoryDescriptor category,
                Func<CatalogValidationResult> validate)
            {
                CatalogAsset = catalogAsset;
                Title = $"{catalogAsset.name} Catalog";
                _categories = new[] { category };
                _validate = validate;
            }

            public ScriptableObject CatalogAsset { get; }
            public string Title { get; }
            public IReadOnlyList<CatalogCategoryDescriptor> Categories => _categories;

            public CatalogValidationResult Validate()
            {
                var result = CatalogEditorValidation.Validate(this);
                var custom = _validate?.Invoke();
                if (custom == null)
                {
                    return result;
                }

                for (var i = 0; i < custom.Errors.Count; i++)
                {
                    AddUniqueError(result, custom.Errors[i]);
                }

                for (var i = 0; i < custom.Warnings.Count; i++)
                {
                    AddUniqueWarning(result, custom.Warnings[i]);
                }

                return result;
            }

            private static void AddUniqueError(CatalogValidationResult result, string message)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                for (var i = 0; i < result.Errors.Count; i++)
                {
                    if (string.Equals(result.Errors[i], message, StringComparison.Ordinal))
                    {
                        return;
                    }
                }

                result.AddError(message);
            }

            private static void AddUniqueWarning(CatalogValidationResult result, string message)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                for (var i = 0; i < result.Warnings.Count; i++)
                {
                    if (string.Equals(result.Warnings[i], message, StringComparison.Ordinal))
                    {
                        return;
                    }
                }

                result.AddWarning(message);
            }
        }
    }
}
