using Evo.Infrastructure.DI;
using Evo.Infrastructure.Runtime.Loading;
using VContainer;

namespace Evo.Infrastructure.Runtime.Loading
{
    public static class LoadingFeatureExtensions
    {
        public static EvoFeatureRegistry UseLoading(
            this EvoFeatureRegistry features,
            SceneTransitionOptions sceneTransitionOptions = null,
            LoadingExecutionOptions executionOptions = null,
            StartupLoadingOptions startupLoadingOptions = null)
        {
            var builder = features.Builder;
            builder.RegisterInstance(sceneTransitionOptions ?? new SceneTransitionOptions());
            builder.RegisterInstance(executionOptions ?? new LoadingExecutionOptions());
            builder.RegisterInstance(startupLoadingOptions ?? new StartupLoadingOptions());
            builder.Register<ILoadingProgress, LoadingProgressReporter>(Lifetime.Singleton);
            builder.Register<ISceneLoadingPipeline, SceneLoadingPipeline>(Lifetime.Singleton);
            builder.Register<IApplicationStartupLoadingPipeline, ApplicationStartupLoadingPipeline>(Lifetime.Singleton);
            builder.Register<ILoadingStep, TargetFrameRateStep>(Lifetime.Transient);
            return features;
        }
    }
}
