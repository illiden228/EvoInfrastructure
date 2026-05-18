using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Runtime.Loading
{
    public interface ILoadingStep
    {
        string Message { get; }
        float Weight { get; }
        int Order { get; }
        UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken);
    }
}
