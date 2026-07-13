using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Core.Async
{
    public sealed class AsyncGate
    {
        private readonly object _sync = new object();
        private readonly Queue<Waiter> _waiters = new Queue<Waiter>();
        private bool _isHeld;
        private long _activeLeaseId;
        private long _nextLeaseId;

        public UniTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_sync)
            {
                if (!_isHeld)
                {
                    _isHeld = true;
                    _activeLeaseId = NextLeaseId();
                    return UniTask.FromResult(new Releaser(this, _activeLeaseId));
                }

                var waiter = new Waiter(this);
                _waiters.Enqueue(waiter);
                waiter.RegisterCancellation(cancellationToken);
                return waiter.Task;
            }
        }

        private void Cancel(Waiter waiter, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (waiter.State != WaiterState.Waiting)
                {
                    return;
                }

                waiter.State = WaiterState.Cancelled;
            }

            waiter.TrySetCanceled(cancellationToken);
        }

        private void Release(long leaseId)
        {
            Waiter next = null;
            long nextLeaseId = 0;
            lock (_sync)
            {
                if (!_isHeld || leaseId != _activeLeaseId)
                {
                    return;
                }

                while (_waiters.Count > 0)
                {
                    var candidate = _waiters.Dequeue();
                    if (candidate.State != WaiterState.Waiting)
                    {
                        candidate.DisposeRegistration();
                        continue;
                    }

                    candidate.State = WaiterState.Acquired;
                    next = candidate;
                    nextLeaseId = NextLeaseId();
                    _activeLeaseId = nextLeaseId;
                    break;
                }

                if (next == null)
                {
                    _isHeld = false;
                    _activeLeaseId = 0;
                }
            }

            if (next == null)
            {
                return;
            }

            next.DisposeRegistration();
            next.TrySetResult(new Releaser(this, nextLeaseId));
        }

        private long NextLeaseId()
        {
            _nextLeaseId++;
            if (_nextLeaseId == 0)
            {
                _nextLeaseId++;
            }

            return _nextLeaseId;
        }

        public readonly struct Releaser : IDisposable
        {
            private readonly AsyncGate _owner;
            private readonly long _leaseId;

            internal Releaser(AsyncGate owner, long leaseId)
            {
                _owner = owner;
                _leaseId = leaseId;
            }

            public void Dispose()
            {
                _owner?.Release(_leaseId);
            }
        }

        private sealed class Waiter
        {
            private readonly AsyncGate _owner;
            private readonly UniTaskCompletionSource<Releaser> _completion = new UniTaskCompletionSource<Releaser>();
            private readonly object _registrationSync = new object();
            private CancellationTokenRegistration _registration;
            private bool _registrationCompleted;

            public Waiter(AsyncGate owner)
            {
                _owner = owner;
            }

            public WaiterState State { get; set; }
            public UniTask<Releaser> Task => _completion.Task;

            public void RegisterCancellation(CancellationToken cancellationToken)
            {
                if (!cancellationToken.CanBeCanceled)
                {
                    return;
                }

                var registration = cancellationToken.Register(() => _owner.Cancel(this, cancellationToken));
                lock (_registrationSync)
                {
                    if (_registrationCompleted)
                    {
                        registration.Dispose();
                    }
                    else
                    {
                        _registration = registration;
                    }
                }
            }

            public void DisposeRegistration()
            {
                CancellationTokenRegistration registration;
                lock (_registrationSync)
                {
                    if (_registrationCompleted)
                    {
                        return;
                    }

                    _registrationCompleted = true;
                    registration = _registration;
                }

                registration.Dispose();
            }

            public void TrySetResult(Releaser releaser)
            {
                _completion.TrySetResult(releaser);
            }

            public void TrySetCanceled(CancellationToken cancellationToken)
            {
                _completion.TrySetCanceled(cancellationToken);
            }
        }

        private enum WaiterState
        {
            Waiting,
            Acquired,
            Cancelled
        }
    }
}
