using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Runtime.Loading
{
    public interface ILoadingProgress
    {
        float CurrentPercent { get; }
        string CurrentMessage { get; }
        event Action Ready;
        event Action Started;
        event Action<LoadingProgress> ProgressChanged;
        event Action Finished;
        bool IsActive { get; }
        void Report(float percent, string message, int stepIndex, int stepCount);
        void NotifyReady();
        void NotifyStarted();
        void NotifyFinished();
        void NotifyHidden();
        UniTask WaitForFinishedAsync(CancellationToken cancellationToken);
    }
}
