using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Identity;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesIdentityFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("crazygames_identity", features => { UseCrazyGamesIdentity(features); });

        public static EvoFeatureRegistry UseCrazyGamesIdentity(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IPlayerIdentityProvider, CrazyGamesIdentityProvider>(Lifetime.Singleton);
            return features;
        }
    }
}
