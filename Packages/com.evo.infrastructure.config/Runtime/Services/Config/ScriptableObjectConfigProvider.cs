using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Config
{
    public sealed class ScriptableObjectConfigProvider : IConfigProvider
    {
        private readonly Dictionary<Type, object> _configs = new();

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
                    if (entry.Asset == null)
                    {
                        continue;
                    }

                    // Runtime lookup must follow the actual asset type. Serialized assembly-qualified
                    // names are editor migration metadata and become stale when a config moves between
                    // the project assembly and a package assembly.
                    _configs[entry.Asset.GetType()] = entry.Asset;
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

            return _configs.TryGetValue(type, out config);
        }
    }
}
