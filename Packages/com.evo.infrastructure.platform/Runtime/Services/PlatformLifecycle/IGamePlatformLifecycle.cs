namespace Evo.Infrastructure.Services.PlatformLifecycle
{
    public interface IGamePlatformLifecycle
    {
        void NotifyGameReady();
        void NotifyGameplayStart();
        void NotifyGameplayStop();
    }
}
