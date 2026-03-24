using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.Debug;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Infrastructure.Services.ResourceLoader
{
    public sealed class AddressablesResourceLoaderService : IResourceLoaderService, IDisposable
    {
        private readonly Dictionary<CacheKey, AsyncOperationHandle> _handles = new();
        private bool _initialized;
        private bool _initInProgress;
        private AsyncOperationHandle _initHandle;

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            if (_initInProgress && _initHandle.IsValid() && !_initHandle.IsDone)
            {
                await _initHandle.ToUniTask(cancellationToken: cancellationToken);
                _initialized = true;
                return;
            }

            var handle = Addressables.InitializeAsync();
            _initInProgress = true;
            _initHandle = handle;

            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
                _initialized = true;
            }
            catch (Exception ex)
            {
                EvoDebug.LogError($"Addressables initialization failed. {ex.Message}", nameof(AddressablesResourceLoaderService));
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }
            finally
            {
                _initInProgress = false;
            }
        }

        public UniTask<T> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                EvoDebug.LogError("LoadAsync called with empty key.", nameof(AddressablesResourceLoaderService));
                throw new ArgumentException("Key is null or empty.", nameof(key));
            }

            var cacheKey = new CacheKey(key, typeof(T));
            if (_handles.TryGetValue(cacheKey, out var existing) && existing.IsValid())
            {
                if (existing.Status == AsyncOperationStatus.Succeeded)
                {
                    return UniTask.FromResult((T)existing.Result);
                }

                return AwaitHandle<T>(existing, cacheKey, cancellationToken);
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            _handles[cacheKey] = handle;
            return AwaitHandle<T>(handle, cacheKey, cancellationToken);
        }

        public UniTask<T> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : class
        {
            var key = GetReferenceKey(reference);
            var cacheKey = new CacheKey(key, typeof(T));
            if (_handles.TryGetValue(cacheKey, out var existing) && existing.IsValid())
            {
                if (existing.Status == AsyncOperationStatus.Succeeded)
                {
                    var cachedHandle = existing.Convert<T>();
                    return UniTask.FromResult(cachedHandle.Result);
                }

                return AwaitHandle<T>(existing, cacheKey, cancellationToken);
            }

            var handle = reference.LoadAssetAsync<T>();
            _handles[cacheKey] = handle;
            return AwaitHandle<T>(handle, cacheKey, cancellationToken);
        }

        public UniTask<SceneInstance> LoadSceneAsync(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = new CacheKey(key, typeof(SceneInstance));
            var handle = LoadSceneHandle(key, mode, activateOnLoad, priority, forceReload);
            return AwaitHandle<SceneInstance>(handle, cacheKey, cancellationToken);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneHandle(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                EvoDebug.LogError("LoadSceneHandle called with empty key.", nameof(AddressablesResourceLoaderService));
                throw new ArgumentException("Key is null or empty.", nameof(key));
            }

            var cacheKey = new CacheKey(key, typeof(SceneInstance));
            if (!forceReload && _handles.TryGetValue(cacheKey, out var existing) && existing.IsValid())
            {
                return existing.Convert<SceneInstance>();
            }

            AsyncOperationHandle oldHandle = default;
            var hasOldHandle = false;
            if (forceReload && _handles.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                oldHandle = cached;
                hasOldHandle = true;
            }

            var handle = Addressables.LoadSceneAsync(key, mode, activateOnLoad, priority);
            _handles[cacheKey] = handle;
            RegisterSceneHandleRelease(handle, cacheKey, oldHandle, hasOldHandle);
            return handle;
        }

        public UniTask<SceneInstance> LoadSceneAsync(
            AssetReference reference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default)
        {
            var key = GetReferenceKey(reference);
            var cacheKey = new CacheKey(key, typeof(SceneInstance));
            var handle = LoadSceneHandle(reference, mode, activateOnLoad, priority, forceReload);
            return AwaitHandle<SceneInstance>(handle, cacheKey, cancellationToken);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneHandle(
            AssetReference reference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false)
        {
            var key = GetReferenceKey(reference);
            var cacheKey = new CacheKey(key, typeof(SceneInstance));
            if (!forceReload && _handles.TryGetValue(cacheKey, out var existing) && existing.IsValid())
            {
                return existing.Convert<SceneInstance>();
            }

            AsyncOperationHandle oldHandle = default;
            var hasOldHandle = false;
            if (forceReload && _handles.TryGetValue(cacheKey, out var cached) && cached.IsValid())
            {
                oldHandle = cached;
                hasOldHandle = true;
            }

            var handle = Addressables.LoadSceneAsync(key, mode, activateOnLoad, priority);
            _handles[cacheKey] = handle;
            RegisterSceneHandleRelease(handle, cacheKey, oldHandle, hasOldHandle);
            return handle;
        }

        public UniTask<T> GetOrLoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (TryGetCached<T>(key, out var cached))
            {
                return UniTask.FromResult(cached);
            }

            return LoadAsync<T>(key, cancellationToken);
        }

        public UniTask<T> GetOrLoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : class
        {
            if (TryGetCached<T>(reference, out var cached))
            {
                return UniTask.FromResult(cached);
            }

            return LoadAsync<T>(reference, cancellationToken);
        }

        public async UniTask<IReadOnlyList<T>> LoadAllAsync<T>(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (keys == null)
            {
                EvoDebug.LogError("LoadAllAsync called with null keys.", nameof(AddressablesResourceLoaderService));
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Count == 0)
            {
                return Array.Empty<T>();
            }

            var tasks = new UniTask<T>[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                tasks[i] = LoadAsync<T>(keys[i], cancellationToken);
            }

            return await UniTask.WhenAll(tasks);
        }

        public bool TryGetCached<T>(string key, out T asset) where T : class
        {
            asset = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            var cacheKey = new CacheKey(key, typeof(T));
            if (_handles.TryGetValue(cacheKey, out var handle) &&
                handle.IsValid() &&
                handle.Status == AsyncOperationStatus.Succeeded)
            {
                asset = (T)handle.Result;
                return true;
            }

            return false;
        }

        public bool TryGetCached<T>(AssetReference reference, out T asset) where T : class
        {
            asset = null;
            if (reference == null)
            {
                return false;
            }

            var key = GetReferenceKey(reference);
            var cacheKey = new CacheKey(key, typeof(T));
            if (_handles.TryGetValue(cacheKey, out var handle) &&
                handle.IsValid() &&
                handle.Status == AsyncOperationStatus.Succeeded)
            {
                asset = (T)handle.Result;
                return true;
            }

            return false;
        }

        public void Release<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var cacheKey = new CacheKey(key, typeof(T));
            if (_handles.TryGetValue(cacheKey, out var handle))
            {
                _handles.Remove(cacheKey);
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        public void Release<T>(AssetReference reference) where T : class
        {
            if (reference == null)
            {
                return;
            }

            var key = GetReferenceKey(reference);
            Release<T>(key);
        }

        public async UniTask UnloadSceneAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (UnityEngine.SceneManagement.SceneManager.sceneCount <= 1)
            {
                return;
            }

            var cacheKey = new CacheKey(key, typeof(SceneInstance));
            if (_handles.TryGetValue(cacheKey, out var handle))
            {
                if (handle.IsValid())
                {
                    var typedHandle = handle.Convert<SceneInstance>();
                    if (typedHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loadedScene = typedHandle.Result.Scene;
                        if (loadedScene.IsValid() && SceneManager.sceneCount <= 1)
                        {
                            return;
                        }
                    }

                    _handles.Remove(cacheKey);
                    await Addressables.UnloadSceneAsync(typedHandle, true)
                        .ToUniTask(cancellationToken: cancellationToken);
                }
                else
                {
                    _handles.Remove(cacheKey);
                }
            }
        }

        public UniTask UnloadSceneAsync(AssetReference reference, CancellationToken cancellationToken = default)
        {
            if (reference == null)
            {
                return UniTask.CompletedTask;
            }

            var key = GetReferenceKey(reference);
            return UnloadSceneAsync(key, cancellationToken);
        }

        public void ReleaseAll()
        {
            foreach (var pair in _handles)
            {
                var handle = pair.Value;
                if (!handle.IsValid())
                {
                    continue;
                }

                if (pair.Key.Type == typeof(SceneInstance))
                {
                    // Scene handles must be unloaded explicitly; skip to avoid unloading last scene.
                    continue;
                }

                Addressables.Release(handle);
            }

            _handles.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }

        private async UniTask<T> AwaitHandle<T>(
            AsyncOperationHandle handle,
            CacheKey cacheKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var typedHandle = handle.Convert<T>();
                await typedHandle.ToUniTask(cancellationToken: cancellationToken);
                return typedHandle.Result;
            }
            catch (OperationCanceledException)
            {
                if (_handles.TryGetValue(cacheKey, out var existing) && existing.Equals(handle))
                {
                    _handles.Remove(cacheKey);
                }

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                EvoDebug.LogWarning(
                    $"Load canceled for '{cacheKey.Key}' ({cacheKey.Type?.Name}).",
                    nameof(AddressablesResourceLoaderService));
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogError(
                    $"Failed to load '{cacheKey.Key}' ({cacheKey.Type?.Name}). {ex.Message}",
                    nameof(AddressablesResourceLoaderService));
                if (_handles.TryGetValue(cacheKey, out var existing) && existing.Equals(handle))
                {
                    _handles.Remove(cacheKey);
                }

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }
        }

        private async UniTask<T> AwaitHandleWithRelease<T>(
            AsyncOperationHandle handle,
            CacheKey cacheKey,
            AsyncOperationHandle oldHandle,
            bool hasOldHandle,
            CancellationToken cancellationToken)
        {
            var result = await AwaitHandle<T>(handle, cacheKey, cancellationToken);
            if (hasOldHandle && oldHandle.IsValid())
            {
                Addressables.Release(oldHandle);
            }

            return result;
        }

        private void RegisterSceneHandleRelease(
            AsyncOperationHandle handle,
            CacheKey cacheKey,
            AsyncOperationHandle oldHandle,
            bool hasOldHandle)
        {
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    if (hasOldHandle && oldHandle.IsValid())
                    {
                        Addressables.Release(oldHandle);
                    }

                    return;
                }

                if (op.OperationException is OperationCanceledException)
                {
                    EvoDebug.LogWarning(
                        $"Load canceled for '{cacheKey.Key}' ({cacheKey.Type?.Name}).",
                        nameof(AddressablesResourceLoaderService));
                }
                else
                {
                    EvoDebug.LogError(
                        $"Failed to load '{cacheKey.Key}' ({cacheKey.Type?.Name}). {op.OperationException?.Message}",
                        nameof(AddressablesResourceLoaderService));
                }
                if (_handles.TryGetValue(cacheKey, out var existing) && existing.Equals(op))
                {
                    _handles.Remove(cacheKey);
                }

                if (op.IsValid())
                {
                    Addressables.Release(op);
                }
            };
        }

        private static string GetReferenceKey(AssetReference reference)
        {
            if (reference == null)
            {
                EvoDebug.LogError("GetReferenceKey called with null reference.", nameof(AddressablesResourceLoaderService));
                throw new ArgumentNullException(nameof(reference));
            }

            if (!string.IsNullOrEmpty(reference.AssetGUID))
            {
                return reference.AssetGUID;
            }

            var runtimeKey = reference.RuntimeKey;
            if (runtimeKey == null)
            {
                EvoDebug.LogError("GetReferenceKey found null RuntimeKey.", nameof(AddressablesResourceLoaderService));
                throw new InvalidOperationException("AssetReference has no RuntimeKey.");
            }

            return runtimeKey.ToString();
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly string _key;
            private readonly Type _type;

            public CacheKey(string key, Type type)
            {
                _key = key;
                _type = type;
            }

            public Type Type => _type;
            public string Key => _key;

            public bool Equals(CacheKey other) => _key == other._key && _type == other._type;

            public override bool Equals(object obj) => obj is CacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_key != null ? _key.GetHashCode() : 0) * 397) ^ (_type != null ? _type.GetHashCode() : 0);
                }
            }
        }
    }
}
