namespace _Project.Scripts.Infrastructure.Services.PlatformInfo
{
    public interface IPlatformInfoProvider
    {
        int Priority { get; }
        bool TryGet(out PlatformInfoSnapshot snapshot);
    }
}
