using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.PlatformInfo
{
    public sealed class UnityPlatformInfoProvider : IPlatformInfoProvider
    {
        public int Priority => 0;

        public bool TryGet(out PlatformInfoSnapshot snapshot)
        {
            var isWeb = UnityEngine.Application.platform == RuntimePlatform.WebGLPlayer;
            var isMobileWeb = isWeb && (UnityEngine.Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld);
            var isDesktopWeb = isWeb && !isMobileWeb;
            snapshot = new PlatformInfoSnapshot(isWeb, isMobileWeb, isDesktopWeb);
            return true;
        }
    }
}
