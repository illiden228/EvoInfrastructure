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
        private const string SourceName = nameof(SceneLoadingPipeline);

        private readonly ISceneLoaderService _sceneLoader;
        private readonly ILoadingProgress _progress;
        private readonly ILoadingPresentation _loadingPresentation;
        private readonly SceneTransitionOptions _transitionOptions;
        private readonly LoadingExecutionOptions _executionOptions;
        private readonly string _transitionSceneName;
        private readonly SemaphoreSlim _loadGate = new(1, 1);

        public SceneLoadingPipeline(ISceneLoaderService sceneLoader, ILoadingProgress progress, IConfigService configService)
            : this(sceneLoader, progress, configService, null, null)
        {
        }

        [Inject]
        public SceneLoadingPipeline(
            ISceneLoaderService sceneLoader,
            ILoadingProgress progress,
            IConfigService configService,
            IObjectResolver resolver,
            LoadingExecutionOptions executionOptions)
        {
            _sceneLoader = sceneLoader;
            _progress = progress;
            _loadingPresentation = TryResolve<ILoadingPresentation>(resolver);
            _transitionOptions = TryResolve<SceneTransitionOptions>(resolver) ?? new SceneTransitionOptions();
            _executionOptions = executionOptions ??
                                TryResolve<LoadingExecutionOptions>(resolver) ??
                                new LoadingExecutionOptions();
            _transitionSceneName = !string.IsNullOrWhiteSpace(_transitionOptions.TransitionSceneName)
                ? _transitionOptions.TransitionSceneName
                : ResolveTransitionSceneName(configService);
        }

        public IReadOnlyList<ILoadingStep> CreateSteps(
            AssetReference sceneReference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100)
        {
            return CreateSteps(
                sceneReference,
                mode,
                activateOnLoad,
                priority,
                unloadPreviousAfterTargetLoad: true,
                previousActiveScene: SceneManager.GetActiveScene());
        }

        private IReadOnlyList<ILoadingStep> CreateSteps(
            AssetReference sceneReference,
            LoadSceneMode mode,
            bool activateOnLoad,
            int priority,
            bool unloadPreviousAfterTargetLoad,
            UnityEngine.SceneManagement.Scene previousActiveScene)
        {
            if (sceneReference == null)
            {
                return Array.Empty<ILoadingStep>();
            }

            var context = new SceneLoadingContext(
                sceneReference,
                mode,
                activateOnLoad,
                priority,
                unloadPreviousAfterTargetLoad,
                previousActiveScene,
                _executionOptions);

            return new ILoadingStep[]
            {
                new SceneLoadStep(_sceneLoader, context),
                new SceneStepsStep(context),
                new SceneCommitStep(context)
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

            await _loadGate.WaitAsync(cancellationToken);
            using var operationTimeout = CreateTimeoutTokenSource(
                cancellationToken,
                _executionOptions.EnableOperationTimeout
                    ? _executionOptions.OperationTimeoutSeconds
                    : 0f);
            var operationToken = operationTimeout?.Token ?? cancellationToken;
            var recoveryScene = SceneManager.GetActiveScene();
            var useTransition = mode == LoadSceneMode.Single && !string.IsNullOrEmpty(_transitionSceneName);
            var transitionSceneActivated = false;

            try
            {
                await ShowLoadingPresentationIfNeeded(operationToken);
                _progress?.NotifyReady();
                _progress?.NotifyStarted();

                if (useTransition)
                {
                    transitionSceneActivated = await LoadAndActivateTransitionSceneAsync(operationToken);
                    if (!transitionSceneActivated)
                    {
                        EvoDebug.LogWarning(
                            $"Transition scene '{_transitionSceneName}' was not activated. Falling back to target-first scene load.",
                            SourceName);
                    }
                }

                var attempts = Math.Max(0, _executionOptions.OperationRetryCount) + 1;
                for (var attempt = 1; attempt <= attempts; attempt++)
                {
                    try
                    {
                        var runner = new LoadingRunner(_executionOptions);
                        var steps = CreateSteps(
                            sceneReference,
                            mode,
                            activateOnLoad,
                            priority,
                            unloadPreviousAfterTargetLoad: true,
                            previousActiveScene: recoveryScene);

                        await runner.RunAsync(steps, _progress, operationToken, notifyLifecycle: false);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        await CleanupFailedTargetAsync(sceneReference, recoveryScene, CancellationToken.None);
                        throw;
                    }
                    catch (Exception ex) when (attempt < attempts)
                    {
                        await CleanupFailedTargetAsync(sceneReference, recoveryScene, operationToken);
                        EvoDebug.LogWarning(
                            $"Scene loading pipeline attempt {attempt}/{attempts} failed. Retrying. {ex.GetType().Name}: {ex.Message}",
                            SourceName);
                        if (_executionOptions.RetryDelaySeconds > 0f)
                        {
                            await UniTask.Delay(
                                TimeSpan.FromSeconds(_executionOptions.RetryDelaySeconds),
                                cancellationToken: operationToken);
                        }
                    }
                    catch
                    {
                        await CleanupFailedTargetAsync(sceneReference, recoveryScene, CancellationToken.None);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                operationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Scene loading pipeline timed out after {_executionOptions.OperationTimeoutSeconds:0.###} seconds.");
            }
            finally
            {
                _progress?.NotifyFinished();
                if (transitionSceneActivated)
                {
                    await UnloadTransitionSceneAsync();
                }

                await HideLoadingPresentationIfNeeded();
                _loadGate.Release();
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

            using var timeout = CreateTimeoutTokenSource(
                cancellationToken,
                _executionOptions.PresentationTimeoutSeconds);
            try
            {
                await _loadingPresentation.ShowAsync(timeout?.Token ?? cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                EvoDebug.LogWarning(
                    $"Loading presentation timed out after {_executionOptions.PresentationTimeoutSeconds:0.###} seconds. Continuing without it.",
                    SourceName);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Loading presentation failed. Continuing without it. {ex.Message}",
                    SourceName);
            }
        }

        private async UniTask HideLoadingPresentationIfNeeded()
        {
            if (_loadingPresentation == null ||
                _transitionOptions == null ||
                !_transitionOptions.HideLoadingPresentationAfterLoadingFinished)
            {
                return;
            }

            using var timeout = CreateTimeoutTokenSource(
                CancellationToken.None,
                _executionOptions.PresentationTimeoutSeconds);
            try
            {
                await _loadingPresentation.HideAsync(timeout?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) when (timeout != null && timeout.IsCancellationRequested)
            {
                EvoDebug.LogWarning(
                    $"Loading presentation hide timed out after {_executionOptions.PresentationTimeoutSeconds:0.###} seconds.",
                    SourceName);
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning($"Loading presentation hide failed or timed out. {ex.Message}", SourceName);
            }
        }

        private async UniTask<bool> LoadAndActivateTransitionSceneAsync(CancellationToken cancellationToken)
        {
            var existingScene = SceneManager.GetSceneByName(_transitionSceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                EvoDebug.Log(
                    $"Transition scene loaded: '{_transitionSceneName}' already loaded.",
                    SourceName);
                return TrySetTransitionSceneActive(existingScene);
            }

            var transitionLoad = SceneManager.LoadSceneAsync(_transitionSceneName, LoadSceneMode.Additive);
            if (transitionLoad == null)
            {
                EvoDebug.LogWarning(
                    $"Transition scene '{_transitionSceneName}' failed to start loading.",
                    SourceName);
                return false;
            }

            try
            {
                using var timeout = CreateTimeoutTokenSource(
                    cancellationToken,
                    _executionOptions.TransitionTimeoutSeconds);
                await transitionLoad.ToUniTask(cancellationToken: timeout?.Token ?? cancellationToken);
                var scene = SceneManager.GetSceneByName(_transitionSceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    EvoDebug.LogWarning(
                        $"Transition scene '{_transitionSceneName}' load completed but scene is not loaded.",
                        SourceName);
                    return false;
                }

                EvoDebug.Log(
                    $"Transition scene loaded: '{_transitionSceneName}'.",
                    SourceName);

                return TrySetTransitionSceneActive(scene);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                EvoDebug.LogWarning(
                    $"Transition scene load timed out after {_executionOptions.TransitionTimeoutSeconds:0.###} seconds.",
                    SourceName);
                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Transition scene load failed. {ex.Message}",
                    SourceName);
                return false;
            }
        }

        private bool TrySetTransitionSceneActive(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (!SceneManager.SetActiveScene(scene))
            {
                EvoDebug.LogWarning(
                    $"Transition scene '{scene.name}' failed to become active.",
                    SourceName);
                return false;
            }

            EvoDebug.Log(
                $"Transition scene set active: '{scene.name}'.",
                SourceName);
            return true;
        }

        private async UniTask UnloadTransitionSceneAsync()
        {
            var scene = SceneManager.GetSceneByName(_transitionSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (SceneManager.sceneCount <= 1)
            {
                EvoDebug.LogWarning(
                    $"Transition scene '{_transitionSceneName}' was not unloaded because it is the only loaded scene.",
                    SourceName);
                return;
            }

            try
            {
                var unloadOperation = SceneManager.UnloadSceneAsync(scene);
                if (unloadOperation != null)
                {
                    using var timeout = CreateTimeoutTokenSource(
                        CancellationToken.None,
                        _executionOptions.TransitionTimeoutSeconds);
                    await unloadOperation.ToUniTask(
                        cancellationToken: timeout?.Token ?? CancellationToken.None);
                    EvoDebug.Log(
                        $"Transition scene unloaded: '{_transitionSceneName}'.",
                        SourceName);
                }
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Transition scene unload failed. {ex.Message}",
                    SourceName);
            }
        }

        private async UniTask CleanupFailedTargetAsync(
            AssetReference sceneReference,
            UnityEngine.SceneManagement.Scene recoveryScene,
            CancellationToken cancellationToken)
        {
            try
            {
                var transitionScene = string.IsNullOrEmpty(_transitionSceneName)
                    ? default
                    : SceneManager.GetSceneByName(_transitionSceneName);
                if (transitionScene.IsValid() && transitionScene.isLoaded)
                {
                    SceneManager.SetActiveScene(transitionScene);
                }
                else if (recoveryScene.IsValid() && recoveryScene.isLoaded)
                {
                    SceneManager.SetActiveScene(recoveryScene);
                }

                var key = GetReferenceKey(sceneReference);
                if (!string.IsNullOrEmpty(key))
                {
                    using var timeout = CreateTimeoutTokenSource(
                        cancellationToken,
                        _executionOptions.TransitionTimeoutSeconds);
                    await _sceneLoader.UnloadAsync(key, timeout?.Token ?? cancellationToken);
                }
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Failed target cleanup was not fully completed. {ex.Message}",
                    SourceName);
            }
        }

        private sealed class SceneLoadingContext
        {
            public readonly AssetReference SceneReference;
            public readonly LoadSceneMode Mode;
            public readonly bool ActivateOnLoad;
            public readonly int Priority;
            public readonly bool UnloadPreviousAfterTargetLoad;
            public readonly UnityEngine.SceneManagement.Scene PreviousActiveScene;
            public readonly LoadingExecutionOptions ExecutionOptions;
            public SceneInstance SceneInstance;
            public string SceneKey;

            public SceneLoadingContext(
                AssetReference sceneReference,
                LoadSceneMode mode,
                bool activateOnLoad,
                int priority,
                bool unloadPreviousAfterTargetLoad,
                UnityEngine.SceneManagement.Scene previousActiveScene,
                LoadingExecutionOptions executionOptions)
            {
                SceneReference = sceneReference;
                Mode = mode;
                ActivateOnLoad = activateOnLoad;
                Priority = priority;
                UnloadPreviousAfterTargetLoad = unloadPreviousAfterTargetLoad;
                PreviousActiveScene = previousActiveScene;
                ExecutionOptions = executionOptions ?? new LoadingExecutionOptions();
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
                        EvoDebug.Log(
                            $"Target scene loaded: '{_context.SceneInstance.Scene.name}'.",
                            SourceName);

                        if (SceneManager.SetActiveScene(_context.SceneInstance.Scene))
                        {
                            EvoDebug.Log(
                                $"Target scene set active: '{_context.SceneInstance.Scene.name}'.",
                                SourceName);
                        }
                        else
                        {
                            EvoDebug.LogWarning(
                                $"Target scene '{_context.SceneInstance.Scene.name}' failed to become active.",
                                SourceName);
                        }
                    }

                    progress?.Report(1f);
                }
                finally
                {
                    _sceneLoader.SceneLoadProgress -= OnSceneProgress;
                }
            }
        }

        private sealed class SceneCommitStep : ILoadingStep
        {
            private readonly SceneLoadingContext _context;

            public SceneCommitStep(SceneLoadingContext context)
            {
                _context = context;
            }

            public string Message => "Finalizing scene transition";
            public float Weight => 0.2f;
            public int Order => 100;

            public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                progress?.Report(0f);
                var previous = _context.PreviousActiveScene;
                if (_context.Mode == LoadSceneMode.Single &&
                    _context.UnloadPreviousAfterTargetLoad &&
                    previous.IsValid() &&
                    previous.isLoaded &&
                    previous != _context.SceneInstance.Scene)
                {
                    var unload = SceneManager.UnloadSceneAsync(previous);
                    if (unload != null)
                    {
                        await unload.ToUniTask(cancellationToken: cancellationToken);
                    }
                }

                progress?.Report(1f);
            }
        }

        private sealed class SceneStepsStep : ILoadingStep, ILoadingStepTimeout
        {
            private readonly SceneLoadingContext _context;

            public SceneStepsStep(SceneLoadingContext context)
            {
                _context = context;
            }

            public string Message => "Initializing scene";
            public float Weight => 2f;
            public int Order => 10;
            public float TimeoutSeconds => -1f;

            public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
            {
                progress?.Report(0f);
                var steps = await WaitForSceneStepsAsync(cancellationToken);
                if (steps == null || steps.Count == 0)
                {
                    progress?.Report(1f);
                    return;
                }

                steps = LoadingStepOrdering.Prepare(
                    steps,
                    _context.ExecutionOptions.StepOrderMode);
                EvoDebug.Log(
                    LoadingStepOrdering.FormatPlan(
                        steps,
                        _context.ExecutionOptions.StepOrderMode,
                        _context.ExecutionOptions,
                        "Gameplay Loading Plan"),
                    SourceName);

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
                    await LoadingStepExecution.ExecuteAsync(
                        step,
                        new Progress<float>(ReportLocal),
                        _context.ExecutionOptions,
                        cancellationToken,
                        cancellationToken);
                    ReportLocal(1f);
                    completedWeight += weight;
                }
            }

            private async UniTask<List<ILoadingStep>> WaitForSceneStepsAsync(CancellationToken cancellationToken)
            {
                var scope = await WaitForSceneScopeAsync(cancellationToken);
                if (scope == null || scope.Container == null)
                {
                    throw new TimeoutException(
                        $"Scene scope for '{_context.SceneInstance.Scene.name}' was not ready within 120 frames.");
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
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to resolve gameplay loading steps for scene '{_context.SceneInstance.Scene.name}'.",
                        ex);
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

            var configType = FindTypeByName("Game.Runtime.Config.ProjectConfig") ??
                             FindTypeByName("Evo.Infrastructure.Runtime.Config.ProjectConfig");
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

        private static CancellationTokenSource CreateTimeoutTokenSource(
            CancellationToken parentToken,
            float timeoutSeconds)
        {
            if (timeoutSeconds <= 0f)
            {
                return null;
            }

            var source = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            return source;
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
