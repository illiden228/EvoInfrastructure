using System;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.CrazyGames;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.PlatformInfo
{
    public sealed class CrazyGamesPlatformInfoProvider : IPlatformInfoProvider
    {
        private const string PLATFORM_ID = "crazy";
        private readonly IConfigService _configService;

        public CrazyGamesPlatformInfoProvider(IConfigService configService = null)
        {
            _configService = configService;
        }

        public int Priority => 90;

        public bool TryGet(out PlatformInfoSnapshot snapshot)
        {
            if (!IsCrazyPlatformSelected())
            {
                snapshot = default;
                return false;
            }

            if (!CrazyGamesSdk.IsSupportedRuntime)
            {
                snapshot = default;
                return false;
            }

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
            return CrazyGamesSdk.TryGetDeviceType(out deviceType);
        }

        private bool IsCrazyPlatformSelected()
        {
            if (_configService == null ||
                !_configService.TryGet<PlatformCatalog>(out var catalog) ||
                catalog == null)
            {
                return false;
            }

            return string.Equals(catalog.CurrentPlatformId, PLATFORM_ID, StringComparison.OrdinalIgnoreCase);
        }
    }
}
