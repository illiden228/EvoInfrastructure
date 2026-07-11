using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Achievements;
using VContainer;
using System.Runtime.CompilerServices;

namespace Evo.Infrastructure.GooglePlayGames.Achievements
{
    public static class GooglePlayGamesAchievementsFeatureExtensions
    {
        private const string AdapterTypeName = "Evo.Infrastructure.GooglePlayGames.Achievements.GooglePlayGamesAchievementAdapter, Evo.Infrastructure.GooglePlayGames.Achievements.Sdk";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesAchievements(this EvoFeatureRegistry features, GooglePlayGamesAchievementsOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesAchievementsOptions());
            var adapterType = Type.GetType(AdapterTypeName, false);
            if (adapterType != null) features.Builder.Register(adapterType, Lifetime.Singleton).As<IAchievementAdapter>();
            return features;
        }
    }
}
