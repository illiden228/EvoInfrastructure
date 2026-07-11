using Evo.Infrastructure.DI;
using VContainer;

namespace Evo.Infrastructure.Services.Achievements
{
    public static class AchievementsFeatureExtensions
    {
        public static EvoFeatureRegistry UseAchievements(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IAchievementService, AchievementService>(Lifetime.Singleton);
            return features;
        }
    }
}
