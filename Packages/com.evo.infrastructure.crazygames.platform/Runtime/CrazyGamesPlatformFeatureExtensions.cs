using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.PlatformLifecycle;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesPlatformFeatureExtensions
    {
        public static EvoFeatureRegistry UseCrazyGamesPlatform(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IPlatformInfoProvider, CrazyGamesPlatformInfoProvider>(Lifetime.Singleton);
            features.Builder.Register<IGamePlatformLifecycleProvider, CrazyGamesPlatformLifecycleProvider>(Lifetime.Singleton);
            return features;
        }
    }
}
