using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Identity;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.Services.Yandex
{
    internal static class YandexIdentityProviderRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterFactory()
        {
            EvoOptionalFeatureRegistry.Register("yandex_identity", RegisterProvider);
        }

        private static void RegisterProvider(EvoFeatureRegistry features)
        {
            features.Builder.Register<YandexIdentityProvider>(Lifetime.Singleton)
                .As<IPlayerIdentityProvider>();
        }
    }
}
