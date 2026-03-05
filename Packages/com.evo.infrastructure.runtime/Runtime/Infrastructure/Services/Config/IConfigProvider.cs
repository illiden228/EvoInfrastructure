using System;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    public interface IConfigProvider
    {
        bool TryGet(Type type, out object config);
    }
}
