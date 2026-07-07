using System;
using UnityEngine;
#if CRAZY
using CrazyGames;
#endif

namespace Evo.Infrastructure.Services.PlatformInfo
{
    public sealed class CrazyGamesPlatformInfoProvider : IPlatformInfoProvider
    {
        public int Priority => 90;

        public bool TryGet(out PlatformInfoSnapshot snapshot)
        {
            var isWeb = UnityEngine.Application.platform == RuntimePlatform.WebGLPlayer;
            var isMobileWeb = false;

            if (isWeb && TryGetCrazyDeviceType(out var deviceType))
            {
                isMobileWeb = string.Equals(deviceType, "mobile", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(deviceType, "tablet", StringComparison.OrdinalIgnoreCase);
            }
            else if (isWeb)
            {
                isMobileWeb = UnityEngine.Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
            }

            snapshot = new PlatformInfoSnapshot(isWeb, isMobileWeb, isWeb && !isMobileWeb);
            return true;
        }

        private static bool TryGetCrazyDeviceType(out string deviceType)
        {
            deviceType = null;
#if CRAZY
            if (!CrazySDK.IsInitialized)
            {
                return false;
            }

            try
            {
                deviceType = CrazySDK.User.SystemInfo?.device?.type;
                return !string.IsNullOrWhiteSpace(deviceType);
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }
    }
}
