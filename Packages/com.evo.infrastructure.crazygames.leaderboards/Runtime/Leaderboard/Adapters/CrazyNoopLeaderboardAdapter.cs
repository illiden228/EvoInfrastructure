using Evo.Infrastructure.Services.CrazyGames;

namespace Evo.Infrastructure.Services.Leaderboard.Adapters
{
    public sealed class CrazyNoopLeaderboardAdapter : ILeaderboardAdapter
    {
        public string AdapterId => "crazy";
        public int Priority => 0;
        public bool IsInitialized => true;
        public bool IsAvailable => CrazyGamesSdk.IsSupportedRuntime;

        public bool Supports(bool isTimeSeconds)
        {
            return true;
        }

        public void Submit(in LeaderboardSubmitRequest request)
        {
        }
    }
}
