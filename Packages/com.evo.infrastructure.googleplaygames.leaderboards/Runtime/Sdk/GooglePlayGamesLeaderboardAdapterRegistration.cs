using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Leaderboard;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.GooglePlayGames.Leaderboards
{
    internal static class GooglePlayGamesLeaderboardAdapterRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("google_play_games_leaderboards", RegisterAdapter);
        }

        private static void RegisterAdapter(EvoFeatureRegistry features)
        {
            features.Builder.Register<GooglePlayGamesLeaderboardAdapter>(Lifetime.Singleton)
                .As<ILeaderboardAdapter>();
        }
    }
}
