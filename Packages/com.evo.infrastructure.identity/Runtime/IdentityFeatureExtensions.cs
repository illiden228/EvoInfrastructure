using Evo.Infrastructure.DI;
using VContainer;

namespace Evo.Infrastructure.Services.Identity
{
    public static class IdentityFeatureExtensions
    {
        public static EvoFeatureRegistry UseIdentity(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IPlayerIdentityService, PlayerIdentityService>(Lifetime.Singleton);
            return features;
        }
    }
}
