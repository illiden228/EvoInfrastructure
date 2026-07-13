using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.TestTools;
using UnityEngine.TestTools;

namespace Evo.Infrastructure.Runtime.Loading.Tests
{
    public sealed class LoadingRunnerTests
    {
        [UnityTest]
        public IEnumerator NeverCompletingStep_TimesOut()
        {
            var options = new LoadingExecutionOptions
            {
                EnableStepTimeout = true,
                StepTimeoutSeconds = 0.05f,
                EnableOperationTimeout = false,
                IgnoreTimeoutWhenApplicationNotFocused = false
            };
            var runner = new LoadingRunner(options);

            return RunInPlayMode(async () =>
            {
                try
                {
                    await runner.RunAsync(
                        new List<ILoadingStep> { new NeverCompletingStep() },
                        new ProgressStub(),
                        CancellationToken.None).AsTask();
                    Assert.Fail("Expected the loading step to time out.");
                }
                catch (TimeoutException)
                {
                }
            });
        }

        [UnityTest]
        public IEnumerator PerStepTimeout_OverridesGlobalFallback()
        {
            var runner = new LoadingRunner(new LoadingExecutionOptions
            {
                EnableStepTimeout = true,
                StepTimeoutSeconds = 10f,
                EnableOperationTimeout = false,
                IgnoreTimeoutWhenApplicationNotFocused = false
            });

            return RunInPlayMode(async () =>
            {
                try
                {
                    await runner.RunAsync(
                        new List<ILoadingStep> { new TimedNeverCompletingStep(0.05f) },
                        new ProgressStub(),
                        CancellationToken.None).AsTask();
                    Assert.Fail("Expected the per-step timeout to expire.");
                }
                catch (TimeoutException)
                {
                }
            });
        }

        [UnityTest]
        public IEnumerator NeverCompletingStep_UsesOperationTimeout()
        {
            var runner = new LoadingRunner(new LoadingExecutionOptions
            {
                EnableStepTimeout = false,
                EnableOperationTimeout = true,
                OperationTimeoutSeconds = 0.05f,
                IgnoreTimeoutWhenApplicationNotFocused = false
            });

            return RunInPlayMode(async () =>
            {
                try
                {
                    await runner.RunAsync(
                        new List<ILoadingStep> { new NeverCompletingStep() },
                        new ProgressStub(),
                        CancellationToken.None).AsTask();
                    Assert.Fail("Expected the loading operation to time out.");
                }
                catch (TimeoutException exception)
                {
                    Assert.That(exception.Message, Does.Contain("operation timed out"));
                }
            });
        }

        [UnityTest]
        public IEnumerator ParentCancellation_RemainsOperationCanceledException()
        {
            var runner = new LoadingRunner(new LoadingExecutionOptions
            {
                EnableStepTimeout = false,
                EnableOperationTimeout = true,
                OperationTimeoutSeconds = 10f,
                IgnoreTimeoutWhenApplicationNotFocused = false
            });
            return RunInPlayMode(async () =>
            {
                using var cancellation = new CancellationTokenSource();
                var runTask = runner.RunAsync(
                    new List<ILoadingStep> { new NeverCompletingStep() },
                    new ProgressStub(),
                    cancellation.Token).AsTask();

                await UniTask.Yield();
                cancellation.Cancel();
                try
                {
                    await runTask;
                    Assert.Fail("Expected caller cancellation.");
                }
                catch (OperationCanceledException)
                {
                }
            });
        }

        [UnityTest]
        public IEnumerator TimeoutLifecycleContinuation_RunsOnPlayerLoopThread()
        {
            var expectedThread = Thread.CurrentThread.ManagedThreadId;
            var progress = new ProgressStub();
            var runner = new LoadingRunner(new LoadingExecutionOptions
            {
                EnableStepTimeout = true,
                StepTimeoutSeconds = 0.05f,
                EnableOperationTimeout = false,
                IgnoreTimeoutWhenApplicationNotFocused = false
            });

            return RunInPlayMode(async () =>
            {
                try
                {
                    await runner.RunAsync(
                        new List<ILoadingStep> { new NeverCompletingStep() },
                        progress,
                        CancellationToken.None).AsTask();
                    Assert.Fail("Expected the loading step to time out.");
                }
                catch (TimeoutException)
                {
                }

                Assert.That(progress.FinishedThreadId, Is.EqualTo(expectedThread));
            });
        }

