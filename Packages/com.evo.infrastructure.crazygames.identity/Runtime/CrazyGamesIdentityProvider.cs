using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Identity;

namespace Evo.Infrastructure.Services.CrazyGames
{
    public sealed class CrazyGamesIdentityProvider : IPlayerIdentityProvider
    {
        public string ProviderId => "crazygames";
        public int Priority => 100;
        public bool IsAvailable => CrazyGamesSdk.IsSupportedRuntime;
        public PlayerAuthenticationState State { get; private set; } = PlayerAuthenticationState.NotInitialized;
        public PlayerIdentity Current { get; private set; } = PlayerIdentity.Empty;

        public async UniTask<PlayerAuthenticationResult> AuthenticateAsync(PlayerAuthenticationMode mode, CancellationToken cancellationToken = default)
        {
            State = PlayerAuthenticationState.Authenticating;
            try
            {
                if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
                {
                    return Complete(
                        PlayerAuthenticationState.Unavailable,
                        null,
                        "CrazyGames SDK is unavailable.");
                }

                var user = mode == PlayerAuthenticationMode.Interactive
                    ? await CrazyGamesSdk.ShowAuthPromptAsync(cancellationToken)
                    : await CrazyGamesSdk.GetUserAsync(cancellationToken);
                return user.HasValue
                    ? Complete(PlayerAuthenticationState.Authenticated, user)
                    : Complete(PlayerAuthenticationState.Unauthenticated, null);
            }
            catch (System.OperationCanceledException)
            {
                State = PlayerAuthenticationState.Unauthenticated;
                Current = PlayerIdentity.Empty;
                throw;
            }
            catch (System.Exception exception)
            {
                return Complete(PlayerAuthenticationState.Failed, null, exception.Message);
            }
        }

        private PlayerAuthenticationResult Complete(
            PlayerAuthenticationState state,
            CrazyGamesUserInfo? user,
            string error = "")
        {
            State = state;
            Current = state == PlayerAuthenticationState.Authenticated
                // CrazyGames PortalUser exposes no stable identifier. The username is the
                // best available key and must not be used for cross-provider account linking.
                ? new PlayerIdentity(
                    ProviderId,
                    user?.Username,
                    user?.Username,
                    user?.AvatarUrl)
                : PlayerIdentity.Empty;
            return new PlayerAuthenticationResult(state, Current, error);
        }
    }
}
