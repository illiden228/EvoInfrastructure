using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Infrastructure.Services.ResourceLoader
{
    public interface IResourceLoaderService
    {
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        UniTask<T> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        UniTask<T> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : class;
        UniTask<SceneInstance> LoadSceneAsync(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default);
        AsyncOperationHandle<SceneInstance> LoadSceneHandle(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false);
        UniTask<SceneInstance> LoadSceneAsync(
            AssetReference reference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default);
        AsyncOperationHandle<SceneInstance> LoadSceneHandle(
            AssetReference reference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false);
        UniTask<IReadOnlyList<T>> LoadAllAsync<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
            where T : class;
        UniTask<T> GetOrLoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        UniTask<T> GetOrLoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : class;
        bool TryGetCached<T>(string key, out T asset) where T : class;
        bool TryGetCached<T>(AssetReference reference, out T asset) where T : class;
        void Release<T>(string key) where T : class;
        void Release<T>(AssetReference reference) where T : class;
        UniTask UnloadSceneAsync(string key, CancellationToken cancellationToken = default);
        UniTask UnloadSceneAsync(AssetReference reference, CancellationToken cancellationToken = default);
        void ReleaseAll();
    }
}
