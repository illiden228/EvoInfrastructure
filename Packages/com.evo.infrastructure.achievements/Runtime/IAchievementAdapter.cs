namespace Evo.Infrastructure.Services.Achievements
{
    public interface IAchievementAdapter
    {
        string AdapterId { get; }
        int Priority { get; }
        bool IsInitialized { get; }
        bool IsAvailable { get; }
        bool Supports(string achievementKey);
        void Unlock(string achievementKey);
        void Reveal(string achievementKey);
        void Increment(string achievementKey, int steps);
        void SetProgress(string achievementKey, double progressPercent);
        void ShowPlatformUi();
    }
}
