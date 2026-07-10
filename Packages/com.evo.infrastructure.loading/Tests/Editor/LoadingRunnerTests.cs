using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Evo.Infrastructure.Runtime.Loading.Tests
{
    public sealed class LoadingRunnerTests
    {
        [Test]
        public void NeverCompletingStep_TimesOut()
        {
            var options = new LoadingExecutionOptions
            {
                EnableStepTimeout = true,
                StepTimeoutSeconds = 0.05f,
                EnableOperationTimeout = false
            };
            var runner = new LoadingRunner(options);

            Assert.ThrowsAsync<TimeoutException>(async () =>
                await runner.RunAsync(
                    new List<ILoadingStep> { new NeverCompletingStep() },
                    new ProgressStub(),
                    CancellationToken.None).AsTask());
        }

        [Test]
        public void PerStepTimeout_OverridesGlobalFallback()
        {
            var runner = new LoadingRunner(new LoadingExecutionOptions
            {
                EnableStepTimeout = true,
                StepTimeoutSeconds = 10f,
                EnableOperationTimeout = false
            });

            Assert.ThrowsAsync<TimeoutException>(async () =>
                await runner.RunAsync(
                    new List<ILoadingStep> { new TimedNeverCompletingStep(0.05f) },
                    new ProgressStub(),
                    CancellationToken.None).AsTask());
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
            public void NotifyFinished() { IsActive = false; Finished?.Invoke(); }
            public void NotifyHidden() => Hidden?.Invoke();
            public UniTask WaitForFinishedAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
        }
    }
}
