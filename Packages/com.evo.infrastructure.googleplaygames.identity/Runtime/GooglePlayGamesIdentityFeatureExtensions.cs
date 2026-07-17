using Evo.Infrastructure.DI;
using System.Runtime.CompilerServices;

namespace Evo.Infrastructure.GooglePlayGames.Identity
{
    public static class GooglePlayGamesIdentityFeatureExtensions
    {
        private const string ProviderId = "google_play_games_identity";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesIdentity(this EvoFeatureRegistry features)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            EvoOptionalFeatureRegistry.TryRegister(features, ProviderId);
            return features;
        }
    }
}
