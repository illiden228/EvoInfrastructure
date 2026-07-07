using Evo.Infrastructure.Services.Ads;
using Evo.Infrastructure.Services.Ads.Adapters;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Leaderboard;
using Evo.Infrastructure.Services.Leaderboard.Adapters;
using Evo.Infrastructure.Services.PlatformInfo;
using Evo.Infrastructure.Services.PlatformLifecycle;
using Evo.Infrastructure.Services.Save;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesRuntimeInstaller
    {
        public static void Register(IContainerBuilder builder, IConfigService configService = null)
        {
            if (builder == null)
            {
                return;
            }

#if !CRAZY
            return;
#endif

            var config = GetConfig(configService);
            if (config.Ads)
            {
                builder.Register<IAdsAdapterFactory, CrazyGamesAdsAdapterFactory>(Lifetime.Singleton);
            }

            if (config.Leaderboard)
            {
                builder.Register<ILeaderboardAdapter, CrazyNoopLeaderboardAdapter>(Lifetime.Singleton);
            }

            if (config.PlatformInfo)
            {
                builder.Register<IPlatformInfoProvider, CrazyGamesPlatformInfoProvider>(Lifetime.Singleton);
            }

            if (config.PlatformLifecycle)
            {
                builder.Register<IGamePlatformLifecycleProvider, CrazyGamesPlatformLifecycleProvider>(Lifetime.Singleton);
            }

            if (config.CloudSave)
            {
                builder.Register<ISaveBackend, CrazySaveBackend>(Lifetime.Singleton);
            }

            if (config.PlayerAuth)
            {
                builder.Register<IPlayerAuthService, CrazyPlayerAuthService>(Lifetime.Singleton);
            }
        }

        private static CrazyGamesRuntimeFeatureFlags GetConfig(IConfigService configService)
        {
            if (configService != null && configService.TryGet<CrazyGamesRuntimeConfig>(out var config) && config != null)
            {
                return new CrazyGamesRuntimeFeatureFlags(
                    config.Ads,
                    config.Leaderboard,
                    config.PlatformInfo,
                    config.PlatformLifecycle,
                    config.CloudSave,
                    config.PlayerAuth);
            }

            return CrazyGamesRuntimeFeatureFlags.Default;
        }

        private readonly struct CrazyGamesRuntimeFeatureFlags
        {
            public static readonly CrazyGamesRuntimeFeatureFlags Default = new(true, true, true, true, true, true);

            public readonly bool Ads;
            public readonly bool Leaderboard;
            public readonly bool PlatformInfo;
            public readonly bool PlatformLifecycle;
            public readonly bool CloudSave;
            public readonly bool PlayerAuth;

            public CrazyGamesRuntimeFeatureFlags(
                bool ads,
                bool leaderboard,
                bool platformInfo,
                bool platformLifecycle,
                bool cloudSave,
                bool playerAuth)
            {
                Ads = ads;
                Leaderboard = leaderboard;
                PlatformInfo = platformInfo;
                PlatformLifecycle = platformLifecycle;
                CloudSave = cloudSave;
                PlayerAuth = playerAuth;
            }
        }
    }
}
