using Evo.Infrastructure.DI;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Evo.Infrastructure.GooglePlayGames.Achievements
{
    public static class GooglePlayGamesAchievementsFeatureExtensions
    {
        private const string AdapterId = "google_play_games_achievements";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesAchievements(this EvoFeatureRegistry features, GooglePlayGamesAchievementsOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesAchievementsOptions());
            if (!EvoOptionalFeatureRegistry.TryRegister(features, AdapterId))
            {
                Debug.LogWarning("[GooglePlayGamesAchievementsFeatureExtensions] Achievements bridge is unavailable; adapter was not registered.");
            }
            return features;
        }
    }
}
