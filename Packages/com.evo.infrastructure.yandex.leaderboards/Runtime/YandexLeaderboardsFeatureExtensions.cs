using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Leaderboard;
using Evo.Infrastructure.Services.Leaderboard.Adapters;
using Evo.Infrastructure.Services.PlatformInfo;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexLeaderboardsFeatureExtensions
    {
        public static EvoFeatureRegistry UseYandexLeaderboards(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<ILeaderboardAdapter, YandexGamesLeaderboardAdapter>(Lifetime.Singleton)
                .WithParameter<IPlatformInfoService>(resolver =>
                    resolver.TryResolve<IPlatformInfoService>(out var service) ? service : null);
#endif
            return features;
        }
    }
}
