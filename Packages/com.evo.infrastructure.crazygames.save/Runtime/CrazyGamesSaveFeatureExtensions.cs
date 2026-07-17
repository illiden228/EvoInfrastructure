using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Save;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesSaveFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("crazygames_save", features => { UseCrazyGamesSave(features); });

        public static EvoFeatureRegistry UseCrazyGamesSave(this EvoFeatureRegistry features)
        {
            features.Builder.Register<ISaveBackend, CrazySaveBackend>(Lifetime.Singleton)
                .WithParameter<IConfigService>(resolver =>
                    resolver.TryResolve<IConfigService>(out var service) ? service : null);
            return features;
        }
    }
}
