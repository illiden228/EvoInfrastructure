using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Debug;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    public sealed class ConfigService : IConfigService
    {
        private readonly IReadOnlyList<IConfigProvider> _providers;

        public ConfigService(IReadOnlyList<IConfigProvider> providers)
        {
            _providers = providers ?? Array.Empty<IConfigProvider>();
        }

        public T Get<T>() where T : class
        {
            if (TryGet<T>(out var config))
            {
                return config;
            }

            EvoDebug.LogError($"Config of type '{typeof(T).Name}' not found.", nameof(ConfigService));
            throw new InvalidOperationException($"Config of type '{typeof(T).Name}' not found.");
        }

        public bool TryGet<T>(out T config) where T : class
        {
            config = null;
            if (TryGet(typeof(T), out var obj))
            {
                config = obj as T;
                return config != null;
            }

            return false;
        }

        public object Get(Type type)
        {
            if (TryGet(type, out var config))
            {
                return config;
            }

            EvoDebug.LogError($"Config of type '{type?.Name}' not found.", nameof(ConfigService));
            throw new InvalidOperationException($"Config of type '{type?.Name}' not found.");
        }

        public bool TryGet(Type type, out object config)
        {
            config = null;
            if (type == null || _providers == null)
            {
                return false;
            }

            for (var i = 0; i < _providers.Count; i++)
            {
                if (_providers[i] != null && _providers[i].TryGet(type, out config))
                {
                    return config != null;
                }
            }

            return false;
        }
    }
}
