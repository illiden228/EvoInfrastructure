using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexIdentityFeatureExtensions
    {
        private const string ProviderId = "yandex_identity";

        public static EvoFeatureRegistry UseYandexIdentity(this EvoFeatureRegistry features)
        {
            if (!EvoOptionalFeatureRegistry.TryRegister(features, ProviderId))
            {
                EvoDebug.LogWarning(
                    "Yandex identity is unavailable. Install PluginYG2 and enable its Authorization module.",
                    nameof(YandexIdentityFeatureExtensions));
            }

            return features;
        }
    }
}
