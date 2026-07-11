using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Identity;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesIdentityFeatureExtensions
    {
        public static EvoFeatureRegistry UseCrazyGamesIdentity(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IPlayerIdentityProvider, CrazyGamesIdentityProvider>(Lifetime.Singleton);
            return features;
        }
    }
}
