namespace _Project.Scripts.Infrastructure.Services.Leaderboard
{
    public interface ILeaderboardService
    {
        void Submit(in LeaderboardSubmitRequest request);
    }
}
