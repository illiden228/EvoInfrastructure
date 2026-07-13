using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Identity
{
    public sealed class PlayerIdentityService : IPlayerIdentityService
    {
        private readonly IReadOnlyList<IPlayerIdentityProvider> _providers;
        private UniTask<PlayerAuthenticationResult> _authenticationTask;
        private bool _authenticationInFlight;

        public PlayerIdentityService(IReadOnlyList<IPlayerIdentityProvider> providers)
        {
            _providers = providers;
        }

        public bool IsAuthenticated => State == PlayerAuthenticationState.Authenticated;

        public PlayerAuthenticationState State { get; private set; } = PlayerAuthenticationState.NotInitialized;

        public PlayerIdentity Current { get; private set; } = PlayerIdentity.Empty;

        public UniTask<PlayerAuthenticationResult> AuthenticateAsync(PlayerAuthenticationMode mode, CancellationToken cancellationToken = default)
        {
            if (!_authenticationInFlight)
            {
                _authenticationInFlight = true;
                _authenticationTask = AuthenticateCoreAsync(mode, cancellationToken).Preserve();
            }

            return _authenticationTask.AttachExternalCancellation(cancellationToken);
        }

        private async UniTask<PlayerAuthenticationResult> AuthenticateCoreAsync(
            PlayerAuthenticationMode mode,
            CancellationToken operationCancellationToken)
        {
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
                    return Complete(await provider.AuthenticateAsync(mode, operationCancellationToken));
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
                _authenticationInFlight = false;
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
