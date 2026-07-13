using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Runtime.Loading
{
    internal static class LoadingStepExecution
    {
        public static float ResolveTimeoutSeconds(
            ILoadingStep step,
            LoadingExecutionOptions options)
        {
            if (step is ILoadingStepTimeout timedStep)
            {
                if (timedStep.TimeoutSeconds < 0f)
                {
                    return 0f;
                }

                if (timedStep.TimeoutSeconds > 0f)
                {
                    return timedStep.TimeoutSeconds;
                }
            }

            return options != null && options.EnableStepTimeout
                ? Math.Max(0f, options.StepTimeoutSeconds)
                : 0f;
        }

        public static async UniTask ExecuteAsync(
            ILoadingStep step,
            IProgress<float> progress,
            LoadingExecutionOptions options,
            CancellationToken operationToken,
            CancellationToken callerToken)
        {
            var timeoutSeconds = ResolveTimeoutSeconds(step, options);
            using var timeout = LoadingTimeoutScope.Create(operationToken, timeoutSeconds, options);
            var stepToken = timeout?.Token ?? operationToken;
            try
            {
                await step.Execute(progress, stepToken)
                    .AttachExternalCancellation(stepToken);
            }
            catch (OperationCanceledException) when (
                !callerToken.IsCancellationRequested &&
                stepToken.IsCancellationRequested)
            {
                if (operationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Loading operation timed out while executing '{step.Message}'.");
                }

                throw new TimeoutException(
                    $"Loading step '{step.Message}' timed out after {timeoutSeconds:0.###} seconds.");
            }
        }

    }
}
