using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.CrazyGames;
using Evo.Infrastructure.Services.Debug;
#if CRAZY
using CrazyGames;
#endif

namespace Evo.Infrastructure.Services.Save
{
    public sealed class CrazyPlayerAuthService : IPlayerAuthService
    {
        private const string SOURCE = nameof(CrazyPlayerAuthService);
        private bool _autoPromptTried;

        public bool IsAuthorized { get; private set; }
        public string PlayerName { get; private set; } = string.Empty;

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
#if CRAZY
            if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                SetUnauthorized();
                return;
            }

            var user = await GetUserAsync(cancellationToken);
            if (user == null && !_autoPromptTried && CrazySDK.User.IsUserAccountAvailable)
            {
                _autoPromptTried = true;
                user = await ShowAuthPromptAsync(cancellationToken);
            }

            IsAuthorized = user != null;
            PlayerName = user?.username ?? string.Empty;
#else
            SetUnauthorized();
            await UniTask.CompletedTask;
#endif
        }

        public async UniTask RequestAuthorizationAsync(CancellationToken cancellationToken = default)
        {
#if CRAZY
            if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                SetUnauthorized();
                return;
            }

            var user = await ShowAuthPromptAsync(cancellationToken);
            IsAuthorized = user != null;
            PlayerName = user?.username ?? string.Empty;
#else
            SetUnauthorized();
            await UniTask.CompletedTask;
#endif
        }

        private void SetUnauthorized()
        {
            IsAuthorized = false;
            PlayerName = string.Empty;
        }

#if CRAZY
        private static UniTask<PortalUser> GetUserAsync(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource<PortalUser>();
            CancellationTokenRegistration registration = default;

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() => tcs.TrySetResult(null));
            }

            CrazySDK.User.GetUser(user =>
            {
                registration.Dispose();
                tcs.TrySetResult(user);
            });

            return tcs.Task;
        }

        private static UniTask<PortalUser> ShowAuthPromptAsync(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource<PortalUser>();
            CancellationTokenRegistration registration = default;

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() => tcs.TrySetResult(null));
            }

            CrazySDK.User.ShowAuthPrompt((error, user) =>
            {
                registration.Dispose();
                if (error != null)
                {
                    EvoDebug.LogWarning($"ShowAuthPromptAsync error: {error.code} / {error.message}", SOURCE);
                    tcs.TrySetResult(null);
                    return;
                }

                tcs.TrySetResult(user);
            });

            return tcs.Task;
        }
#endif
    }
}
