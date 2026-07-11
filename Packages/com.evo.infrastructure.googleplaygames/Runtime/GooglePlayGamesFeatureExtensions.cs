using System;
using System.Runtime.CompilerServices;
using Evo.Infrastructure.DI;
using VContainer;
using VContainer.Unity;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.GooglePlayGames
{
    public static class GooglePlayGamesFeatureExtensions
    {
        private const string SessionTypeName = "Evo.Infrastructure.GooglePlayGames.GooglePlayGamesSession, Evo.Infrastructure.GooglePlayGames.Sdk";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGames(this EvoFeatureRegistry features, GooglePlayGamesOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesOptions());
            var sessionType = Type.GetType(SessionTypeName, false);
            if (sessionType != null)
                features.Builder.Register(sessionType, Lifetime.Singleton)
                    .As(typeof(IGooglePlayGamesSession), typeof(IAsyncStartable));
            else
            {
                features.Builder.Register<UnavailableGooglePlayGamesSession>(Lifetime.Singleton)
                    .As(typeof(IGooglePlayGamesSession), typeof(IAsyncStartable));
                EvoDebug.LogWarning(
                    "Google Play Games SDK runtime is unavailable. Install the official plugin and enable EVO_GOOGLE_PLAY_GAMES_SDK for Android.",
                    nameof(GooglePlayGamesFeatureExtensions));
            }
            return features;
        }
    }
}
