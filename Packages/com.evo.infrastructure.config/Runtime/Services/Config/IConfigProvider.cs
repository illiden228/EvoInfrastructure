using System;

namespace Evo.Infrastructure.Services.Config
{
    public interface IConfigProvider
    {
        bool TryGet(Type type, out object config);
    }
}
