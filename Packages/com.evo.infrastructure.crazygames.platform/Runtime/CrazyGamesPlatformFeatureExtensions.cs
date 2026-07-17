using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.PlatformLifecycle;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesPlatformFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("crazygames_platform", features => { UseCrazyGamesPlatform(features); });

        public static EvoFeatureRegistry UseCrazyGamesPlatform(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IPlatformInfoProvider, CrazyGamesPlatformInfoProvider>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null);
            features.Builder.Register<IGamePlatformLifecycleProvider, CrazyGamesPlatformLifecycleProvider>(Lifetime.Singleton);
            return features;
        }
    }
}
