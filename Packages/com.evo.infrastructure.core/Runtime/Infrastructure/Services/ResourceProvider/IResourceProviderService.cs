using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace _Project.Scripts.Infrastructure.Services.ResourceProvider
{
    public interface IResourceProviderService
    {
        UniTask<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        UniTask<T> LoadAssetAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : class;
        UniTask<Sprite> GetSpriteAsync(string key, CancellationToken cancellationToken = default);
        UniTask<AudioClip> GetAudioAsync(string key, CancellationToken cancellationToken = default);
        UniTask<GameObject> InstantiateAsync(string key, CancellationToken cancellationToken = default);
        UniTask<GameObject> InstantiateAsync(AssetReference reference, CancellationToken cancellationToken = default);
        void ReleaseAsset<T>(string key) where T : class;
        void ReleaseAsset<T>(AssetReference reference) where T : class;
        void DestroyInstance(GameObject instance);
    }
}
