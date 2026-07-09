using Evo.Infrastructure.Services.PlatformLifecycle;
#if YandexGamesPlatform_yg
using YG;
#endif

namespace Evo.Infrastructure.Services.Yandex
{
    public sealed class YandexGamePlatformLifecycleProvider : IGamePlatformLifecycleProvider
    {
        public string ProviderId => "yandex";
        public int Priority => 100;

        public bool IsAvailable
        {
            get
            {
#if YandexGamesPlatform_yg
                return true;
#else
                return false;
#endif
            }
        }

        public void NotifyGameReady()
        {
#if YandexGamesPlatform_yg
            YG2.GameReadyAPI();
#endif
        }

        public void NotifyGameplayStart()
        {
#if YandexGamesPlatform_yg
            YG2.GameplayStart();
#endif
        }

        public void NotifyGameplayStop()
        {
#if YandexGamesPlatform_yg
            YG2.GameplayStop();
#endif
        }
    }
}
