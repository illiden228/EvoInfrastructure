using System;
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
                EvoOptionalFeatureRegistry.TryRegister(features, "yandex_platform");
            }

            if (config.Ads)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "yandex_ads");
            }

            if (config.Analytics)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "yandex_analytics");
            }

            if (config.Leaderboard)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "yandex_leaderboards");
            }

            if (config.CloudSave)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "yandex_save");
            }

            if (config.PlayerAuth)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "yandex_identity");
            }
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
