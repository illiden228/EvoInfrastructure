using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Save
{
    public interface IPlayerAuthService
    {
        bool IsAuthorized { get; }
        string PlayerName { get; }
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        UniTask RequestAuthorizationAsync(CancellationToken cancellationToken = default);
    }
}
