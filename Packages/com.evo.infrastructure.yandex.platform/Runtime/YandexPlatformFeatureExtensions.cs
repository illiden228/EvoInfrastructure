using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.PlatformLifecycle;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexPlatformFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("yandex_platform", features => { UseYandexPlatform(features); });

        public static EvoFeatureRegistry UseYandexPlatform(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<IPlatformInfoProvider, YandexGamesPlatformInfoProvider>(Lifetime.Singleton);
            features.Builder.Register<IGamePlatformLifecycleProvider, YandexGamePlatformLifecycleProvider>(Lifetime.Singleton);
#endif
            return features;
        }
    }
}
