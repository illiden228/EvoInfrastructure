using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Infrastructure.Services.SceneLoader
{
    public interface ISceneLoaderService
    {
        event Action<SceneLoadInfo> SceneLoadStarted;
        event Action<SceneLoadProgress> SceneLoadProgress;
        event Action<SceneLoadInfo> SceneLoadFinished;
        event Action<string> ActiveSceneChanged;
        string CurrentSceneName { get; }
        UniTask<SceneInstance> LoadAsync(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default);
        UniTask<SceneInstance> LoadAsync(
            AssetReference reference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            bool forceReload = false,
            CancellationToken cancellationToken = default);

        UniTask UnloadAsync(string key, CancellationToken cancellationToken = default);
        UniTask UnloadByNameAsync(string sceneName, CancellationToken cancellationToken = default);
        bool IsLoaded(string sceneName);
        UniTask ReloadActiveAsync(CancellationToken cancellationToken = default);
    }
}
