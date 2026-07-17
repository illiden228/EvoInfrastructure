using Evo.Infrastructure.DI;
using System.Runtime.CompilerServices;

namespace Evo.Infrastructure.GooglePlayGames.Leaderboards
{
    public static class GooglePlayGamesLeaderboardsFeatureExtensions
    {
        private const string AdapterId = "google_play_games_leaderboards";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesLeaderboards(this EvoFeatureRegistry features, GooglePlayGamesLeaderboardsOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesLeaderboardsOptions());
            EvoOptionalFeatureRegistry.TryRegister(features, AdapterId);
            return features;
        }
    }
}
