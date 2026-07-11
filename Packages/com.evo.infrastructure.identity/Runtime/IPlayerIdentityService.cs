using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Identity
{
    public interface IPlayerIdentityService
    {
        bool IsAuthenticated { get; }
        PlayerAuthenticationState State { get; }
        PlayerIdentity Current { get; }
        UniTask<PlayerAuthenticationResult> AuthenticateAsync(PlayerAuthenticationMode mode, CancellationToken cancellationToken = default);
    }
}
