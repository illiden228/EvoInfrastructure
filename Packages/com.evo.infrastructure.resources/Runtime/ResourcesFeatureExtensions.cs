using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.ResourceCatalog;
using Evo.Infrastructure.Services.ResourceLoader;
using Evo.Infrastructure.Services.ResourceProvider;
using VContainer;

namespace Evo.Infrastructure.Services.ResourceProvider
{
    public static class ResourcesFeatureExtensions
    {
        public static EvoFeatureRegistry UseResources(
            this EvoFeatureRegistry features,
            IResourceCatalog resourceCatalog = null)
        {
            var builder = features.Builder;
            if (resourceCatalog != null)
            {
                builder.RegisterInstance(resourceCatalog);
            }

            builder.Register<IResourceLoaderService, AddressablesResourceLoaderService>(Lifetime.Singleton);
            builder.Register<IResourceProviderService, ResourceProviderService>(Lifetime.Singleton)
                .WithParameter<IResourceCatalog>(resourceCatalog);
            return features;
        }
    }
}
