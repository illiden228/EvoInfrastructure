using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Save;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesSaveFeatureExtensions
    {
        public static EvoFeatureRegistry UseCrazyGamesSave(this EvoFeatureRegistry features)
        {
            features.Builder.Register<ISaveBackend, CrazySaveBackend>(Lifetime.Singleton);
            features.Builder.Register<IPlayerAuthService, CrazyPlayerAuthService>(Lifetime.Singleton);
            return features;
        }
    }
}
