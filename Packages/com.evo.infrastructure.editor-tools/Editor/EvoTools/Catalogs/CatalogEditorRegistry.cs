using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    public static class CatalogEditorRegistry
    {
        private static readonly List<Func<ScriptableObject, ICatalogEditorAdapter>> Factories = new();
        private static readonly List<ICatalogEditorAdapterProvider> Providers = new();

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

        public static void RegisterProvider(ICatalogEditorAdapterProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            for (var i = 0; i < Providers.Count; i++)
            {
                if (Providers[i]?.GetType() == provider.GetType())
                {
                    return;
                }
            }

            Providers.Add(provider);
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

            for (var i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].TryCreateAdapter(catalogAsset, out adapter) && adapter != null)
                {
                    return true;
                }
            }

            return DefaultCatalogEditorAdapter.TryCreate(catalogAsset, out adapter);
        }
    }
}
