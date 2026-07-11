using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.GooglePlayGames
{
    public interface IGooglePlayGamesSession
    {
        GooglePlayGamesAuthenticationState State { get; }
        bool IsInitialized { get; }
        bool IsAuthenticated { get; }
        UniTask<bool> AuthenticateAsync(bool interactive = false, CancellationToken cancellationToken = default);
    }
}
