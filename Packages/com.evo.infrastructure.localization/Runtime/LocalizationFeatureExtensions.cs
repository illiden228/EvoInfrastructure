using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Localization;
using VContainer;

namespace Evo.Infrastructure.Services.Localization
{
    public static class LocalizationFeatureExtensions
    {
        public static EvoFeatureRegistry UseLocalization(this EvoFeatureRegistry features)
        {
            features.Builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton);
            return features;
        }
    }
}
