using _Project.Scripts.Infrastructure.Services.PlatformInfo;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Analytics
{
    public static class AnalyticsRuntimePlatformResolver
    {
        public static AnalyticsRuntimePlatform Resolve(IPlatformInfoService platformInfoService = null)
        {
#if UNITY_EDITOR
            return AnalyticsRuntimePlatform.Editor;
#else
            if (platformInfoService != null && platformInfoService.IsWeb)
            {
                return AnalyticsRuntimePlatform.WebGL;
            }

            return UnityEngine.Application.platform switch
            {
                RuntimePlatform.Android => AnalyticsRuntimePlatform.Android,
                RuntimePlatform.IPhonePlayer => AnalyticsRuntimePlatform.IOS,
                RuntimePlatform.WebGLPlayer => AnalyticsRuntimePlatform.WebGL,
                RuntimePlatform.WindowsPlayer => AnalyticsRuntimePlatform.Standalone,
                RuntimePlatform.OSXPlayer => AnalyticsRuntimePlatform.Standalone,
                RuntimePlatform.LinuxPlayer => AnalyticsRuntimePlatform.Standalone,
                _ => AnalyticsRuntimePlatform.Any
            };
#endif
        }
    }
}
