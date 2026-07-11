using System;
using System.Reflection;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Config;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    [Obsolete("Use builder.RegisterEvoFeatures(features => features.UseYandex...()) from split Yandex packages.")]
    public static class YandexRuntimeInstaller
    {
        public static void Register(IContainerBuilder builder, IConfigService configService = null)
        {
            if (builder == null)
            {
                return;
            }

            var config = GetConfig(configService);
            var features = new EvoFeatureRegistry(builder);
            if (config.PlatformInfo || config.PlatformLifecycle)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.Yandex.YandexPlatformFeatureExtensions", "UseYandexPlatform");
            }

            if (config.Ads)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.Yandex.YandexAdsFeatureExtensions", "UseYandexAds");
            }

            if (config.Analytics)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.Yandex.YandexAnalyticsFeatureExtensions", "UseYandexAnalytics");
            }

            if (config.Leaderboard)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.Yandex.YandexLeaderboardsFeatureExtensions", "UseYandexLeaderboards");
            }

            if (config.CloudSave)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.Yandex.YandexSaveFeatureExtensions", "UseYandexSave");
            }

            if (config.PlayerAuth)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.Yandex.YandexIdentityFeatureExtensions", "UseYandexIdentity");
            }
        }

        private static void TryUseFeature(EvoFeatureRegistry features, string typeName, string methodName)
        {
            var type = FindType(typeName);
            var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, new object[] { features });
        }

        private static Type FindType(string fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static YandexRuntimeFeatureFlags GetConfig(IConfigService configService)
        {
            if (configService != null && configService.TryGet<YandexRuntimeConfig>(out var config) && config != null)
            {
                return new YandexRuntimeFeatureFlags(
                    config.Ads,
                    config.Analytics,
                    config.Leaderboard,
                    config.PlatformInfo,
                    config.PlatformLifecycle,
                    config.CloudSave,
                    config.PlayerAuth);
            }

            return YandexRuntimeFeatureFlags.Default;
        }

        private readonly struct YandexRuntimeFeatureFlags
        {
            public static readonly YandexRuntimeFeatureFlags Default = new(true, true, true, true, true, true, true);

            public readonly bool Ads;
            public readonly bool Analytics;
            public readonly bool Leaderboard;
            public readonly bool PlatformInfo;
            public readonly bool PlatformLifecycle;
            public readonly bool CloudSave;
            public readonly bool PlayerAuth;

            public YandexRuntimeFeatureFlags(
                bool ads,
                bool analytics,
                bool leaderboard,
                bool platformInfo,
                bool platformLifecycle,
                bool cloudSave,
                bool playerAuth)
            {
                Ads = ads;
                Analytics = analytics;
                Leaderboard = leaderboard;
                PlatformInfo = platformInfo;
                PlatformLifecycle = platformLifecycle;
                CloudSave = cloudSave;
                PlayerAuth = playerAuth;
            }
        }
    }
}
