using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EvoDebug = Evo.Infrastructure.Services.Debug.EvoDebug;

namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class LoadingRunner
    {
        private readonly LoadingExecutionOptions _options;

        public LoadingRunner(LoadingExecutionOptions options = null)
        {
            _options = options ?? new LoadingExecutionOptions();
        }

        public async UniTask RunAsync(
            IReadOnlyList<ILoadingStep> steps,
            ILoadingProgress progress,
            CancellationToken cancellationToken,
            bool notifyLifecycle = true)
        {
            if (progress == null)
            {
                return;
            }

            await UniTask.SwitchToMainThread();

            using var operationTimeout = LoadingTimeoutScope.Create(
                cancellationToken,
                _options.EnableOperationTimeout ? _options.OperationTimeoutSeconds : 0f,
                _options);
            var operationToken = operationTimeout?.Token ?? cancellationToken;

            if (notifyLifecycle)
            {
                progress.NotifyReady();
                progress.NotifyStarted();
            }
            try
            {
                if (steps == null || steps.Count == 0)
                {
                    progress.Report(1f, "Ready", 0, 0);
                    return;
                }

                var ordered = LoadingStepOrdering.Prepare(steps, _options.StepOrderMode);
                EvoDebug.Log(
                    LoadingStepOrdering.FormatPlan(
                        ordered,
                        _options.StepOrderMode,
                        _options,
                        "Loading Plan"),
                    "Loading");
                var totalWeight = GetTotalWeight(ordered);
                if (totalWeight <= 0f)
                {
                    progress.Report(1f, "Ready", 0, 0);
                    return;
                }

                var completedWeight = 0f;
                for (var i = 0; i < ordered.Count; i++)
                {
                    var step = ordered[i];
                    if (step == null)
                    {
                        continue;
                    }

                    var weight = GetWeight(step);
                    var message = step.Message;
                    EvoDebug.Log($"Step {i + 1}/{ordered.Count}. {message} start loading", "Loading");

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
                        progress.Report(total, message, i, ordered.Count);
                        EvoDebug.Log($"Loading progress: {total:0.00}", "Loading");
                    }

                    ReportLocal(0f);
                    var localProgress = new ImmediateProgress(ReportLocal);
                    await LoadingStepExecution.ExecuteAsync(
                        step,
                        localProgress,
                        _options,
                        operationToken,
                        cancellationToken);
                    ReportLocal(1f);
                    stepActive = false;

                    completedWeight += weight;
                    EvoDebug.Log($"Step {i + 1}/{ordered.Count}. {message} completed", "Loading");
                }
            }
            finally
            {
                if (notifyLifecycle)
                {
                    await UniTask.SwitchToMainThread();
                    progress.NotifyFinished();
                }
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

            if (value > 1f)
            {
                return 1f;
            }

            return value;
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
    }
}
