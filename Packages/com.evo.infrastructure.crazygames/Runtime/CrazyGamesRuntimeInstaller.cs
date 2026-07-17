using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Config;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    [Obsolete("Use builder.RegisterEvoFeatures(features => features.UseCrazyGames...()) from split CrazyGames packages.")]
    public static class CrazyGamesRuntimeInstaller
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
                EvoOptionalFeatureRegistry.TryRegister(features, "crazygames_platform");
            }

            if (config.Ads)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "crazygames_ads");
            }

            if (config.Leaderboard)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "crazygames_leaderboards");
            }

            if (config.CloudSave)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "crazygames_save");
            }

            if (config.PlayerAuth)
            {
                EvoOptionalFeatureRegistry.TryRegister(features, "crazygames_identity");
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
