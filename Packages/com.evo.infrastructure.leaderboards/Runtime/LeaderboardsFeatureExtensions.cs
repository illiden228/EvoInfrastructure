using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Leaderboard;
using VContainer;

namespace Evo.Infrastructure.Services.Leaderboard
{
    public static class LeaderboardsFeatureExtensions
    {
        public static EvoFeatureRegistry UseLeaderboards(this EvoFeatureRegistry features)
        {
            features.Builder.Register<ILeaderboardService, LeaderboardService>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null);
            return features;
        }
    }
}
