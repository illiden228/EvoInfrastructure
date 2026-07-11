using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Evo.Infrastructure.GooglePlayGames
{
    public sealed class UnavailableGooglePlayGamesSession : IGooglePlayGamesSession, IAsyncStartable
    {
        public GooglePlayGamesAuthenticationState State => GooglePlayGamesAuthenticationState.Unavailable;
        public bool IsInitialized => true;
        public bool IsAuthenticated => false;

        public UniTask<bool> AuthenticateAsync(
            bool interactive = false,
            CancellationToken cancellationToken = default)
        {
            return UniTask.FromResult(false);
        }

        public UniTask StartAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
