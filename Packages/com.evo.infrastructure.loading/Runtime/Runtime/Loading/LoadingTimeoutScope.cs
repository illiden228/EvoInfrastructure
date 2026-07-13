using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.Loading
{
    internal sealed class LoadingTimeoutScope : IDisposable
    {
        private readonly CancellationTokenSource _timeoutSource;
        private readonly CancellationTokenSource _monitorSource = new();
        private readonly ILoadingTimeoutEnvironment _environment;
        private readonly double _timeoutSeconds;
        private readonly bool _ignoreWhenNotFocused;

        private double _elapsedSeconds;
        private double _lastTimestamp;
        private bool _isFocused;
        private bool _disposed;

        private LoadingTimeoutScope(
            CancellationToken parentToken,
            double timeoutSeconds,
            bool ignoreWhenNotFocused,
            ILoadingTimeoutEnvironment environment)
        {
            _timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            _timeoutSeconds = timeoutSeconds;
            _ignoreWhenNotFocused = ignoreWhenNotFocused;
            _environment = environment;
            _isFocused = environment.IsApplicationFocused;
            _lastTimestamp = environment.RealtimeSinceStartup;
            environment.FocusChanged += OnFocusChanged;
            MonitorAsync().Forget();
        }

        public CancellationToken Token => _timeoutSource.Token;

        public bool IsTimeoutRequested { get; private set; }

        public static LoadingTimeoutScope Create(
            CancellationToken parentToken,
            float timeoutSeconds,
            LoadingExecutionOptions options)
        {
            return Create(
                parentToken,
                timeoutSeconds,
                options?.IgnoreTimeoutWhenApplicationNotFocused ?? true,
                UnityLoadingTimeoutEnvironment.Instance);
        }

        internal static LoadingTimeoutScope Create(
            CancellationToken parentToken,
            float timeoutSeconds,
            bool ignoreWhenNotFocused,
            ILoadingTimeoutEnvironment environment)
        {
            if (timeoutSeconds <= 0f)
            {
                return null;
            }

            return new LoadingTimeoutScope(
                parentToken,
                timeoutSeconds,
                ignoreWhenNotFocused,
                environment ?? throw new ArgumentNullException(nameof(environment)));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _environment.FocusChanged -= OnFocusChanged;
            _monitorSource.Cancel();
            _monitorSource.Dispose();
            _timeoutSource.Dispose();
        }

        private async UniTaskVoid MonitorAsync()
        {
            try
            {
                while (!_disposed)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, _monitorSource.Token);
                    if (_disposed)
                    {
                        return;
                    }

                    Tick();
                }
            }
            catch (OperationCanceledException) when (_monitorSource.IsCancellationRequested)
            {
            }
        }

        private void OnFocusChanged(bool isFocused)
        {
            if (_disposed)
            {
                return;
            }

            AdvanceTo(_environment.RealtimeSinceStartup);
            _isFocused = isFocused;
        }

        internal void Tick()
        {
            if (!_disposed)
            {
                AdvanceTo(_environment.RealtimeSinceStartup);
            }
        }

        private void AdvanceTo(double timestamp)
        {
            var delta = Math.Max(0d, timestamp - _lastTimestamp);
            _lastTimestamp = timestamp;
            if (!ShouldPauseTimeout())
            {
                _elapsedSeconds += delta;
            }

            if (!IsTimeoutRequested && _elapsedSeconds >= _timeoutSeconds)
            {
                IsTimeoutRequested = true;
                _timeoutSource.Cancel();
            }
        }

        private bool ShouldPauseTimeout()
        {
            return _ignoreWhenNotFocused &&
                   !_environment.RunInBackground &&
                   !_isFocused;
        }
    }

    internal interface ILoadingTimeoutEnvironment
    {
        double RealtimeSinceStartup { get; }
        bool IsApplicationFocused { get; }
        bool RunInBackground { get; }
        event Action<bool> FocusChanged;
    }

    internal sealed class UnityLoadingTimeoutEnvironment : ILoadingTimeoutEnvironment
    {
        public static readonly UnityLoadingTimeoutEnvironment Instance = new();

        private UnityLoadingTimeoutEnvironment()
        {
        }

        public double RealtimeSinceStartup => Time.realtimeSinceStartupAsDouble;
        public bool IsApplicationFocused => Application.isFocused;
        public bool RunInBackground => Application.runInBackground;

        public event Action<bool> FocusChanged
        {
            add => Application.focusChanged += value;
            remove => Application.focusChanged -= value;
        }
    }
}
