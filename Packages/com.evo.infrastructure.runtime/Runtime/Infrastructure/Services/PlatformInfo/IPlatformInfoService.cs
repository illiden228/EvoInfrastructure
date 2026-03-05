namespace _Project.Scripts.Infrastructure.Services.PlatformInfo
{
    public interface IPlatformInfoService
    {
        PlatformInfoSnapshot Current { get; }
        bool IsWeb { get; }
        bool IsMobileWeb { get; }
        bool IsDesktopWeb { get; }
        void Refresh();
    }
}
