using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Identity;
using YG;

namespace Evo.Infrastructure.Services.Yandex
{
    public sealed class YandexIdentityProvider : IPlayerIdentityProvider
    {
        private const int AuthenticationTimeoutMilliseconds = 10000;

        public string ProviderId => "yandex";
        public int Priority => 100;
        public bool IsAvailable => YG2.isSDKEnabled;
        public PlayerAuthenticationState State { get; private set; } = PlayerAuthenticationState.NotInitialized;
        public PlayerIdentity Current { get; private set; } = PlayerIdentity.Empty;

        public async UniTask<PlayerAuthenticationResult> AuthenticateAsync(
            PlayerAuthenticationMode mode,
            CancellationToken cancellationToken = default)
        {
            State = PlayerAuthenticationState.Authenticating;
            if (!YG2.isSDKEnabled)
            {
                return Complete(PlayerAuthenticationState.Unavailable);
            }

            try
            {
                if (YG2.player.auth)
                {
                    return Complete(PlayerAuthenticationState.Authenticated);
                }

                await WaitForAuthenticationResponseAsync(
                    mode == PlayerAuthenticationMode.Interactive
                        ? YG2.OpenAuthDialog
                        : YG2.GetAuth,
                    cancellationToken);
                return Complete(YG2.player.auth
                    ? PlayerAuthenticationState.Authenticated
                    : PlayerAuthenticationState.Unauthenticated);
            }
            catch (OperationCanceledException)
            {
                State = PlayerAuthenticationState.Unauthenticated;
                throw;
            }
            catch (TimeoutException exception)
            {
                return Complete(PlayerAuthenticationState.Unauthenticated, exception.Message);
            }
            catch (Exception exception)
            {
                return Complete(PlayerAuthenticationState.Failed, exception.Message);
            }
        }

        private static async UniTask WaitForAuthenticationResponseAsync(
            Action request,
            CancellationToken cancellationToken)
        {
            var response = new UniTaskCompletionSource();
            void OnResponse()
            {
                // PluginYG2 exposes no auth-only callback. The first shared data response
                // after the request carries either the accepted or rejected auth state.
                response.TrySetResult();
            }

            YG2.onGetSDKData += OnResponse;
            try
            {
                request();
                await response.Task.Timeout(
                    TimeSpan.FromMilliseconds(AuthenticationTimeoutMilliseconds))
                    .AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                YG2.onGetSDKData -= OnResponse;
            }
        }

        private PlayerAuthenticationResult Complete(PlayerAuthenticationState state, string error = "")
        {
            State = state;
            Current = state == PlayerAuthenticationState.Authenticated
                ? new PlayerIdentity(ProviderId, YG2.player.id, YG2.player.name, YG2.player.photo)
                : PlayerIdentity.Empty;
            return new PlayerAuthenticationResult(state, Current, error);
        }
    }
}
