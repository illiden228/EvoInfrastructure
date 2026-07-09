using Evo.Infrastructure.Services.CrazyGames;
using Evo.Infrastructure.Services.Debug;

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
                return CrazyGamesSdk.IsSupportedRuntime;
            }
        }

        public void NotifyGameReady()
        {
            EvoDebug.Log("CrazySDK has no direct GameReady API. Notification skipped.", SOURCE);
        }

        public void NotifyGameplayStart()
        {
            CrazyGamesSdk.NotifyGameplayStart();
        }

        public void NotifyGameplayStop()
        {
            CrazyGamesSdk.NotifyGameplayStop();
        }
    }
}
