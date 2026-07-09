using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.PlatformLifecycle
{
    public sealed class GamePlatformLifecycle : IGamePlatformLifecycle
    {
        private const string SOURCE = nameof(GamePlatformLifecycle);
        private readonly IReadOnlyList<IGamePlatformLifecycleProvider> _providers;

        public GamePlatformLifecycle(IReadOnlyList<IGamePlatformLifecycleProvider> providers)
        {
            _providers = providers ?? Array.Empty<IGamePlatformLifecycleProvider>();
        }

        public void NotifyGameReady()
        {
            var provider = ResolveProvider();
            if (provider == null)
            {
                return;
            }

            try
            {
                provider.NotifyGameReady();
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning($"GameReady notification failed for '{provider.ProviderId}': {ex.Message}", SOURCE);
            }
        }

        public void NotifyGameplayStart()
        {
            var provider = ResolveProvider();
            if (provider == null)
            {
                return;
            }

            try
            {
                provider.NotifyGameplayStart();
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning($"GameplayStart notification failed for '{provider.ProviderId}': {ex.Message}", SOURCE);
            }
        }

        public void NotifyGameplayStop()
        {
            var provider = ResolveProvider();
            if (provider == null)
            {
                return;
            }

            try
            {
                provider.NotifyGameplayStop();
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning($"GameplayStop notification failed for '{provider.ProviderId}': {ex.Message}", SOURCE);
            }
        }

        private IGamePlatformLifecycleProvider ResolveProvider()
        {
            IGamePlatformLifecycleProvider selectedProvider = null;
            var selectedPriority = int.MinValue;

            for (var i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (provider == null || !provider.IsAvailable)
                {
                    continue;
                }

                if (selectedProvider != null && provider.Priority < selectedPriority)
                {
                    continue;
                }

                selectedProvider = provider;
                selectedPriority = provider.Priority;
            }

            return selectedProvider;
        }
    }
}
