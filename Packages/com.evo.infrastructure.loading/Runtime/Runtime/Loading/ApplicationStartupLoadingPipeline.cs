using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Core.Async;
using Evo.Infrastructure.Services.Debug;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;

namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class ApplicationStartupLoadingPipeline : IApplicationStartupLoadingPipeline
    {
        private const string SourceName = nameof(ApplicationStartupLoadingPipeline);

        private readonly ISceneLoadingPipeline _sceneLoadingPipeline;
        private readonly ILoadingProgress _progress;
        private readonly ILoadingPresentation _loadingPresentation;
        private readonly SceneTransitionOptions _transitionOptions;
        private readonly LoadingExecutionOptions _executionOptions;
        private readonly StartupLoadingOptions _startupOptions;
        private readonly AsyncGate _loadGate = new();

        public ApplicationStartupLoadingPipeline(
            ISceneLoadingPipeline sceneLoadingPipeline,
            ILoadingProgress progress,
            LoadingExecutionOptions executionOptions = null,
            SceneTransitionOptions transitionOptions = null,
            StartupLoadingOptions startupOptions = null,
            ILoadingPresentation loadingPresentation = null)
        {
            _sceneLoadingPipeline = sceneLoadingPipeline;
            _progress = progress;
            _executionOptions = executionOptions ?? new LoadingExecutionOptions();
            _transitionOptions = transitionOptions ?? new SceneTransitionOptions();
            _startupOptions = startupOptions ?? new StartupLoadingOptions();
            _loadingPresentation = loadingPresentation;
        }

        [Inject]
        public ApplicationStartupLoadingPipeline(
            ISceneLoadingPipeline sceneLoadingPipeline,
            ILoadingProgress progress,
            LoadingExecutionOptions executionOptions,
            SceneTransitionOptions transitionOptions,
            StartupLoadingOptions startupOptions,
            IObjectResolver resolver)
            : this(
                sceneLoadingPipeline,
                progress,
                executionOptions,
                transitionOptions,
                startupOptions,
                TryResolve<ILoadingPresentation>(resolver))
        {
        }

        public async UniTask LoadStartupAsync(
            IReadOnlyList<ILoadingStep> bootstrapSteps,
            AssetReference startupScene,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            CancellationToken cancellationToken = default)
        {
            if (!_startupOptions.Enabled)
            {
                return;
            }

            await UniTask.SwitchToMainThread();
            using var loadLease = await _loadGate.EnterAsync(cancellationToken);
            await UniTask.SwitchToMainThread();
            using var operationTimeout = LoadingTimeoutScope.Create(
                cancellationToken,
                _executionOptions.EnableOperationTimeout
                    ? _executionOptions.OperationTimeoutSeconds
                    : 0f,
                _executionOptions);
            var operationToken = operationTimeout?.Token ?? cancellationToken;

            try
            {
                await ShowLoadingPresentationIfNeeded(operationToken);
                _progress?.NotifyReady();
                _progress?.NotifyStarted();

                var sceneSteps = _sceneLoadingPipeline?.CreateSteps(startupScene, mode, activateOnLoad, priority);
                await RunStartupStepsAsync(bootstrapSteps, sceneSteps, operationToken, cancellationToken);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                operationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Application startup loading timed out after {_executionOptions.OperationTimeoutSeconds:0.###} seconds.");
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                _progress?.NotifyFinished();
                await HideLoadingPresentationIfNeeded();
            }
        }

        private async UniTask RunStartupStepsAsync(
            IReadOnlyList<ILoadingStep> bootstrapSteps,
            IReadOnlyList<ILoadingStep> sceneSteps,
            CancellationToken operationToken,
            CancellationToken cancellationToken)
        {
            var bootstrap = LoadingStepOrdering.Prepare(bootstrapSteps, _executionOptions.StepOrderMode);
            var scene = LoadingStepOrdering.Prepare(sceneSteps, _executionOptions.StepOrderMode);
            var stepCount = bootstrap.Count + scene.Count;
            if (stepCount == 0)
            {
                _progress?.Report(1f, "Ready", 0, 0);
                return;
            }

            var totalWeight = GetTotalWeight(bootstrap) + GetTotalWeight(scene);
            if (totalWeight <= 0f)
            {
                _progress?.Report(1f, "Ready", 0, 0);
                return;
            }

            var completedWeight = 0f;
            var stepIndex = 0;
            await RunStepGroupAsync(
                bootstrap,
                totalWeight,
                completedWeight,
                stepIndex,
                stepCount,
                operationToken,
                cancellationToken);
            completedWeight += GetTotalWeight(bootstrap);
            stepIndex += bootstrap.Count;
            await RunStepGroupAsync(
                scene,
                totalWeight,
                completedWeight,
                stepIndex,
                stepCount,
                operationToken,
                cancellationToken);
        }

        private async UniTask RunStepGroupAsync(
            IReadOnlyList<ILoadingStep> steps,
            float totalWeight,
            float completedBeforeGroup,
            int firstStepIndex,
            int stepCount,
            CancellationToken operationToken,
            CancellationToken cancellationToken)
        {
            var completedWeight = completedBeforeGroup;
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                {
                    continue;
                }

                var weight = GetWeight(step);
                var message = step.Message;
                var currentStepIndex = firstStepIndex + i;
                var localValue = 0f;
                var stepActive = true;
                void ReportLocal(float value)
                {
                    if (!stepActive)
                    {
                        return;
                    }

                    localValue = Clamp01(value);
                    var total = Clamp01((completedWeight + localValue * weight) / totalWeight);
                    _progress?.Report(total, message, currentStepIndex, stepCount);
                }

                ReportLocal(0f);
                await LoadingStepExecution.ExecuteAsync(
                    step,
                    new ImmediateProgress(ReportLocal),
                    _executionOptions,
                    operationToken,
                    cancellationToken);
                ReportLocal(1f);
                stepActive = false;

                completedWeight += weight;
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

            using var timeout = LoadingTimeoutScope.Create(
                cancellationToken,
                _executionOptions.PresentationTimeoutSeconds,
                _executionOptions);
            try
            {
                await _loadingPresentation.ShowAsync(timeout?.Token ?? cancellationToken);
            }
            catch (OperationCanceledException) when (timeout?.IsTimeoutRequested == true)
            {
                EvoDebug.LogWarning(
                    $"Startup loading presentation timed out after {_executionOptions.PresentationTimeoutSeconds:0.###} seconds. Continuing without it.",
                    SourceName);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Startup loading presentation failed. Continuing without it. {ex.Message}",
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

            using var timeout = LoadingTimeoutScope.Create(
                CancellationToken.None,
                _executionOptions.PresentationTimeoutSeconds,
                _executionOptions);
            try
            {
                await _loadingPresentation.HideAsync(timeout?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) when (timeout != null && timeout.IsTimeoutRequested)
            {
                EvoDebug.LogWarning(
                    $"Startup loading presentation hide timed out after {_executionOptions.PresentationTimeoutSeconds:0.###} seconds.",
                    SourceName);
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning($"Startup loading presentation hide failed or timed out. {ex.Message}", SourceName);
            }
        }

        private static float GetTotalWeight(IReadOnlyList<ILoadingStep> steps)
        {
            var total = 0f;
            for (var i = 0; i < steps.Count; i++)
            {
                total += GetWeight(steps[i]);
            }

            return total;
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

        private sealed class ImmediateProgress : IProgress<float>
        {
            private readonly Action<float> _report;

            public ImmediateProgress(Action<float> report)
            {
                _report = report;
            }

            public void Report(float value)
            {
                _report?.Invoke(value);
            }
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

    }
}
