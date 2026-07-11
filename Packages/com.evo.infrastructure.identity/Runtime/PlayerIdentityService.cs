using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Identity
{
    public sealed class PlayerIdentityService : IPlayerIdentityService
    {
        private readonly IReadOnlyList<IPlayerIdentityProvider> _providers;
        private readonly SemaphoreSlim _authenticationGate = new(1, 1);

        public PlayerIdentityService(IReadOnlyList<IPlayerIdentityProvider> providers)
        {
            _providers = providers;
        }

        public bool IsAuthenticated => State == PlayerAuthenticationState.Authenticated;

        public PlayerAuthenticationState State { get; private set; } = PlayerAuthenticationState.NotInitialized;

        public PlayerIdentity Current { get; private set; } = PlayerIdentity.Empty;

        public async UniTask<PlayerAuthenticationResult> AuthenticateAsync(PlayerAuthenticationMode mode, CancellationToken cancellationToken = default)
        {
            await _authenticationGate.WaitAsync(cancellationToken);
            try
            {
                var provider = SelectProvider();
                if (provider == null)
                {
                    return Complete(new PlayerAuthenticationResult(
                        PlayerAuthenticationState.Unavailable,
                        PlayerIdentity.Empty,
                        "No identity provider is available."));
                }

                State = PlayerAuthenticationState.Authenticating;
                try
                {
                    return Complete(await provider.AuthenticateAsync(mode, cancellationToken));
                }
                catch (System.OperationCanceledException)
                {
                    State = PlayerAuthenticationState.Unauthenticated;
                    throw;
                }
                catch (System.Exception exception)
                {
                    return Complete(new PlayerAuthenticationResult(
                        PlayerAuthenticationState.Failed,
                        PlayerIdentity.Empty,
                        exception.Message));
                }
            }
            finally
            {
                _authenticationGate.Release();
            }
        }

        private IPlayerIdentityProvider SelectProvider()
        {
            IPlayerIdentityProvider selected = null;
            foreach (var provider in _providers)
            {
                if (provider.IsAvailable && (selected == null || provider.Priority > selected.Priority))
                {
                    selected = provider;
                }
            }

            return selected;
        }

        private PlayerAuthenticationResult Complete(PlayerAuthenticationResult result)
        {
            State = result.State;
            Current = result.Identity;
            return result;
        }
    }
}
