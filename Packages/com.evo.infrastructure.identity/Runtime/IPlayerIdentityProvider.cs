using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Identity
{
    public interface IPlayerIdentityProvider
    {
        string ProviderId { get; }
        int Priority { get; }
        bool IsAvailable { get; }
        PlayerAuthenticationState State { get; }
        PlayerIdentity Current { get; }
        UniTask<PlayerAuthenticationResult> AuthenticateAsync(PlayerAuthenticationMode mode, CancellationToken cancellationToken = default);
    }
}
