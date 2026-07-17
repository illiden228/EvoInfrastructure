using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Leaderboard;
using Evo.Infrastructure.Services.Leaderboard.Adapters;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesLeaderboardsFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("crazygames_leaderboards", features => { UseCrazyGamesLeaderboards(features); });

        public static EvoFeatureRegistry UseCrazyGamesLeaderboards(this EvoFeatureRegistry features)
        {
            features.Builder.Register<ILeaderboardAdapter, CrazyNoopLeaderboardAdapter>(Lifetime.Singleton);
            return features;
        }
    }
}
