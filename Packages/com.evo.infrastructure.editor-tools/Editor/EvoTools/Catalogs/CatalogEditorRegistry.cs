using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public static class CatalogEditorRegistry
    {
        private static readonly List<Func<ScriptableObject, ICatalogEditorAdapter>> Factories = new();
        private static List<ICatalogEditorAdapterProvider> _providers;

        public static void Register(Func<ScriptableObject, ICatalogEditorAdapter> factory)
        {
            if (factory != null && !Factories.Contains(factory))
            {
                Factories.Add(factory);
            }
        }

        public static void Register<TCatalog>(Func<TCatalog, ICatalogEditorAdapter> factory)
            where TCatalog : ScriptableObject
        {
            if (factory == null)
            {
                return;
            }

            Register(catalog => catalog is TCatalog typed ? factory(typed) : null);
        }

        public static bool TryCreateAdapter(ScriptableObject catalogAsset, out ICatalogEditorAdapter adapter)
        {
            adapter = null;
            if (catalogAsset == null)
            {
                return false;
            }

            for (var i = Factories.Count - 1; i >= 0; i--)
            {
                adapter = Factories[i]?.Invoke(catalogAsset);
                if (adapter != null)
                {
                    return true;
                }
            }

            var providers = GetProviders();
            for (var i = 0; i < providers.Count; i++)
            {
                if (providers[i].TryCreateAdapter(catalogAsset, out adapter) && adapter != null)
                {
                    return true;
                }
            }

            return DefaultCatalogEditorAdapter.TryCreate(catalogAsset, out adapter);
        }

        private static List<ICatalogEditorAdapterProvider> GetProviders()
        {
            if (_providers != null)
            {
                return _providers;
            }

            _providers = new List<ICatalogEditorAdapterProvider>();
            var providerTypes = TypeCache.GetTypesDerivedFrom<ICatalogEditorAdapterProvider>();
            for (var i = 0; i < providerTypes.Count; i++)
            {
                var type = providerTypes[i];
                if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is ICatalogEditorAdapterProvider provider)
                {
                    _providers.Add(provider);
                }
            }

            return _providers;
        }
    }
}
