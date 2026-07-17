using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Save;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexSaveFeatureExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLegacyFactory() =>
            EvoOptionalFeatureRegistry.Register("yandex_save", features => { UseYandexSave(features); });

        public static EvoFeatureRegistry UseYandexSave(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<ISaveBackend, YandexSaveBackend>(Lifetime.Singleton);
#endif
            return features;
        }
    }
}
