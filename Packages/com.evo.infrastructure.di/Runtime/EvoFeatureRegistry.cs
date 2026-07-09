using System;
using VContainer;

namespace Evo.Infrastructure.DI
{
    public sealed class EvoFeatureRegistry
    {
        public EvoFeatureRegistry(IContainerBuilder builder)
        {
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public IContainerBuilder Builder { get; }
    }
}
