using System;
using System.Collections.Generic;
using System.Linq;
using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Purchases.Tests
{
    internal sealed class ConfigService : IConfigService
    {
        private readonly Dictionary<Type, object> _values;
        public ConfigService(params object[] values) => _values = values.ToDictionary(value => value.GetType());
        public T Get<T>() where T : class => TryGet(out T value) ? value : null;
        public bool TryGet<T>(out T config) where T : class
        { var found = _values.TryGetValue(typeof(T), out var value); config = value as T; return found; }
        public object Get(Type type) => _values.TryGetValue(type, out var value) ? value : null;
        public bool TryGet(Type type, out object config) => _values.TryGetValue(type, out config);
    }
}
