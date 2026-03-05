namespace _Project.Scripts.Infrastructure.Services.PlatformInfo
{
    public readonly struct PlatformInfoSnapshot
    {
        public readonly bool IsWeb;
        public readonly bool IsMobileWeb;
        public readonly bool IsDesktopWeb;

        public PlatformInfoSnapshot(bool isWeb, bool isMobileWeb, bool isDesktopWeb)
        {
            IsWeb = isWeb;
            IsMobileWeb = isMobileWeb;
            IsDesktopWeb = isDesktopWeb;
        }
    }
}
