using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Identity;
using Evo.Infrastructure.Services.Debug;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    public static class YandexIdentityFeatureExtensions
    {
        private const string ProviderTypeName =
            "Evo.Infrastructure.Services.Yandex.YandexIdentityProvider, " +
            "Evo.Infrastructure.Yandex.Identity.Sdk";

        public static EvoFeatureRegistry UseYandexIdentity(this EvoFeatureRegistry features)
        {
            var providerType = Type.GetType(ProviderTypeName, false);
            if (providerType != null)
            {
                features.Builder.Register(providerType, Lifetime.Singleton).As<IPlayerIdentityProvider>();
            }
            else
            {
                EvoDebug.LogWarning(
                    "Yandex identity is unavailable. Install PluginYG2 and enable its Authorization module.",
                    nameof(YandexIdentityFeatureExtensions));
            }

            return features;
        }
    }
}
