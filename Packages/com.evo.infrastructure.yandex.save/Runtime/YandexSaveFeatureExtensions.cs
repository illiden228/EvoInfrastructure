using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Save;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexSaveFeatureExtensions
    {
        public static EvoFeatureRegistry UseYandexSave(this EvoFeatureRegistry features)
        {
#if YandexGamesPlatform_yg
            features.Builder.Register<ISaveBackend, YandexSaveBackend>(Lifetime.Singleton);
#endif
            return features;
        }
    }
}
