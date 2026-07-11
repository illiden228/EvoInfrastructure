using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Leaderboard;
using VContainer;
using System.Runtime.CompilerServices;

namespace Evo.Infrastructure.GooglePlayGames.Leaderboards
{
    public static class GooglePlayGamesLeaderboardsFeatureExtensions
    {
        private const string AdapterTypeName = "Evo.Infrastructure.GooglePlayGames.Leaderboards.GooglePlayGamesLeaderboardAdapter, Evo.Infrastructure.GooglePlayGames.Leaderboards.Sdk";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesLeaderboards(this EvoFeatureRegistry features, GooglePlayGamesLeaderboardsOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesLeaderboardsOptions());
            var adapterType = Type.GetType(AdapterTypeName, false);
            if (adapterType != null) features.Builder.Register(adapterType, Lifetime.Singleton).As<ILeaderboardAdapter>();
            return features;
        }
    }
}
