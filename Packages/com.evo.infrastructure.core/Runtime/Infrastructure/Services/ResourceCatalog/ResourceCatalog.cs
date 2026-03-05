using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.ResourceCatalog
{
    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Project/Resource Catalog")]
    public sealed class ResourceCatalog : ScriptableObject, IResourceCatalog
    {
        [SerializeField] private List<ResourceCatalogEntry> _entries = new();

        private Dictionary<string, ResourceCatalogEntry> _lookup;

        public IReadOnlyList<ResourceCatalogEntry> Entries => _entries;

        public bool TryGetEntry(string key, out ResourceCatalogEntry entry)
        {
            EnsureLookup();
            return _lookup.TryGetValue(key, out entry);
        }

        public bool TryGetEntry(string key, ResourceType type, out ResourceCatalogEntry entry)
        {
            if (TryGetEntry(key, out entry))
            {
                return entry.Type == type;
            }

            return false;
        }

#if UNITY_EDITOR
        public void UpsertEntry(ResourceCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Key))
            {
                return;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Key == entry.Key)
                {
                    _entries[i] = entry;
                    _lookup = null;
                    return;
                }
            }

            _entries.Add(entry);
            _lookup = null;
        }

        public void ClearEntries()
        {
            _entries.Clear();
            _lookup = null;
        }
#endif

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, ResourceCatalogEntry>(StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                _lookup[entry.Key] = entry;
            }
        }
    }
}
