using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.SceneLoader;
using Evo.Infrastructure.Services.ScenePayload;
using VContainer;

namespace Evo.Infrastructure.Services.SceneLoader
{
    public static class SceneFeatureExtensions
    {
        public static EvoFeatureRegistry UseScene(
            this EvoFeatureRegistry features,
            SceneLoaderOptions options = null)
        {
            var builder = features.Builder;
            builder.RegisterInstance(options ?? new SceneLoaderOptions());
            builder.Register<ISceneLoaderService, SceneLoaderService>(Lifetime.Singleton);
            builder.Register<IScenePayloadService, ScenePayloadService>(Lifetime.Singleton);
            return features;
        }
    }
}
