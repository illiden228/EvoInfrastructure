using System;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    public interface IConfigService
    {
        T Get<T>() where T : class;
        bool TryGet<T>(out T config) where T : class;
        object Get(Type type);
        bool TryGet(Type type, out object config);
    }
}
