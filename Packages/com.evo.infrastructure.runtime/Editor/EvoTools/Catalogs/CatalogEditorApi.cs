using System;
using System.Collections;
using System.Collections.Generic;
using Evo.Infrastructure.Runtime.Config.Catalogs;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public interface ICatalogEditorAdapter
    {
        ScriptableObject CatalogAsset { get; }
        string Title { get; }
        IReadOnlyList<CatalogCategoryDescriptor> Categories { get; }
        CatalogValidationResult Validate();
    }

    public interface ICatalogEditorAdapterProvider
    {
        bool TryCreateAdapter(ScriptableObject catalogAsset, out ICatalogEditorAdapter adapter);
    }

    public sealed class CatalogCategoryDescriptor
    {
        public string Id;
        public string Name;
        public Type ItemType;
        public string DefaultCreateDirectory;
        public Func<UnityEngine.Object, bool> Contains;
        public Func<IList> GetMutableList;
        public Func<Type, string> BuildAssetBaseName;
        public Func<Type, IReadOnlyList<Type>> GetCreatableTypes;
        public ICatalogItemKeyProvider KeyProvider;

        public string SettingsKey(ScriptableObject catalogAsset)
        {
            var catalogKey = catalogAsset == null ? "UnknownCatalog" : catalogAsset.GetType().FullName;
            return $"{catalogKey}.{Id ?? Name ?? ItemType?.Name ?? "category"}.directory";
        }
    }

    public interface ICatalogItemKeyProvider
    {
        CatalogItemKey GetKey(UnityEngine.Object item, int index);
        string GetDisplayName(UnityEngine.Object item, int index);
        bool CanSetKey(UnityEngine.Object item);
        bool TrySetKey(UnityEngine.Object item, string value);
    }

    public readonly struct CatalogItemKey
    {
        public CatalogItemKey(string value, CatalogItemKeySource source, bool isStable)
        {
            Value = value ?? string.Empty;
            Source = source;
            IsStable = isStable;
        }

        public string Value { get; }
        public CatalogItemKeySource Source { get; }
        public bool IsStable { get; }
    }

    public enum CatalogItemKeySource
    {
        Explicit,
        Custom,
        AssetName,
        Index,
        Missing
    }
}
