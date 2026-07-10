using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Evo.Infrastructure.Runtime.Loading.Tests
{
    public sealed class ApplicationStartupLoadingPipelineTests
    {
        [Test]
        public async System.Threading.Tasks.Task StartupLoading_UsesSingleLifecycleAndContinuousProgress()
        {
            var events = new List<string>();
            var progressValues = new List<float>();
            var progress = new ProgressStub(events, progressValues);
            var presentation = new PresentationStub(events);
            var scenePipeline = new SceneLoadingPipelineStub(events);
            var pipeline = new ApplicationStartupLoadingPipeline(
                scenePipeline,
                progress,
                new LoadingExecutionOptions
                {
                    EnableStepTimeout = false,
                    EnableOperationTimeout = false,
                    StepOrderMode = LoadingStepOrderMode.OrderProperty
                },
                new SceneTransitionOptions
                {
                    AwaitLoadingPresentationBeforeSceneLoad = true,
                    HideLoadingPresentationAfterLoadingFinished = false
                },
                new StartupLoadingOptions(),
                presentation);

            await pipeline.LoadStartupAsync(
                new List<ILoadingStep>
                {
                    new RecordingStep("bootstrap 0..1", 0, events),
                    new RecordingStep("bootstrap warmup", 1, events)
                },
                new AssetReference("startup-scene-guid"),
                LoadSceneMode.Single,
                cancellationToken: CancellationToken.None).AsTask();

            Assert.That(scenePipeline.CreateStepsCallCount, Is.EqualTo(1));
            Assert.That(scenePipeline.LoadSceneCallCount, Is.EqualTo(0));
            Assert.That(progress.ReadyCount, Is.EqualTo(1));
            Assert.That(progress.StartedCount, Is.EqualTo(1));
            Assert.That(progress.FinishedCount, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(new[]
            {
                "show",
                "ready",
                "started",
                "step:bootstrap 0..1",
                "step:bootstrap warmup",
                "step:startup scene load",
                "step:scene gameplay loading",
                "finished"
            }));

            for (var i = 1; i < progressValues.Count; i++)
            {
                Assert.That(progressValues[i], Is.GreaterThanOrEqualTo(progressValues[i - 1]));
            }

            Assert.That(progressValues, Is.Not.Empty);
            Assert.That(progressValues[progressValues.Count - 1], Is.EqualTo(1f));
        }

        private sealed class SceneLoadingPipelineStub : ISceneLoadingPipeline
        {
            private readonly List<string> _events;

            public SceneLoadingPipelineStub(List<string> events)
            {
                _events = events;
            }

            public int CreateStepsCallCount { get; private set; }
            public int LoadSceneCallCount { get; private set; }

            public IReadOnlyList<ILoadingStep> CreateSteps(
                AssetReference sceneReference,
                LoadSceneMode mode = LoadSceneMode.Single,
                bool activateOnLoad = true,
                int priority = 100)
            {
                CreateStepsCallCount++;
                Assert.That(sceneReference, Is.Not.Null);
                Assert.That(mode, Is.EqualTo(LoadSceneMode.Single));
                return new ILoadingStep[]
                {
                    new RecordingStep("startup scene load", 2, _events),
                    new RecordingStep("scene gameplay loading", 3, _events)
                };
            }

            public UniTask LoadSceneAsync(
                AssetReference sceneReference,
                LoadSceneMode mode = LoadSceneMode.Single,
                bool activateOnLoad = true,
                int priority = 100,
                CancellationToken cancellationToken = default)
            {
                LoadSceneCallCount++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class RecordingStep : ILoadingStep
        {
            private readonly string _name;
            private readonly List<string> _events;

            public RecordingStep(string name, int order, List<string> events)
            {
                _name = name;
                Order = order;
                _events = events;
            }

            public string Message => _name;
            public float Weight => 1f;
            public int Order { get; }

            public UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                _events.Add("step:" + _name);
                progress?.Report(0f);
                progress?.Report(0.5f);
                progress?.Report(1f);
                return UniTask.CompletedTask;
            }
        }

        private sealed class PresentationStub : ILoadingPresentation
        {
            private readonly List<string> _events;

            public PresentationStub(List<string> events)
            {
                _events = events;
            }

            public bool IsVisible { get; private set; }

            public UniTask ShowAsync(CancellationToken cancellationToken)
            {
                IsVisible = true;
                _events.Add("show");
                return UniTask.CompletedTask;
            }

            public UniTask HideAsync(CancellationToken cancellationToken)
            {
                IsVisible = false;
                _events.Add("hide");
                return UniTask.CompletedTask;
            }
        }

        private sealed class ProgressStub : ILoadingProgress
        {
            private readonly List<string> _events;
            private readonly List<float> _progressValues;

            public ProgressStub(List<string> events, List<float> progressValues)
            {
                _events = events;
                _progressValues = progressValues;
            }

            public float CurrentPercent { get; private set; }
            public string CurrentMessage { get; private set; }
            public bool IsActive { get; private set; }
            public int ReadyCount { get; private set; }
            public int StartedCount { get; private set; }
            public int FinishedCount { get; private set; }
            public event Action Ready;
            public event Action Started;
            public event Action<LoadingProgress> ProgressChanged;
            public event Action Finished;
            public event Action Hidden;

            public void Report(float percent, string message, int stepIndex, int stepCount)
            {
                CurrentPercent = percent;
                CurrentMessage = message;
                _progressValues.Add(percent);
                ProgressChanged?.Invoke(new LoadingProgress(percent, message, stepIndex, stepCount));
            }

            public void NotifyReady()
            {
                ReadyCount++;
                CurrentPercent = 0f;
                _events.Add("ready");
                Ready?.Invoke();
            }

            public void NotifyStarted()
            {
                StartedCount++;
                IsActive = true;
                _events.Add("started");
                Started?.Invoke();
            }

            public void NotifyFinished()
            {
                FinishedCount++;
                IsActive = false;
                _events.Add("finished");
                Finished?.Invoke();
            }

            public void NotifyHidden()
            {
                Hidden?.Invoke();
            }

            public UniTask WaitForFinishedAsync(CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
