using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Identity;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.GooglePlayGames.Identity
{
    internal static class GooglePlayGamesIdentityProviderRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("google_play_games_identity", RegisterProvider);
        }

        private static void RegisterProvider(EvoFeatureRegistry features)
        {
            features.Builder.Register<GooglePlayGamesIdentityProvider>(Lifetime.Singleton)
                .As<IPlayerIdentityProvider>();
        }
    }
}