        [TestCase(LoadingStepOrderMode.Registration, "second,first")]
        [TestCase(LoadingStepOrderMode.OrderProperty, "first,second")]
        public async System.Threading.Tasks.Task Steps_UseConfiguredOrdering(
            LoadingStepOrderMode mode,
            string expected)
        {
            var executed = new List<string>();
            var runner = new LoadingRunner(new LoadingExecutionOptions
            {
                StepOrderMode = mode,
                EnableStepTimeout = false,
                EnableOperationTimeout = false
            });

            await runner.RunAsync(
                new List<ILoadingStep>
                {
                    new RecordingStep("second", 20, executed),
                    new RecordingStep("first", 10, executed)
                },
                new ProgressStub(),
                CancellationToken.None).AsTask();

            Assert.That(string.Join(",", executed), Is.EqualTo(expected));
        }

        private static IEnumerator RunInPlayMode(Func<System.Threading.Tasks.Task> test)
        {
            yield return new EnterPlayMode();

            System.Threading.Tasks.Task task = null;
            Exception immediateFailure = null;
            try
            {
                task = test();
            }
            catch (Exception exception)
            {
                immediateFailure = exception;
            }

            while (task != null && !task.IsCompleted)
            {
                yield return null;
            }

            var failure = immediateFailure ?? task?.Exception?.GetBaseException();
            var wasCanceled = task?.IsCanceled == true;
            yield return new ExitPlayMode();

            if (failure != null)
            {
                throw failure;
            }

            if (wasCanceled)
            {
                Assert.Fail("The test task was unexpectedly canceled.");
            }
        }

        private sealed class RecordingStep : ILoadingStep
        {
            private readonly string _name;
            private readonly List<string> _executed;

            public RecordingStep(string name, int order, List<string> executed)
            {
                _name = name;
                Order = order;
                _executed = executed;
            }

            public string Message => _name;
            public float Weight => 1f;
            public int Order { get; }

            public UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                _executed.Add(_name);
                return UniTask.CompletedTask;
            }
        }

        private sealed class NeverCompletingStep : ILoadingStep
        {
            private readonly UniTaskCompletionSource _completion = new();

            public string Message => "Never completes";
            public float Weight => 1f;
            public int Order => 0;

            public UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                return _completion.Task;
            }
        }

        private sealed class TimedNeverCompletingStep : ILoadingStep, ILoadingStepTimeout
        {
            private readonly UniTaskCompletionSource _completion = new();

            public TimedNeverCompletingStep(float timeoutSeconds)
            {
                TimeoutSeconds = timeoutSeconds;
            }

            public string Message => "Timed step";
            public float Weight => 1f;
            public int Order => 0;
            public float TimeoutSeconds { get; }

            public UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                return _completion.Task;
            }
        }

        private sealed class ProgressStub : ILoadingProgress
        {
            public float CurrentPercent { get; private set; }
            public string CurrentMessage { get; private set; }
            public bool IsActive { get; private set; }
            public int FinishedThreadId { get; private set; } = -1;
            public event Action Ready;
            public event Action Started;
            public event Action<LoadingProgress> ProgressChanged;
            public event Action Finished;
            public event Action Hidden;

            public void Report(float percent, string message, int stepIndex, int stepCount)
            {
                CurrentPercent = percent;
                CurrentMessage = message;
                ProgressChanged?.Invoke(new LoadingProgress(percent, message, stepIndex, stepCount));
            }

            public void NotifyReady() => Ready?.Invoke();
            public void NotifyStarted() { IsActive = true; Started?.Invoke(); }
            public void NotifyFinished()
            {
                IsActive = false;
                FinishedThreadId = Thread.CurrentThread.ManagedThreadId;
                Finished?.Invoke();
            }
            public void NotifyHidden() => Hidden?.Invoke();
            public UniTask WaitForFinishedAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
        }
    }
}
