namespace Evo.Infrastructure.Services.PlatformLifecycle
{
    public sealed class NullGamePlatformLifecycleProvider : IGamePlatformLifecycleProvider
    {
        public string ProviderId => "none";
        public int Priority => int.MinValue;
        public bool IsAvailable => true;

        public void NotifyGameReady()
        {
        }

        public void NotifyGameplayStart()
        {
        }

        public void NotifyGameplayStop()
        {
        }
    }
}
