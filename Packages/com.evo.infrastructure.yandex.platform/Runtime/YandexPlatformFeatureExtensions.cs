using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.PlatformLifecycle;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexPlatformFeatureExtensions
    {
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
