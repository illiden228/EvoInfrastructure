using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Save;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.GooglePlayGames.Save
{
    internal static class GooglePlayGamesSaveBackendRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("google_play_games_save", RegisterBackend);
        }

        private static void RegisterBackend(EvoFeatureRegistry features)
        {
            features.Builder.Register<GooglePlayGamesSaveBackend>(Lifetime.Singleton)
                .As<ISaveBackend>();
        }
    }
}
