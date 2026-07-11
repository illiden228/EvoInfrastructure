using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Identity;
using VContainer;
using System.Runtime.CompilerServices;

namespace Evo.Infrastructure.GooglePlayGames.Identity
{
    public static class GooglePlayGamesIdentityFeatureExtensions
    {
        private const string ProviderTypeName = "Evo.Infrastructure.GooglePlayGames.Identity.GooglePlayGamesIdentityProvider, Evo.Infrastructure.GooglePlayGames.Identity.Sdk";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesIdentity(this EvoFeatureRegistry features)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            var providerType = Type.GetType(ProviderTypeName, false);
            if (providerType != null) features.Builder.Register(providerType, Lifetime.Singleton).As<IPlayerIdentityProvider>();
            return features;
        }
    }
}
