using System;
using System.Reflection;
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
                TryUseFeature(features, "Evo.Infrastructure.Services.CrazyGames.CrazyGamesPlatformFeatureExtensions", "UseCrazyGamesPlatform");
            }

            if (config.Ads)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.CrazyGames.CrazyGamesAdsFeatureExtensions", "UseCrazyGamesAds");
            }

            if (config.Leaderboard)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.CrazyGames.CrazyGamesLeaderboardsFeatureExtensions", "UseCrazyGamesLeaderboards");
            }

            if (config.CloudSave || config.PlayerAuth)
            {
                TryUseFeature(features, "Evo.Infrastructure.Services.CrazyGames.CrazyGamesSaveFeatureExtensions", "UseCrazyGamesSave");
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
