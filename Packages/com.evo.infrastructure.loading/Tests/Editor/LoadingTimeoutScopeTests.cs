using System;
using System.Threading;
using NUnit.Framework;

namespace Evo.Infrastructure.Runtime.Loading.Tests
{
    public sealed class LoadingTimeoutScopeTests
    {
        [Test]
        public void Timeout_PausesWhileApplicationIsNotFocused()
        {
            var environment = new FakeTimeoutEnvironment();
            using var scope = LoadingTimeoutScope.Create(
                CancellationToken.None,
                0.05f,
                ignoreWhenNotFocused: true,
                environment);

            environment.SetFocused(false);
            environment.Advance(10d);
            scope.Tick();
            Assert.That(scope.Token.IsCancellationRequested, Is.False);

            environment.SetFocused(true);
            environment.Advance(0.04d);
            scope.Tick();
            Assert.That(scope.Token.IsCancellationRequested, Is.False);

            environment.Advance(0.02d);
            scope.Tick();
            Assert.That(scope.IsTimeoutRequested, Is.True);
        }

        [Test]
        public void Timeout_UsesWallClockWhenFocusIgnoringIsDisabled()
        {
            var environment = new FakeTimeoutEnvironment();
            using var scope = LoadingTimeoutScope.Create(
                CancellationToken.None,
                0.05f,
                ignoreWhenNotFocused: false,
                environment);

            environment.SetFocused(false);
            environment.Advance(1d);
            scope.Tick();

            Assert.That(scope.IsTimeoutRequested, Is.True);
        }

        [Test]
        public void Timeout_UsesWallClockWhenApplicationRunsInBackground()
        {
            var environment = new FakeTimeoutEnvironment { RunInBackground = true };
            using var scope = LoadingTimeoutScope.Create(
                CancellationToken.None,
                0.05f,
                ignoreWhenNotFocused: true,
                environment);

            environment.SetFocused(false);
            environment.Advance(1d);
            scope.Tick();

            Assert.That(scope.IsTimeoutRequested, Is.True);
        }

        [Test]
        public void Dispose_UnsubscribesAndStopsTimeoutMonitor()
        {
            var environment = new FakeTimeoutEnvironment();
            var scope = LoadingTimeoutScope.Create(
                CancellationToken.None,
                0.05f,
                ignoreWhenNotFocused: true,
                environment);
            var token = scope.Token;

            Assert.That(environment.SubscriptionCount, Is.EqualTo(1));
            scope.Dispose();
            Assert.That(environment.SubscriptionCount, Is.Zero);

            environment.Advance(1d);
            scope.Tick();
            Assert.That(token.IsCancellationRequested, Is.False);
        }

        [Test]
        public void TimeoutCancellation_RunsOnCallingPlayerLoopThread()
        {
            var environment = new FakeTimeoutEnvironment();
            using var scope = LoadingTimeoutScope.Create(
                CancellationToken.None,
                0.05f,
                ignoreWhenNotFocused: true,
                environment);
            var expectedThread = Thread.CurrentThread.ManagedThreadId;
            var cancellationThread = -1;
            using var registration = scope.Token.Register(
                () => cancellationThread = Thread.CurrentThread.ManagedThreadId);

            environment.Advance(1d);
            scope.Tick();

            Assert.That(cancellationThread, Is.EqualTo(expectedThread));
        }

        private sealed class FakeTimeoutEnvironment : ILoadingTimeoutEnvironment
        {
            private Action<bool> _focusChanged;

            public double RealtimeSinceStartup { get; private set; }
            public bool IsApplicationFocused { get; private set; } = true;
            public bool RunInBackground { get; set; }
            public int SubscriptionCount { get; private set; }

            public event Action<bool> FocusChanged
            {
                add
                {
                    _focusChanged += value;
                    SubscriptionCount++;
                }
                remove
                {
                    _focusChanged -= value;
                    SubscriptionCount--;
                }
            }

            public void Advance(double seconds)
            {
                RealtimeSinceStartup += seconds;
            }

            public void SetFocused(bool isFocused)
            {
                IsApplicationFocused = isFocused;
                _focusChanged?.Invoke(isFocused);
            }
        }
    }
}
