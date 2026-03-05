using UnityEngine;
#if YandexGamesPlatform_yg
using YG;
#endif

namespace _Project.Scripts.Infrastructure.Services.PlatformInfo
{
    public sealed class YandexGamesPlatformInfoProvider : IPlatformInfoProvider
    {
        public int Priority => 100;

        public bool TryGet(out PlatformInfoSnapshot snapshot)
        {
#if YandexGamesPlatform_yg
            if (UnityEngine.Application.platform != RuntimePlatform.WebGLPlayer)
            {
                snapshot = default;
                return false;
            }

            var isMobileWeb = IsMobileWebByYgOrFallback();
            snapshot = new PlatformInfoSnapshot(true, isMobileWeb, !isMobileWeb);
            return true;
#else
            snapshot = default;
            return false;
#endif
        }

        private static bool IsMobileWebByYgOrFallback()
        {
#if YandexGamesPlatform_yg && EnvirData_yg
            return YG2.envir.isMobile || YG2.envir.isTablet;
#else
            return UnityEngine.Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#endif
        }
    }
}
