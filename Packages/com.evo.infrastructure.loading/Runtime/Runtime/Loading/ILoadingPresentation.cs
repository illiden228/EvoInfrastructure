using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Runtime.Loading
{
    public interface ILoadingPresentation
    {
        bool IsVisible { get; }
        UniTask ShowAsync(CancellationToken cancellationToken);
        UniTask HideAsync(CancellationToken cancellationToken);
    }
}
