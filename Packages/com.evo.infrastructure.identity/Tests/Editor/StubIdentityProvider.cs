using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Identity.Tests
{
    internal sealed class StubIdentityProvider : IPlayerIdentityProvider
    {
        private readonly Func<PlayerAuthenticationMode, CancellationToken, UniTask<PlayerAuthenticationResult>> _authenticate;

        public StubIdentityProvider(
            string providerId,
            int priority,
            bool isAvailable,
            Func<PlayerAuthenticationMode, CancellationToken, UniTask<PlayerAuthenticationResult>> authenticate)
        {
            ProviderId = providerId;
            Priority = priority;
            IsAvailable = isAvailable;
            _authenticate = authenticate;
        }

        public string ProviderId { get; }
        public int Priority { get; }
        public bool IsAvailable { get; }
        public PlayerAuthenticationState State { get; private set; } = PlayerAuthenticationState.NotInitialized;
        public PlayerIdentity Current { get; private set; } = PlayerIdentity.Empty;
        public int AuthenticationCount { get; private set; }

        public async UniTask<PlayerAuthenticationResult> AuthenticateAsync(
            PlayerAuthenticationMode mode,
            CancellationToken cancellationToken = default)
        {
            AuthenticationCount++;
            State = PlayerAuthenticationState.Authenticating;
            var result = await _authenticate(mode, cancellationToken);
            State = result.State;
            Current = result.Identity;
            return result;
        }
    }
}
