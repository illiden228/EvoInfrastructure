using Evo.Infrastructure.Services.CrazyGames;
using Evo.Infrastructure.Services.Debug;
#if CRAZY
using CrazyGames;
#endif

namespace Evo.Infrastructure.Services.PlatformLifecycle
{
    public sealed class CrazyGamesPlatformLifecycleProvider : IGamePlatformLifecycleProvider
    {
        private const string SOURCE = nameof(CrazyGamesPlatformLifecycleProvider);

        public string ProviderId => "crazy";
        public int Priority => 90;

        public bool IsAvailable
        {
            get
            {
#if CRAZY
                return CrazyGamesSdk.IsSupportedRuntime;
#else
                return false;
#endif
            }
        }

        public void NotifyGameReady()
        {
            EvoDebug.Log("CrazySDK has no direct GameReady API. Notification skipped.", SOURCE);
        }

        public void NotifyGameplayStart()
        {
#if CRAZY
            if (!CrazyGamesSdk.TryEnsureReady())
            {
                return;
            }

            CrazySDK.Game.GameplayStart();
#endif
        }

        public void NotifyGameplayStop()
        {
#if CRAZY
            if (!CrazyGamesSdk.TryEnsureReady())
            {
                return;
            }

            CrazySDK.Game.GameplayStop();
#endif
        }
    }
}
