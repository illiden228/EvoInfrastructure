using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Ads.Adapters;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Analytics.Adapters;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Leaderboard;
using Evo.Infrastructure.Services.Leaderboard.Adapters;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.Save;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexRuntimeInstaller
    {
        public static void Register(IContainerBuilder builder, IConfigService configService = null)
        {
            if (builder == null)
            {
                return;
            }

            var config = GetConfig(configService);
            if (config.Ads)
            {
                builder.Register<IAdsAdapterFactory, YandexGamesAdsAdapterFactory>(Lifetime.Singleton);
            }

            if (config.Analytics)
            {
                builder.Register<IAnalyticsAdapter, YandexGamesAnalyticsAdapter>(Lifetime.Singleton);
            }

            if (config.Leaderboard)
            {
                builder.Register<ILeaderboardAdapter, YandexGamesLeaderboardAdapter>(Lifetime.Singleton);
            }

            if (config.PlatformInfo)
            {
                builder.Register<IPlatformInfoProvider, YandexGamesPlatformInfoProvider>(Lifetime.Singleton);
            }

            if (config.CloudSave)
            {
                builder.Register<ISaveBackend, YandexSaveBackend>(Lifetime.Singleton);
            }

            if (config.PlayerAuth)
            {
                builder.Register<IPlayerAuthService, YandexPlayerAuthService>(Lifetime.Singleton);
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
                    config.CloudSave,
                    config.PlayerAuth);
            }

            return YandexRuntimeFeatureFlags.Default;
        }

        private readonly struct YandexRuntimeFeatureFlags
        {
            public static readonly YandexRuntimeFeatureFlags Default = new(true, true, true, true, true, true);

            public readonly bool Ads;
            public readonly bool Analytics;
            public readonly bool Leaderboard;
            public readonly bool PlatformInfo;
            public readonly bool CloudSave;
            public readonly bool PlayerAuth;

            public YandexRuntimeFeatureFlags(
                bool ads,
                bool analytics,
                bool leaderboard,
                bool platformInfo,
                bool cloudSave,
                bool playerAuth)
            {
                Ads = ads;
                Analytics = analytics;
                Leaderboard = leaderboard;
                PlatformInfo = platformInfo;
                CloudSave = cloudSave;
                PlayerAuth = playerAuth;
            }
        }
    }
}
