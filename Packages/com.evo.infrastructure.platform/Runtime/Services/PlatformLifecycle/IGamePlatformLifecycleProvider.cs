namespace Evo.Infrastructure.Services.PlatformLifecycle
{
    public interface IGamePlatformLifecycleProvider : IGamePlatformLifecycle
    {
        string ProviderId { get; }
        int Priority { get; }
        bool IsAvailable { get; }
    }
}
