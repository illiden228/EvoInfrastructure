using Evo.Infrastructure.DI;
using Evo.Infrastructure.Runtime.Loading;
using VContainer;

namespace Evo.Infrastructure.Runtime.Loading
{
    public static class LoadingFeatureExtensions
    {
        public static EvoFeatureRegistry UseLoading(
            this EvoFeatureRegistry features,
            SceneTransitionOptions sceneTransitionOptions = null)
        {
            var builder = features.Builder;
            builder.RegisterInstance(sceneTransitionOptions ?? new SceneTransitionOptions());
            builder.Register<ILoadingProgress, LoadingProgressReporter>(Lifetime.Singleton);
            builder.Register<ISceneLoadingPipeline, SceneLoadingPipeline>(Lifetime.Singleton);
            builder.Register<ILoadingStep, TargetFrameRateStep>(Lifetime.Transient);
            return features;
        }
    }
}
