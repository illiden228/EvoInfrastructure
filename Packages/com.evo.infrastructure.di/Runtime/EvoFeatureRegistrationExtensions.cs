using System;
using VContainer;

namespace Evo.Infrastructure.DI
{
    public static class EvoFeatureRegistrationExtensions
    {
        public static IContainerBuilder RegisterEvoFeatures(
            this IContainerBuilder builder,
            Action<EvoFeatureRegistry> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            configure?.Invoke(new EvoFeatureRegistry(builder));
            return builder;
        }
    }
}
