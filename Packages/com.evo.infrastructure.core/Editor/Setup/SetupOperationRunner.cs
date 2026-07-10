#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Evo.Infrastructure.Core.Editor.Setup
{
    internal enum SetupOperationState
    {
        Idle,
        Analyze,
        ResolvePackages,
        InstallPackages,
        WaitForUnity,
        Reanalyze,
        ApplyProjectActions,
        Validate,
        Complete,
        Failed,
        Canceled
    }

    internal sealed class SetupOperationRunner : IDisposable
    {
        private readonly Action _tick;
        private bool _subscribed;

        public SetupOperationRunner(Action tick)
        {
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
        }

        public void Start()
        {
            if (_subscribed)
            {
                return;
            }

            EditorApplication.update += Tick;
            _subscribed = true;
        }

        public void Stop()
        {
            if (!_subscribed)
            {
                return;
            }

            EditorApplication.update -= Tick;
            _subscribed = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private void Tick()
        {
            _tick();
        }
    }
}
#endif
