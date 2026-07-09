using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Leaderboard;
using Evo.Infrastructure.Services.Leaderboard.Adapters;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesLeaderboardsFeatureExtensions
    {
        public static EvoFeatureRegistry UseCrazyGamesLeaderboards(this EvoFeatureRegistry features)
        {
            features.Builder.Register<ILeaderboardAdapter, CrazyNoopLeaderboardAdapter>(Lifetime.Singleton);
            return features;
        }
    }
}
