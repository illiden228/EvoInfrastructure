using System;

namespace Evo.Infrastructure.Services.SceneLoader
{
    [Serializable]
    public sealed class SceneLoaderOptions
    {
        public bool verboseLogging;
        public bool enableTimeout = true;
        public float timeoutSeconds = 45f;
        public bool ignoreTimeoutWhenApplicationNotFocused = true;
        public float completedProgressThreshold = 0.99f;
        public float finalizationTimeoutSeconds = 180f;
        public int retryCount = 1;
        public float retryDelaySeconds = 0.5f;
    }
}
