using System;
using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Purchases.Tests
{
    internal sealed class EmptyConfigService : IConfigService
    {
        public T Get<T>() where T : class => null;
        public bool TryGet<T>(out T config) where T : class { config = null; return false; }
        public object Get(Type type) => null;
        public bool TryGet(Type type, out object config) { config = null; return false; }
    }
}
