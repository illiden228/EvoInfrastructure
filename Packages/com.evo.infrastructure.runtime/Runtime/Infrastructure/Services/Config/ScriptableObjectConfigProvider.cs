using System;
using System.Collections.Generic;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    public sealed class ScriptableObjectConfigProvider : IConfigProvider
    {
        private readonly Dictionary<string, object> _configs = new(StringComparer.Ordinal);

        public ScriptableObjectConfigProvider(IReadOnlyList<ScriptableConfigCatalog> catalogs)
        {
            if (catalogs == null)
            {
                return;
            }

            for (var i = 0; i < catalogs.Count; i++)
            {
                var catalog = catalogs[i];
                if (catalog == null)
                {
                    continue;
                }

                var entries = catalog.Entries;
                for (var j = 0; j < entries.Count; j++)
                {
                    var entry = entries[j];
                    if (entry.Asset == null || string.IsNullOrEmpty(entry.TypeName))
                    {
                        continue;
                    }

                    _configs[entry.TypeName] = entry.Asset;
                }
            }
        }

        public bool TryGet(Type type, out object config)
        {
            config = null;
            if (type == null)
            {
                return false;
            }

            var key = type.AssemblyQualifiedName;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _configs.TryGetValue(key, out config);
        }
    }
}
