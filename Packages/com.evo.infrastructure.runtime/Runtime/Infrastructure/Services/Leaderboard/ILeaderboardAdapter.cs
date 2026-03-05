namespace _Project.Scripts.Infrastructure.Services.Leaderboard
{
    public interface ILeaderboardAdapter
    {
        string AdapterId { get; }
        int Priority { get; }
        bool IsInitialized { get; }
        bool IsAvailable { get; }
        bool Supports(bool isTimeSeconds);
        void Submit(in LeaderboardSubmitRequest request);
    }
}
