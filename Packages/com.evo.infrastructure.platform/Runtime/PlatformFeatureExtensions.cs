using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.PlatformLifecycle;
using VContainer;

namespace Evo.Infrastructure.Services.PlatformInfo
{
    public static class PlatformFeatureExtensions
    {
        public static EvoFeatureRegistry UsePlatform(this EvoFeatureRegistry features)
        {
            var builder = features.Builder;
            builder.Register<IPlatformInfoProvider, UnityPlatformInfoProvider>(Lifetime.Singleton);
            builder.Register<IPlatformInfoService, PlatformInfoService>(Lifetime.Singleton);
            builder.Register<IGamePlatformLifecycleProvider, NullGamePlatformLifecycleProvider>(Lifetime.Singleton);
            builder.Register<IGamePlatformLifecycle, GamePlatformLifecycle>(Lifetime.Singleton);
            return features;
        }
    }
}
