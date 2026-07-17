using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Achievements;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.GooglePlayGames.Achievements
{
    internal static class GooglePlayGamesAchievementAdapterRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("google_play_games_achievements", RegisterAdapter);
        }

        private static void RegisterAdapter(EvoFeatureRegistry features)
        {
            features.Builder.Register<GooglePlayGamesAchievementAdapter>(Lifetime.Singleton)
                .As<IAchievementAdapter>();
        }
    }
}
