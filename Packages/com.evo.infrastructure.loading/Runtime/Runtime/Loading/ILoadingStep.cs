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

    /// <summary>
    /// Optional per-step timeout metadata. Positive values override the global
    /// fallback, zero uses it, and negative values disable only the step timeout.
    /// </summary>
    public interface ILoadingStepTimeout
    {
        float TimeoutSeconds { get; }
    }
}
