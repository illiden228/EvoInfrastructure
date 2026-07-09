using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.CrazyGames;

namespace Evo.Infrastructure.Services.Save
{
    public sealed class CrazyPlayerAuthService : IPlayerAuthService
    {
        private bool _autoPromptTried;

        public bool IsAuthorized { get; private set; }
        public string PlayerName { get; private set; } = string.Empty;

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                SetUnauthorized();
                return;
            }

            var user = await GetUserAsync(cancellationToken);
            if (user == null && !_autoPromptTried && CrazyGamesSdk.IsUserAccountAvailable)
            {
                _autoPromptTried = true;
                user = await ShowAuthPromptAsync(cancellationToken);
            }

            IsAuthorized = user.HasValue;
            PlayerName = user?.Username ?? string.Empty;
        }

        public async UniTask RequestAuthorizationAsync(CancellationToken cancellationToken = default)
        {
            if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                SetUnauthorized();
                return;
            }

            var user = await ShowAuthPromptAsync(cancellationToken);
            IsAuthorized = user.HasValue;
            PlayerName = user?.Username ?? string.Empty;
        }

        private void SetUnauthorized()
        {
            IsAuthorized = false;
            PlayerName = string.Empty;
        }

        private static UniTask<CrazyGamesUserInfo?> GetUserAsync(CancellationToken cancellationToken)
        {
            return CrazyGamesSdk.GetUserAsync(cancellationToken);
        }

        private static UniTask<CrazyGamesUserInfo?> ShowAuthPromptAsync(CancellationToken cancellationToken)
        {
            return CrazyGamesSdk.ShowAuthPromptAsync(cancellationToken);
        }
    }
}
