using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Evo.Infrastructure.Runtime.Gameplay.Loading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.SceneLoader;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using VContainer;
using VContainer.Unity;

namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class SceneLoadingPipeline : ISceneLoadingPipeline
    {
        private readonly ISceneLoaderService _sceneLoader;
        private readonly ILoadingProgress _progress;
        private readonly ILoadingPresentation _loadingPresentation;
        private readonly SceneTransitionOptions _transitionOptions;
        private readonly string _transitionSceneName;

        public SceneLoadingPipeline(ISceneLoaderService sceneLoader, ILoadingProgress progress, IConfigService configService)
            : this(sceneLoader, progress, configService, null)
        {
        }

        [Inject]
        public SceneLoadingPipeline(
            ISceneLoaderService sceneLoader,
            ILoadingProgress progress,
            IConfigService configService,
            IObjectResolver resolver)
        {
            _sceneLoader = sceneLoader;
            _progress = progress;
            _loadingPresentation = TryResolve<ILoadingPresentation>(resolver);
            _transitionOptions = TryResolve<SceneTransitionOptions>(resolver) ?? new SceneTransitionOptions();
            _transitionSceneName = ResolveTransitionSceneName(configService);
        }

        public IReadOnlyList<ILoadingStep> CreateSteps(
            AssetReference sceneReference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100)
        {
            if (sceneReference == null)
            {
                return Array.Empty<ILoadingStep>();
            }

            var context = new SceneLoadingContext(sceneReference, mode, activateOnLoad, priority);
            return new ILoadingStep[]
            {
                new SceneLoadStep(_sceneLoader, context),
                new SceneStepsStep(context)
            };
        }

        public async UniTask LoadSceneAsync(
            AssetReference sceneReference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            CancellationToken cancellationToken = default)
        {
            if (sceneReference == null)
            {
                return;
            }

            var runner = new LoadingRunner();
            var steps = CreateSteps(sceneReference, mode, activateOnLoad, priority);
            var useTransition = mode == LoadSceneMode.Single && !string.IsNullOrEmpty(_transitionSceneName);

            try
            {
                await ShowLoadingPresentationIfNeeded(cancellationToken);

                var loadingTask = runner.RunAsync(steps, _progress, cancellationToken);
                var transitionTask = useTransition
                    ? LoadTransitionSceneAdditiveAsync(cancellationToken)
                    : UniTask.CompletedTask;

                await UniTask.WhenAll(loadingTask, transitionTask);
            }
            finally
            {
                if (useTransition)
                {
                    await UnloadTransitionSceneAsync();
                }

                await HideLoadingPresentationIfNeeded();
            }
        }

        private async UniTask ShowLoadingPresentationIfNeeded(CancellationToken cancellationToken)
        {
            if (_loadingPresentation == null ||
                _transitionOptions == null ||
                !_transitionOptions.AwaitLoadingPresentationBeforeSceneLoad)
            {
                return;
            }

            await _loadingPresentation.ShowAsync(cancellationToken);
        }

        private async UniTask HideLoadingPresentationIfNeeded()
        {
            if (_loadingPresentation == null ||
                _transitionOptions == null ||
                !_transitionOptions.HideLoadingPresentationAfterLoadingFinished)
            {
                return;
            }

            await _loadingPresentation.HideAsync(CancellationToken.None);
        }

        private async UniTask LoadTransitionSceneAdditiveAsync(CancellationToken cancellationToken)
        {
            var existingScene = SceneManager.GetSceneByName(_transitionSceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                return;
            }

            var transitionLoad = SceneManager.LoadSceneAsync(_transitionSceneName, LoadSceneMode.Additive);
            if (transitionLoad == null)
            {
                EvoDebug.LogWarning(
                    $"Transition scene '{_transitionSceneName}' failed to start loading.",
                    nameof(SceneLoadingPipeline));
                return;
            }

            try
            {
                await transitionLoad.ToUniTask(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is handled by the loading flow owner.
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Transition scene load failed. {ex.Message}",
                    nameof(SceneLoadingPipeline));
            }
        }

        private async UniTask UnloadTransitionSceneAsync()
        {
            var scene = SceneManager.GetSceneByName(_transitionSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            try
            {
                var unloadOperation = SceneManager.UnloadSceneAsync(scene);
                if (unloadOperation != null)
                {
                    await unloadOperation.ToUniTask(cancellationToken: CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Transition scene unload failed. {ex.Message}",
                    nameof(SceneLoadingPipeline));
            }
        }

        private sealed class SceneLoadingContext
        {
            public readonly AssetReference SceneReference;
            public readonly LoadSceneMode Mode;
            public readonly bool ActivateOnLoad;
            public readonly int Priority;
            public SceneInstance SceneInstance;
            public string SceneKey;

            public SceneLoadingContext(AssetReference sceneReference, LoadSceneMode mode, bool activateOnLoad, int priority)
            {
                SceneReference = sceneReference;
                Mode = mode;
                ActivateOnLoad = activateOnLoad;
                Priority = priority;
                SceneKey = GetReferenceKey(sceneReference);
            }

            private static string GetReferenceKey(AssetReference reference)
            {
                if (reference == null)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(reference.AssetGUID))
                {
                    return reference.AssetGUID;
                }

                var runtimeKey = reference.RuntimeKey?.ToString();
                if (!string.IsNullOrEmpty(runtimeKey))
                {
                    return runtimeKey;
                }

                return reference.AssetGUID;
            }
        }

        private sealed class SceneLoadStep : ILoadingStep
        {
            private readonly ISceneLoaderService _sceneLoader;
            private readonly SceneLoadingContext _context;

            public SceneLoadStep(ISceneLoaderService sceneLoader, SceneLoadingContext context)
            {
                _sceneLoader = sceneLoader;
                _context = context;
            }

            public string Message => "Loading scene";
            public float Weight => 2f;
            public int Order => 0;

            public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                if (_sceneLoader == null || _context.SceneReference == null)
                {
                    progress?.Report(1f);
                    return;
                }

                var previousActiveSceneName = _sceneLoader?.CurrentSceneName;
                var previousActiveScene = string.IsNullOrEmpty(previousActiveSceneName)
                    ? default
                    : SceneManager.GetSceneByName(previousActiveSceneName);
                var loadMode = _context.Mode == LoadSceneMode.Single
                    ? LoadSceneMode.Additive
                    : _context.Mode;

                void OnSceneProgress(SceneLoadProgress info)
                {
                    if (!string.Equals(info.Info.SceneKey, _context.SceneKey, StringComparison.Ordinal))
                    {
                        return;
                    }

                    progress?.Report(info.Progress);
                }

                _sceneLoader.SceneLoadProgress += OnSceneProgress;
                try
                {
                    progress?.Report(0f);
                    _context.SceneInstance = await _sceneLoader.LoadAsync(
                        _context.SceneReference,
                        loadMode,
                        _context.ActivateOnLoad,
                        _context.Priority,
                        _context.Mode == LoadSceneMode.Single,
                        cancellationToken);

                    if (_context.SceneInstance.Scene.IsValid())
                    {
                        SceneManager.SetActiveScene(_context.SceneInstance.Scene);
                    }

                    if (_context.Mode == LoadSceneMode.Single &&
                        previousActiveScene.IsValid() &&
                        previousActiveScene.isLoaded &&
                        previousActiveScene != _context.SceneInstance.Scene)
                    {
                        await SceneManager.UnloadSceneAsync(previousActiveScene)
                            .ToUniTask(cancellationToken: cancellationToken);
                    }

                    progress?.Report(1f);
                }
                finally
                {
                    _sceneLoader.SceneLoadProgress -= OnSceneProgress;
                }
            }
        }

        private sealed class SceneStepsStep : ILoadingStep
        {
            private readonly SceneLoadingContext _context;

            public SceneStepsStep(SceneLoadingContext context)
            {
                _context = context;
            }

            public string Message => "Initializing scene";
            public float Weight => 2f;
            public int Order => 10;

            public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                progress?.Report(0f);
                var steps = await WaitForSceneStepsAsync(cancellationToken);
                if (steps == null || steps.Count == 0)
                {
                    progress?.Report(1f);
                    return;
                }

                var totalWeight = GetTotalWeight(steps);
                var completedWeight = 0f;
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    if (step == null)
                    {
                        continue;
                    }

                    var weight = GetWeight(step);
                    var localValue = 0f;
                    void ReportLocal(float value)
                    {
                        localValue = Clamp01(value);
                        var total = Clamp01((completedWeight + localValue * weight) / totalWeight);
                        progress?.Report(total);
                    }

                    ReportLocal(0f);
                    await step.Execute(new Progress<float>(ReportLocal), cancellationToken);
                    ReportLocal(1f);
                    completedWeight += weight;
                }
            }

            private async UniTask<List<ILoadingStep>> WaitForSceneStepsAsync(CancellationToken cancellationToken)
            {
                var scope = await WaitForSceneScopeAsync(cancellationToken);
                if (scope == null || scope.Container == null)
                {
                    return null;
                }

                try
                {
                    var sceneSteps = scope.Container.Resolve<IReadOnlyList<IGameplayLoadingStep>>();
                    if (sceneSteps == null)
                    {
                        return new List<ILoadingStep>();
                    }

                    var list = new List<ILoadingStep>(sceneSteps.Count);
                    for (var j = 0; j < sceneSteps.Count; j++)
                    {
                        list.Add(sceneSteps[j]);
                    }
                    return list;
                }
                catch
                {
                    return null;
                }
            }

            private async UniTask<LifetimeScope> WaitForSceneScopeAsync(CancellationToken cancellationToken)
            {
                const int MAX_WAIT_FRAMES = 120;
                for (var frame = 0; frame < MAX_WAIT_FRAMES; frame++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var scope = TryGetSceneScope();
                    if (scope != null && scope.Container != null)
                    {
                        return scope;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                return TryGetSceneScope();
            }

            private LifetimeScope TryGetSceneScope()
            {
                var scene = _context.SceneInstance.Scene;
                if (!scene.IsValid())
                {
                    return null;
                }

                var roots = scene.GetRootGameObjects();
                for (var i = 0; i < roots.Length; i++)
                {
                    var scope = roots[i].GetComponentInChildren<LifetimeScope>(true);
                    if (scope != null)
                    {
                        return scope;
                    }
                }

                return null;
            }

            private static float GetTotalWeight(IReadOnlyList<ILoadingStep> steps)
            {
                var total = 0f;
                for (var i = 0; i < steps.Count; i++)
                {
                    total += GetWeight(steps[i]);
                }

                return total <= 0f ? 1f : total;
            }

            private static float GetWeight(ILoadingStep step)
            {
                if (step == null || step.Weight <= 0f)
                {
                    return 1f;
                }

                return step.Weight;
            }

            private static float Clamp01(float value)
            {
                if (value < 0f)
                {
                    return 0f;
                }

                return value > 1f ? 1f : value;
            }
        }

        private static string ResolveTransitionSceneName(IConfigService configService)
        {
            if (configService == null)
            {
                return null;
            }

            var configType = FindTypeByName("Evo.Infrastructure.Runtime.Config.ProjectConfig");
            if (configType == null)
            {
                return null;
            }

            if (!configService.TryGet(configType, out var config) || config == null)
            {
                return null;
            }

            var property = configType.GetProperty("TransitionSceneName", BindingFlags.Public | BindingFlags.Instance);
            if (property == null || property.PropertyType != typeof(string))
            {
                return null;
            }

            return property.GetValue(config) as string;
        }

        private static T TryResolve<T>(IObjectResolver resolver)
            where T : class
        {
            if (resolver == null)
            {
                return null;
            }

            try
            {
                return resolver.TryResolve(typeof(T), out var resolved)
                    ? resolved as T
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static Type FindTypeByName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
