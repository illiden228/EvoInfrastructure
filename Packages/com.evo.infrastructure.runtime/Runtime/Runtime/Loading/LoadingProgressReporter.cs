using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Application.Loading
{
    public sealed class LoadingProgressReporter : ILoadingProgress
    {
        private UniTaskCompletionSource _finishedTcs;

        public float CurrentPercent { get; private set; }
        public string CurrentMessage { get; private set; }
        public bool IsActive { get; private set; }
        public event Action Ready;
        public event Action Started;
        public event Action<LoadingProgress> ProgressChanged;
        public event Action Finished;
        public event Action Hidden;

        public void Report(float percent, string message, int stepIndex, int stepCount)
        {
            if (percent < 0f)
            {
                percent = 0f;
            }
            else if (percent > 1f)
            {
                percent = 1f;
            }

            CurrentPercent = percent;
            CurrentMessage = message;
            ProgressChanged?.Invoke(new LoadingProgress(percent, message, stepIndex, stepCount));
        }

        public void NotifyReady()
        {
            CurrentPercent = 0f;
            CurrentMessage = string.Empty;
            Ready?.Invoke();
        }

        public void NotifyStarted()
        {
            IsActive = true;
            _finishedTcs = new UniTaskCompletionSource();
            Started?.Invoke();
        }

        public void NotifyFinished()
        {
            IsActive = false;
            _finishedTcs?.TrySetResult();
            Finished?.Invoke();
        }
        
        public void NotifyHidden()
        {
            Hidden?.Invoke();
        }

        public UniTask WaitForFinishedAsync(CancellationToken cancellationToken)
        {
            if (!IsActive)
            {
                return UniTask.CompletedTask;
            }

            if (_finishedTcs == null)
            {
                _finishedTcs = new UniTaskCompletionSource();
            }

            return _finishedTcs.Task.AttachExternalCancellation(cancellationToken);
        }
    }
}
