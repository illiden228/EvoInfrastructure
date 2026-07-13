using System;

namespace Evo.Infrastructure.Runtime.Loading
{
    public enum LoadingStepOrderMode
    {
        OrderProperty = 0,
        Registration = 1
    }

    [Serializable]
    public sealed class LoadingExecutionOptions
    {
        public bool EnableStepTimeout = true;
        public float StepTimeoutSeconds = 45f;
        public bool EnableOperationTimeout = true;
        public float OperationTimeoutSeconds = 180f;
        public float PresentationTimeoutSeconds = 15f;
        public float TransitionTimeoutSeconds = 30f;
        public bool IgnoreTimeoutWhenApplicationNotFocused = true;
        public int OperationRetryCount = 1;
        public float RetryDelaySeconds = 0.75f;
        public LoadingStepOrderMode StepOrderMode = LoadingStepOrderMode.OrderProperty;
    }
}
