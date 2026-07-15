using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Runtime.Gameplay.Loading;
using Evo.Infrastructure.Services.SceneLoader;
using NUnit.Framework;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Evo.Infrastructure.Runtime.Loading.Tests
{
    public sealed class SceneLoadingPipelineReloadTests
    {
        [UnityTest]
        public IEnumerator SameSceneSingle_UnloadsPreviousBeforeTargetLoad()
        {
            return RunInPlayMode(async () =>
            {
                var previous = CreateActiveScene("SameScenePrevious");
                var loader = new SceneLoaderStub(previous, sameIdentity: true, targetCopies: 1);
                var pipeline = CreatePipeline(loader);

                await pipeline.LoadSceneAsync(new AssetReference("same-scene-guid"), LoadSceneMode.Single);

                Assert.That(loader.Events, Is.EqualTo(new[] { "unload_previous", "load_target" }));
                Assert.That(loader.MaximumTargetCopies, Is.EqualTo(1));
                Assert.That(CountLoadedScenes("ReloadTarget"), Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator DifferentScenesSingle_KeepsTargetFirstBehavior()
        {
            return RunInPlayMode(async () =>
            {
                var previous = CreateActiveScene("DifferentScenePrevious");
                var loader = new SceneLoaderStub(previous, sameIdentity: false, targetCopies: 0);
                var pipeline = CreatePipeline(loader);

                await pipeline.LoadSceneAsync(new AssetReference("different-scene-guid"), LoadSceneMode.Single);

                Assert.That(loader.PreviousWasLoadedWhenTargetLoadStarted, Is.True);
                Assert.That(previous.isLoaded, Is.False);
            });
        }

        [UnityTest]
        public IEnumerator SameSceneSingle_CancellationAfterPreviousUnload_DoesNotStartTargetLoad()
        {
            return RunInPlayMode(async () =>
            {
                var previous = CreateActiveScene("CanceledSameScenePrevious");
                using var cancellation = new CancellationTokenSource();
                var loader = new SceneLoaderStub(previous, sameIdentity: true, targetCopies: 1)
                {
                    CancelAfterPreviousUnload = cancellation
                };
                var presentation = new LoadingPresentationStub();
                var pipeline = CreatePipeline(loader, presentation);

                try
                {
                    await pipeline.LoadSceneAsync(
                        new AssetReference("same-scene-guid"),
                        LoadSceneMode.Single,
                        cancellationToken: cancellation.Token);
                    Assert.Fail("Expected cancellation after previous scene unload.");
                }
                catch (OperationCanceledException)
                {
                }

                Assert.That(loader.Events, Is.EqualTo(new[] { "unload_previous" }));
                Assert.That(presentation.ShowCount, Is.EqualTo(1));
                Assert.That(presentation.HideCount, Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator SameSceneSingle_TargetLoadFailure_CleansPartialTarget()
        {
            return RunInPlayMode(async () =>
            {
                var previous = CreateActiveScene("FailedSameScenePrevious");
                var loader = new SceneLoaderStub(previous, sameIdentity: true, targetCopies: 1)
                {
                    FailTargetLoad = true
                };
                var presentation = new LoadingPresentationStub();
                var pipeline = CreatePipeline(loader, presentation);

                try
                {
                    await pipeline.LoadSceneAsync(new AssetReference("same-scene-guid"), LoadSceneMode.Single);
                    Assert.Fail("Expected target scene load failure.");
                }
                catch (InvalidOperationException)
                {
                }

                Assert.That(loader.Events, Is.EqualTo(new[]
                {
                    "unload_previous", "load_target", "cleanup_target"
                }));
                Assert.That(loader.TargetCopies, Is.EqualTo(0));
                Assert.That(loader.MaximumTargetCopies, Is.EqualTo(1));
                Assert.That(CountLoadedScenes("ReloadTarget"), Is.EqualTo(0));
                Assert.That(presentation.ShowCount, Is.EqualTo(1));
                Assert.That(presentation.HideCount, Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator SameSceneSingle_RepeatedCall_LeavesOneTargetCopyEachTime()
        {
            return RunInPlayMode(async () =>
            {
                var previous = CreateActiveScene("RepeatedSameScenePrevious");
                var loader = new SceneLoaderStub(previous, sameIdentity: true, targetCopies: 1);
                var pipeline = CreatePipeline(loader);
                await pipeline.LoadSceneAsync(
                    new AssetReference("same-scene-guid"),
                    LoadSceneMode.Single);

                Assert.That(loader.TargetCopies, Is.EqualTo(1));
                Assert.That(loader.MaximumTargetCopies, Is.EqualTo(1));
                Assert.That(CountLoadedScenes("ReloadTarget"), Is.EqualTo(1));

                await pipeline.LoadSceneAsync(
                    new AssetReference("same-scene-guid"),
                    LoadSceneMode.Single);

                Assert.That(loader.TargetCopies, Is.EqualTo(1));
                Assert.That(loader.MaximumTargetCopies, Is.EqualTo(1));
                Assert.That(CountLoadedScenes("ReloadTarget"), Is.EqualTo(1));
            });
        }

        private static SceneLoadingPipeline CreatePipeline(
            SceneLoaderStub loader,
            LoadingPresentationStub presentation = null)
        {
            presentation ??= new LoadingPresentationStub();
            var transitionOptions = new SceneTransitionOptions
            {
                AwaitLoadingPresentationBeforeSceneLoad = true,
                HideLoadingPresentationAfterLoadingFinished = true
            };
            var executionOptions = new LoadingExecutionOptions
            {
                EnableStepTimeout = false,
                EnableOperationTimeout = false,
                OperationRetryCount = 0,
                TransitionTimeoutSeconds = 5f
            };
            var builder = new ContainerBuilder();
            builder.RegisterInstance<ILoadingPresentation>(presentation);
            builder.RegisterInstance(transitionOptions);
            using var resolver = builder.Build();

            return new SceneLoadingPipeline(
                loader,
                new LoadingProgressStub(),
                configService: null,
                resolver,
                executionOptions);
        }

        private static UnityScene CreateActiveScene(string name)
        {
            var scene = SceneManager.CreateScene(name + Guid.NewGuid().ToString("N"));
            Assert.That(SceneManager.SetActiveScene(scene), Is.True);
            return scene;
        }

        private static int CountLoadedScenes(string name)
        {
            var count = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && string.Equals(scene.name, name, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerator RunInPlayMode(Func<System.Threading.Tasks.Task> test)
        {
            yield return new EnterPlayMode();
            var task = test();
            while (!task.IsCompleted)
            {
                yield return null;
            }

            var failure = task.Exception?.GetBaseException();
            yield return new ExitPlayMode();
            if (failure != null)
            {
                throw failure;
            }
        }

        private sealed class SceneLoaderStub : ISceneLoaderService
        {
            private readonly UnityScene _previous;
            private readonly bool _sameIdentity;
            private UnityScene _target;

            public readonly List<string> Events = new();
            public int TargetCopies { get; private set; }
            public int MaximumTargetCopies { get; private set; }
            public bool PreviousWasLoadedWhenTargetLoadStarted { get; private set; }
            public bool FailTargetLoad { get; set; }
            public CancellationTokenSource CancelAfterPreviousUnload { get; set; }

            public event Action<SceneLoadInfo> SceneLoadStarted;
            public event Action<SceneLoadProgress> SceneLoadProgress;
            public event Action<SceneLoadInfo> SceneLoadFinished;
            public event Action<string> ActiveSceneChanged;
            public string CurrentSceneName => SceneManager.GetActiveScene().name;

            public SceneLoaderStub(UnityScene previous, bool sameIdentity, int targetCopies)
            {
                _previous = previous;
                _sameIdentity = sameIdentity;
                TargetCopies = targetCopies;
                MaximumTargetCopies = targetCopies;
            }

            public UniTask<SceneInstance> LoadAsync(
                string key,
                LoadSceneMode mode = LoadSceneMode.Single,
                bool activateOnLoad = true,
                int priority = 100,
                bool forceReload = false,
                CancellationToken cancellationToken = default)
            {
                return LoadTarget(cancellationToken);
            }

            public UniTask<SceneInstance> LoadAsync(
                AssetReference reference,
                LoadSceneMode mode = LoadSceneMode.Single,
                bool activateOnLoad = true,
                int priority = 100,
                bool forceReload = false,
                CancellationToken cancellationToken = default)
            {
                return LoadTarget(cancellationToken);
            }

            private UniTask<SceneInstance> LoadTarget(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Events.Add("load_target");
                PreviousWasLoadedWhenTargetLoadStarted = _previous.IsValid() && _previous.isLoaded;
                TargetCopies++;
                MaximumTargetCopies = Math.Max(MaximumTargetCopies, TargetCopies);
                _target = CreateTargetScene();
                if (FailTargetLoad)
                {
                    return UniTask.FromException<SceneInstance>(new InvalidOperationException("Target load failed."));
                }

                return UniTask.FromResult(CreateSceneInstance(_target));
            }

            public UniTask UnloadAsync(string key, CancellationToken cancellationToken = default)
            {
                return UnloadAsyncCore(cancellationToken);
            }

            public UniTask UnloadAsync(AssetReference reference, CancellationToken cancellationToken = default)
            {
                return UnloadAsyncCore(cancellationToken);
            }

            private async UniTask UnloadAsyncCore(CancellationToken cancellationToken)
            {
                var activeScene = SceneManager.GetActiveScene();
                var sceneToUnload = activeScene == _target && _target.IsValid() && _target.isLoaded
                    ? _target
                    : _previous;
                if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
                {
                    Events.Add("unload_previous");
                    TargetCopies = Math.Max(0, TargetCopies - 1);
                    var operation = SceneManager.UnloadSceneAsync(sceneToUnload);
                    if (operation != null)
                    {
                        await operation.ToUniTask(cancellationToken: cancellationToken);
                    }

                    CancelAfterPreviousUnload?.Cancel();
                    return;
                }

                Events.Add("cleanup_target");
                TargetCopies = Math.Max(0, TargetCopies - 1);
                if (_target.IsValid() && _target.isLoaded)
                {
                    var operation = SceneManager.UnloadSceneAsync(_target);
                    if (operation != null)
                    {
                        await operation.ToUniTask(cancellationToken: cancellationToken);
                    }
                }
            }

            public UniTask UnloadByNameAsync(string sceneName, CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }

            public bool HasSceneIdentity(UnityScene scene, AssetReference reference)
            {
                return _sameIdentity && (scene == _previous || scene == _target);
            }

            public bool IsLoaded(string sceneName) => false;
            public UniTask ReloadActiveAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

            private static UnityScene CreateTargetScene()
            {
                var scene = SceneManager.CreateScene("ReloadTarget");
                var scope = LifetimeScope.Create(
                    builder => builder.RegisterInstance<IReadOnlyList<IGameplayLoadingStep>>(
                        Array.Empty<IGameplayLoadingStep>()),
                    "TestSceneLifetimeScope");
                SceneManager.MoveGameObjectToScene(scope.gameObject, scene);
                return scene;
            }

            private static SceneInstance CreateSceneInstance(UnityScene scene)
            {
                object boxed = default(SceneInstance);
                var sceneField = typeof(SceneInstance).GetField(
                    "m_Scene",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(sceneField, Is.Not.Null);
                sceneField.SetValue(boxed, scene);
                return (SceneInstance)boxed;
            }
        }

        private sealed class LoadingPresentationStub : ILoadingPresentation
        {
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }
            public bool IsVisible { get; private set; }

            public UniTask ShowAsync(CancellationToken cancellationToken)
            {
                ShowCount++;
                IsVisible = true;
                return UniTask.CompletedTask;
            }

            public UniTask HideAsync(CancellationToken cancellationToken)
            {
                HideCount++;
                IsVisible = false;
                return UniTask.CompletedTask;
            }
        }

        private sealed class LoadingProgressStub : ILoadingProgress
        {
            public float CurrentPercent { get; private set; }
            public string CurrentMessage { get; private set; }
            public bool IsActive { get; private set; }
            public event Action Ready;
            public event Action Started;
            public event Action<LoadingProgress> ProgressChanged;
            public event Action Finished;

            public void Report(float percent, string message, int stepIndex, int stepCount)
            {
                CurrentPercent = percent;
                CurrentMessage = message;
            }

            public void NotifyReady() => Ready?.Invoke();

            public void NotifyStarted()
            {
                IsActive = true;
                Started?.Invoke();
            }

            public void NotifyFinished()
            {
                IsActive = false;
                Finished?.Invoke();
            }

            public void NotifyHidden()
            {
            }

            public UniTask WaitForFinishedAsync(CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
