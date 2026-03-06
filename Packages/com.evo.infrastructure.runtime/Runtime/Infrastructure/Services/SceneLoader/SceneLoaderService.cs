using System;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.Debug;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.ResourceLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace _Project.Scripts.Infrastructure.Services.SceneLoader
{
    public sealed class SceneLoaderService : ISceneLoaderService
    {
        private readonly IResourceLoaderService _resourceLoader;
        private string _lastSceneKey;
        private AssetReference _lastSceneReference;

        public event Action<SceneLoadInfo> SceneLoadStarted;
        public event Action<SceneLoadProgress> SceneLoadProgress;
        public event Action<SceneLoadInfo> SceneLoadFinished;
        public event Action<string> ActiveSceneChanged;
        public string CurrentSceneName { get; private set; }

        public SceneLoaderService(IResourceLoaderService resourceLoader)
        {
            _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
            CurrentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
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
            var handle = _resourceLoader.LoadSceneHandle(
                key,
                mode,
                activateOnLoad,
                priority,
                forceReload);
            return LoadWithEvents(handle, info, cancellationToken);
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
            var handle = _resourceLoader.LoadSceneHandle(
                reference,
                mode,
                activateOnLoad,
                priority,
                forceReload);
            return LoadWithEvents(handle, info, cancellationToken);
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
            EvoDebug.Log(
                $"ReloadActiveAsync start. Active='{activeScene.name}', BuildIndex={activeScene.buildIndex}, " +
                $"LastKey='{_lastSceneKey}', LastRefNull={_lastSceneReference == null}",
                nameof(SceneLoaderService));

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
                        var handle = _resourceLoader.LoadSceneHandle(
                            _lastSceneReference,
                            LoadSceneMode.Single,
                            true,
                            100,
                            true);
                        await LoadWithEvents(handle, info, cancellationToken);
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
                    var handle = _resourceLoader.LoadSceneHandle(
                        _lastSceneKey,
                        LoadSceneMode.Single,
                        true,
                        100,
                        true);
                    await LoadWithEvents(handle, info, cancellationToken);
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

        private async UniTask<SceneInstance> LoadWithEvents(
            AsyncOperationHandle<SceneInstance> handle,
            SceneLoadInfo info,
            CancellationToken cancellationToken)
        {
            SceneLoadStarted?.Invoke(info);
            ReportProgress(handle, info, cancellationToken).Forget();

            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
                SceneLoadProgress?.Invoke(new SceneLoadProgress(info, 1f));
                SceneLoadFinished?.Invoke(info);
                return handle.Result;
            }
            catch (Exception ex)
            {
                EvoDebug.LogError(
                    $"Scene load failed for '{info.SceneKey}'. {ex.Message}",
                    nameof(SceneLoaderService));
                SceneLoadFinished?.Invoke(info);
                throw;
            }
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
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            SceneLoadProgress?.Invoke(new SceneLoadProgress(info, 1f));
            SceneLoadFinished?.Invoke(info);
        }

        private async UniTaskVoid ReportProgress(
            AsyncOperationHandle<SceneInstance> handle,
            SceneLoadInfo info,
            CancellationToken cancellationToken)
        {
            while (handle.IsValid() && !handle.IsDone)
            {
                SceneLoadProgress?.Invoke(new SceneLoadProgress(info, handle.PercentComplete));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
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
    }
}
