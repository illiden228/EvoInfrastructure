namespace Evo.Infrastructure.Services.Leaderboard
{
    public interface ILeaderboardService
    {
        void Submit(in LeaderboardSubmitRequest request);
    }
}
