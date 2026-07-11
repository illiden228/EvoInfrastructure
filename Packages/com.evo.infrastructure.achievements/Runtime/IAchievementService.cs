namespace Evo.Infrastructure.Services.Achievements
{
    public interface IAchievementService
    {
        bool Unlock(string achievementKey);
        bool Reveal(string achievementKey);
        bool Increment(string achievementKey, int steps);
        bool SetProgress(string achievementKey, double progressPercent);
        bool ShowPlatformUi();
    }
}
