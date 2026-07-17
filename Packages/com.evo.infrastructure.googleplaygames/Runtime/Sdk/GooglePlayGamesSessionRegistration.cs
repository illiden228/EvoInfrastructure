using Evo.Infrastructure.DI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Evo.Infrastructure.GooglePlayGames
{
    internal static class GooglePlayGamesSessionRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("google_play_games_session", RegisterSession);
        }

        private static void RegisterSession(EvoFeatureRegistry features)
        {
            features.Builder.Register<GooglePlayGamesSession>(Lifetime.Singleton)
                .As<IGooglePlayGamesSession>()
                .As<IAsyncStartable>();
        }
    }
}
