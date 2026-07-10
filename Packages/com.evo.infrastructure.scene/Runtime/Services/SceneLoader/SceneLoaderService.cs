using System;
using System.Diagnostics;
using System.Threading;
using Evo.Infrastructure.Services.Debug;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.ResourceLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Evo.Infrastructure.Services.SceneLoader
{
    public sealed class SceneLoaderService : ISceneLoaderService
    {
        private static long _loadOperationSequence;

        private readonly IResourceLoaderService _resourceLoader;
        private readonly SceneLoaderOptions _options;
        private string _lastSceneKey;
        private AssetReference _lastSceneReference;
        private bool _isApplicationFocused = true;

        public event Action<SceneLoadInfo> SceneLoadStarted;
        public event Action<SceneLoadProgress> SceneLoadProgress;
        public event Action<SceneLoadInfo> SceneLoadFinished;
        public event Action<string> ActiveSceneChanged;
        public string CurrentSceneName { get; private set; }

        public SceneLoaderService(IResourceLoaderService resourceLoader)
            : this(resourceLoader, null)
        {
        }

        public SceneLoaderService(IResourceLoaderService resourceLoader, SceneLoaderOptions options)
        {
            _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
            _options = NormalizeOptions(options);
            _isApplicationFocused = UnityEngine.Application.isFocused;
            CurrentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            UnityEngine.Application.focusChanged += OnApplicationFocusChanged;
        }

        public UniTask<SceneInstance> LoadAsync(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                EvoDebug.LogError("LoadAsync called with empty key.", nameof(SceneLoaderService));
            }
            _lastSceneKey = key;
            _lastSceneReference = null;
            var info = new SceneLoadInfo(
                key,
                mode,
                activateOnLoad,
                priority,
                SceneLoadSource.Key,
                forceReload);
            return LoadWithRetry(
                () => _resourceLoader.LoadSceneHandle(
                    key,
                    mode,
                    activateOnLoad,
                    priority,
                    forceReload),
                info,
                cancellationToken);
        }

        public UniTask<SceneInstance> LoadAsync(
            AssetReference reference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default)
        {
            if (reference == null)
            {
                EvoDebug.LogError("LoadAsync called with null reference.", nameof(SceneLoaderService));
            }
            _lastSceneKey = null;
            _lastSceneReference = reference;
            var sceneKey = GetReferenceKey(reference);
            var info = new SceneLoadInfo(
                sceneKey,
                mode,
                activateOnLoad,
                priority,
                SceneLoadSource.Reference,
                forceReload);
            return LoadWithRetry(
                () => _resourceLoader.LoadSceneHandle(
                    reference,
                    mode,
                    activateOnLoad,
                    priority,
                    forceReload),
                info,
                cancellationToken);
        }

        public UniTask UnloadAsync(string key, CancellationToken cancellationToken = default)
        {
            return _resourceLoader.UnloadSceneAsync(key, cancellationToken);
        }

        public UniTask UnloadByNameAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                EvoDebug.LogWarning("UnloadByNameAsync called with empty scene name.", nameof(SceneLoaderService));
                return UniTask.CompletedTask;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EvoDebug.LogWarning($"UnloadByNameAsync scene not loaded: '{sceneName}'.", nameof(SceneLoaderService));
                return UniTask.CompletedTask;
            }

            return SceneManager.UnloadSceneAsync(scene).ToUniTask(cancellationToken: cancellationToken);
        }

        public bool IsLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public async UniTask ReloadActiveAsync(CancellationToken cancellationToken = default)
        {
            var activeScene = SceneManager.GetActiveScene();
            LogVerbose(
                $"ReloadActiveAsync start. Active='{activeScene.name}', BuildIndex={activeScene.buildIndex}, " +
                $"LastKey='{_lastSceneKey}', LastRefNull={_lastSceneReference == null}");

            if (_lastSceneReference != null)
            {
                var runtimeKey = GetReferenceKey(_lastSceneReference);
                if (!string.IsNullOrEmpty(runtimeKey))
                {
                    try
                    {
                        var info = new SceneLoadInfo(
                            runtimeKey,
                            LoadSceneMode.Single,
                            true,
                            100,
                            SceneLoadSource.Reference,
                            true);
                        await LoadWithRetry(
                            () => _resourceLoader.LoadSceneHandle(
                                _lastSceneReference,
                                LoadSceneMode.Single,
                                true,
                                100,
                                true),
                            info,
                            cancellationToken);
                        return;
                    }
                    catch (Exception ex)
                    {
                        EvoDebug.LogWarning(
                            $"Failed to reload by reference runtime key '{runtimeKey}'. {ex.Message}",
                            nameof(SceneLoaderService));
                    }
                }
            }

            if (!string.IsNullOrEmpty(_lastSceneKey))
            {
                try
                {
                    var info = new SceneLoadInfo(
                        _lastSceneKey,
                        LoadSceneMode.Single,
                        true,
                        100,
                        SceneLoadSource.Key,
                        true);
                    await LoadWithRetry(
                        () => _resourceLoader.LoadSceneHandle(
                            _lastSceneKey,
                            LoadSceneMode.Single,
                            true,
                            100,
                            true),
                        info,
                        cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    EvoDebug.LogWarning(
                        $"Failed to reload by key '{_lastSceneKey}'. {ex.Message}",
                        nameof(SceneLoaderService));
                }
            }

            if (!activeScene.IsValid())
            {
                EvoDebug.LogWarning("ReloadActiveAsync has no valid active scene.", nameof(SceneLoaderService));
                return;
            }

            await LoadViaSceneManager(activeScene.name, cancellationToken);
        }

        private async UniTask<SceneInstance> LoadWithRetry(
            Func<AsyncOperationHandle<SceneInstance>> createHandle,
            SceneLoadInfo info,
            CancellationToken cancellationToken)
        {
            var attempts = Math.Max(0, _options.retryCount) + 1;
            Exception lastException = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    var handle = createHandle();
                    return await LoadWithEvents(handle, info, attempt, attempts, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    ReleaseFailedSceneHandle(info);
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    ReleaseFailedSceneHandle(info);
                    if (attempt >= attempts)
                    {
                        break;
                    }

                    EvoDebug.LogWarning(
                        $"[SceneLoad] Attempt {attempt}/{attempts} failed for key '{info.SceneKey}'. Retrying. {ex.GetType().Name}: {ex.Message}",
                        nameof(SceneLoaderService));

                    if (_options.retryDelaySeconds > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(_options.retryDelaySeconds),
                            cancellationToken: cancellationToken);
                    }
                }
            }

            throw lastException ?? new InvalidOperationException($"Scene load failed for key '{info.SceneKey}'.");
        }

        private void ReleaseFailedSceneHandle(SceneLoadInfo info)
        {
            try
            {
                if (info.Source == SceneLoadSource.Reference && _lastSceneReference != null)
                {
                    _resourceLoader.ReleaseSceneHandle(_lastSceneReference);
                }
                else
                {
                    _resourceLoader.ReleaseSceneHandle(info.SceneKey);
                }
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Failed to release scene handle for '{info.SceneKey}' before retry. {ex.Message}",
                    nameof(SceneLoaderService));
            }
        }

        private async UniTask<SceneInstance> LoadWithEvents(
            AsyncOperationHandle<SceneInstance> handle,
            SceneLoadInfo info,
            int attempt,
            int attempts,
            CancellationToken cancellationToken)
        {
            var operationId = Interlocked.Increment(ref _loadOperationSequence);
            var startedAtUtc = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            LogVerbose(
                $"[SceneLoad:{operationId}] START ts={startedAtUtc:O} key='{info.SceneKey}' mode={info.Mode} " +
                $"activateOnLoad={info.ActivateOnLoad} priority={info.Priority} source={info.Source} " +
                $"isReload={info.IsReload} attempt={attempt}/{attempts} handleValid={handle.IsValid()} " +
                $"status={(handle.IsValid() ? handle.Status.ToString() : "Invalid")}");

            SceneLoadStarted?.Invoke(info);
            ReportProgress(handle, info, cancellationToken).Forget();

            try
            {
                await AwaitHandleCompletionAsync(handle, info, operationId, cancellationToken);

                var loadDoneAtUtc = DateTime.UtcNow;
                var loadedScene = handle.Result.Scene;
                LogVerbose(
                    $"[SceneLoad:{operationId}] LOAD_HANDLE_DONE ts={loadDoneAtUtc:O} elapsedMs={stopwatch.ElapsedMilliseconds} " +
                    $"key='{info.SceneKey}' status={handle.Status} percent={handle.PercentComplete:0.000} " +
                    $"sceneValid={loadedScene.IsValid()} sceneLoaded={loadedScene.isLoaded} sceneName='{loadedScene.name}'");

                LogVerbose(
                    info.ActivateOnLoad
                        ? $"[SceneLoad:{operationId}] ACTIVATION_MODE auto-by-addressables"
                        : $"[SceneLoad:{operationId}] ACTIVATION_MODE deferred-manual");

                SceneLoadProgress?.Invoke(new SceneLoadProgress(info, 1f));
                SceneLoadFinished?.Invoke(info);
                return handle.Result;
            }
            catch (OperationCanceledException)
            {
                EvoDebug.LogWarning(
                    $"[SceneLoad:{operationId}] CANCELED ts={DateTime.UtcNow:O} elapsedMs={stopwatch.ElapsedMilliseconds} " +
                    $"key='{info.SceneKey}' percent={(handle.IsValid() ? handle.PercentComplete : 0f):0.000}",
                    nameof(SceneLoaderService));
                SceneLoadFinished?.Invoke(info);
                throw;
            }
            catch (TimeoutException ex)
            {
                EvoDebug.LogError(
                    $"[SceneLoad:{operationId}] TIMEOUT ts={DateTime.UtcNow:O} elapsedMs={stopwatch.ElapsedMilliseconds} " +
                    $"key='{info.SceneKey}' percent={(handle.IsValid() ? handle.PercentComplete : 0f):0.000}. {ex.Message}",
                    nameof(SceneLoaderService));
                SceneLoadFinished?.Invoke(info);
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogError(
                    $"[SceneLoad:{operationId}] FAILED ts={DateTime.UtcNow:O} elapsedMs={stopwatch.ElapsedMilliseconds} " +
                    $"key='{info.SceneKey}' percent={(handle.IsValid() ? handle.PercentComplete : 0f):0.000}. " +
                    $"{ex.GetType().Name}: {ex.Message}",
                    nameof(SceneLoaderService));
                SceneLoadFinished?.Invoke(info);
                throw;
            }
        }

        private async UniTask AwaitHandleCompletionAsync(
            AsyncOperationHandle<SceneInstance> handle,
            SceneLoadInfo info,
            long operationId,
            CancellationToken cancellationToken)
        {
            var startedAtUtc = DateTime.UtcNow;
            var activeElapsedSeconds = 0f;
            var finalizationElapsedSeconds = 0f;
            var lastTickUtc = DateTime.UtcNow;
            var finalizationWarningLogged = false;
            while (!handle.IsDone)
            {
                var nowUtc = DateTime.UtcNow;
                var deltaSeconds = Math.Max(0f, (float)(nowUtc - lastTickUtc).TotalSeconds);
                lastTickUtc = nowUtc;

                var countTimeout = ShouldCountTimeoutSeconds();
                if (countTimeout)
                {
                    activeElapsedSeconds += deltaSeconds;
                    if (IsWaitingForFinalization(handle))
                    {
                        finalizationElapsedSeconds += deltaSeconds;
                        if (!finalizationWarningLogged)
                        {
                            finalizationWarningLogged = true;
                            LogVerbose(
                                $"[SceneLoad:{operationId}] Scene handle reached progress={handle.PercentComplete:0.000} " +
                                $"for key '{info.SceneKey}' but is not done yet. Waiting for Addressables/Unity finalization.");
                        }
                    }
                }

                if (ShouldTimeout(handle, activeElapsedSeconds, finalizationElapsedSeconds))
                {
                    throw new TimeoutException(
                        $"Scene handle wait exceeded active timeout for key '{info.SceneKey}' " +
                        $"(mode={info.Mode}, activateOnLoad={info.ActivateOnLoad}, status={GetHandleStatus(handle)}, " +
                        $"percent={GetHandleProgress(handle):0.000}, activeElapsed={activeElapsedSeconds:0.###}, " +
                        $"finalizationElapsed={finalizationElapsedSeconds:0.###}, startedAt={startedAtUtc:O}, " +
                        $"operationId={operationId}).");
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                throw handle.OperationException ?? new InvalidOperationException(
                    "Scene load operation failed without explicit exception.");
            }
        }

        private bool ShouldCountTimeoutSeconds()
        {
            if (!_options.enableTimeout)
            {
                return false;
            }

            return !_options.ignoreTimeoutWhenApplicationNotFocused ||
                   _isApplicationFocused ||
                   UnityEngine.Application.runInBackground;
        }

        private bool IsWaitingForFinalization(AsyncOperationHandle<SceneInstance> handle)
        {
            return handle.IsValid() &&
                   handle.Status == AsyncOperationStatus.None &&
                   handle.PercentComplete >= _options.completedProgressThreshold;
        }

        private static string GetHandleStatus(AsyncOperationHandle<SceneInstance> handle)
        {
            return handle.IsValid() ? handle.Status.ToString() : "Invalid";
        }

        private static float GetHandleProgress(AsyncOperationHandle<SceneInstance> handle)
        {
            return handle.IsValid() ? handle.PercentComplete : 0f;
        }

        private bool ShouldTimeout(
            AsyncOperationHandle<SceneInstance> handle,
            float activeElapsedSeconds,
            float finalizationElapsedSeconds)
        {
            if (!_options.enableTimeout)
            {
                return false;
            }

            if (IsWaitingForFinalization(handle))
            {
                return _options.finalizationTimeoutSeconds > 0f &&
                       finalizationElapsedSeconds >= _options.finalizationTimeoutSeconds;
            }

            return _options.timeoutSeconds > 0f && activeElapsedSeconds >= _options.timeoutSeconds;
        }

        private async UniTask LoadViaSceneManager(string sceneName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                EvoDebug.LogWarning("LoadViaSceneManager called with empty name.", nameof(SceneLoaderService));
                return;
            }

            var info = new SceneLoadInfo(sceneName, LoadSceneMode.Single, true, 100, SceneLoadSource.SceneManager, true);
            SceneLoadStarted?.Invoke(info);

            var reloadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (reloadOperation == null)
            {
                EvoDebug.LogWarning("ReloadActiveAsync could not start reload operation.", nameof(SceneLoaderService));
                SceneLoadFinished?.Invoke(info);
                return;
            }

            while (!reloadOperation.isDone)
            {
                SceneLoadProgress?.Invoke(new SceneLoadProgress(info, reloadOperation.progress));
                try
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    EvoDebug.LogWarning(
                        $"ReloadActiveAsync canceled for scene '{sceneName}'.",
                        nameof(SceneLoaderService));
                    SceneLoadFinished?.Invoke(info);
                    return;
                }
            }

            SceneLoadProgress?.Invoke(new SceneLoadProgress(info, 1f));
            SceneLoadFinished?.Invoke(info);
        }

        private async UniTaskVoid ReportProgress(
            AsyncOperationHandle<SceneInstance> handle,
            SceneLoadInfo info,
            CancellationToken cancellationToken)
        {
            try
            {
                while (handle.IsValid() && !handle.IsDone)
                {
                    SceneLoadProgress?.Invoke(new SceneLoadProgress(info, handle.PercentComplete));
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation: progress reporter is fire-and-forget helper.
            }
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

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            CurrentSceneName = nextScene.name;
            ActiveSceneChanged?.Invoke(CurrentSceneName);
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            _isApplicationFocused = hasFocus;
        }

        private void LogVerbose(string message)
        {
            if (_options.verboseLogging)
            {
                EvoDebug.Log(message, nameof(SceneLoaderService));
            }
        }

        private static SceneLoaderOptions NormalizeOptions(SceneLoaderOptions options)
        {
            options ??= new SceneLoaderOptions();
            options.timeoutSeconds = Math.Max(0f, options.timeoutSeconds);
            options.finalizationTimeoutSeconds = Math.Max(options.timeoutSeconds, options.finalizationTimeoutSeconds);
            options.completedProgressThreshold = Math.Max(0.5f, Math.Min(1f, options.completedProgressThreshold));
            options.retryCount = Math.Max(0, options.retryCount);
            options.retryDelaySeconds = Math.Max(0f, options.retryDelaySeconds);
            return options;
        }
    }
}
