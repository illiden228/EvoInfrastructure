using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Leaderboard;
using Evo.Infrastructure.Services.Leaderboard.Adapters;
using Evo.Infrastructure.Services.PlatformInfo;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexLeaderboardsFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("yandex_leaderboards", features => { UseYandexLeaderboards(features); });

        public static EvoFeatureRegistry UseYandexLeaderboards(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<ILeaderboardAdapter, YandexGamesLeaderboardAdapter>(Lifetime.Singleton)
                .WithParameter<IPlatformInfoService>(resolver =>
                    resolver.TryResolve<IPlatformInfoService>(out var service) ? service : null);
#endif
            return features;
        }
    }
}
