using Evo.Infrastructure.DI;
using System.Runtime.CompilerServices;
using VContainer;
using UnityEngine;

namespace Evo.Infrastructure.GooglePlayGames.Save
{
    public static class GooglePlayGamesSaveFeatureExtensions
    {
        private const string BackendId = "google_play_games_save";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesSave(this EvoFeatureRegistry features, GooglePlayGamesSaveOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesSaveOptions());
            if (!EvoOptionalFeatureRegistry.TryRegister(features, BackendId))
            {
                Debug.LogWarning("[GooglePlayGamesSaveFeatureExtensions] Saved Games bridge is unavailable; backend was not registered.");
            }
            return features;
        }
    }
}
