using System;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.Debug;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.ResourceCatalog;
using _Project.Scripts.Infrastructure.Services.ResourceLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace _Project.Scripts.Infrastructure.Services.ResourceProvider
{
    public sealed class ResourceProviderService : IResourceProviderService
    {
        private readonly IResourceLoaderService _resourceLoader;
        private readonly IResourceCatalog _catalog;

        public ResourceProviderService(IResourceLoaderService resourceLoader, IResourceCatalog catalog = null)
        {
            _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
            _catalog = catalog;
        }

        public UniTask<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var resolvedKey = ResolveAssetKey(key, typeof(T));
                return _resourceLoader.LoadAsync<T>(resolvedKey, cancellationToken);
            }
            catch (Exception ex)
            {
                EvoDebug.LogError($"LoadAssetAsync failed for '{key}'. {ex.Message}", nameof(ResourceProviderService));
                throw;
            }
        }

        public UniTask<T> LoadAssetAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                return _resourceLoader.LoadAsync<T>(reference, cancellationToken);
            }
            catch (Exception ex)
            {
                EvoDebug.LogError(
                    $"LoadAssetAsync failed for reference '{reference}'. {ex.Message}",
                    nameof(ResourceProviderService));
                throw;
            }
        }

        public async UniTask<Sprite> GetSpriteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                EvoDebug.LogError("GetSpriteAsync called with empty key.", nameof(ResourceProviderService));
                throw new ArgumentException("Key is null or empty.", nameof(key));
            }

            try
            {
                if (_catalog != null && _catalog.TryGetEntry(key, ResourceType.Sprite, out var entry))
                {
                    if (entry.SpriteType == SpriteEntryType.Atlas)
                    {
                        var atlasKey = string.IsNullOrEmpty(entry.AtlasKey) ? entry.Key : entry.AtlasKey;
                        var atlas = await _resourceLoader.LoadAsync<SpriteAtlas>(atlasKey, cancellationToken);
                        var sprite = atlas.GetSprite(entry.SpriteName);
                        if (sprite == null)
                        {
                            throw new InvalidOperationException(
                                $"Sprite '{entry.SpriteName}' not found in atlas '{atlasKey}'.");
                        }

                        return sprite;
                    }

                    var spriteKey = string.IsNullOrEmpty(entry.AssetKey) ? entry.Key : entry.AssetKey;
                    return await _resourceLoader.LoadAsync<Sprite>(spriteKey, cancellationToken);
                }

                return await _resourceLoader.LoadAsync<Sprite>(key, cancellationToken);
            }
            catch (Exception ex)
            {
                EvoDebug.LogError($"GetSpriteAsync failed for '{key}'. {ex.Message}", nameof(ResourceProviderService));
                throw;
            }
        }

        public async UniTask<AudioClip> GetAudioAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                EvoDebug.LogError("GetAudioAsync called with empty key.", nameof(ResourceProviderService));
                throw new ArgumentException("Key is null or empty.", nameof(key));
            }

            try
            {
                if (_catalog != null && _catalog.TryGetEntry(key, ResourceType.AudioClip, out var entry))
                {
                    var audioKey = string.IsNullOrEmpty(entry.AssetKey) ? entry.Key : entry.AssetKey;
                    return await _resourceLoader.LoadAsync<AudioClip>(audioKey, cancellationToken);
                }

                return await _resourceLoader.LoadAsync<AudioClip>(key, cancellationToken);
            }
            catch (Exception ex)
            {
                EvoDebug.LogError($"GetAudioAsync failed for '{key}'. {ex.Message}", nameof(ResourceProviderService));
                throw;
            }
        }

        public async UniTask<GameObject> InstantiateAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var resolvedKey = ResolveAssetKey(key, typeof(GameObject));
                var prefab = await _resourceLoader.LoadAsync<GameObject>(resolvedKey, cancellationToken);
                return UnityEngine.Object.Instantiate(prefab);
            }
            catch (OperationCanceledException)
            {
                EvoDebug.LogWarning($"InstantiateAsync canceled for '{key}'.", nameof(ResourceProviderService));
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogError($"InstantiateAsync failed for '{key}'. {ex.Message}", nameof(ResourceProviderService));
                throw;
            }
        }

        public async UniTask<GameObject> InstantiateAsync(AssetReference reference, CancellationToken cancellationToken = default)
        {
            try
            {
                var prefab = await _resourceLoader.LoadAsync<GameObject>(reference, cancellationToken);
                return UnityEngine.Object.Instantiate(prefab);
            }
            catch (OperationCanceledException)
            {
                EvoDebug.LogWarning(
                    $"InstantiateAsync canceled for reference '{reference}'.",
                    nameof(ResourceProviderService));
                throw;
            }
            catch (Exception ex)
            {
                EvoDebug.LogError(
                    $"InstantiateAsync failed for reference '{reference}'. {ex.Message}",
                    nameof(ResourceProviderService));
                throw;
            }
        }

        public void ReleaseAsset<T>(string key) where T : class
        {
            var resolvedKey = ResolveAssetKey(key, typeof(T));
            _resourceLoader.Release<T>(resolvedKey);
        }

        public void ReleaseAsset<T>(AssetReference reference) where T : class
        {
            _resourceLoader.Release<T>(reference);
        }

        public void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }

        private string ResolveAssetKey(string key, Type type)
        {
            if (string.IsNullOrEmpty(key))
            {
                EvoDebug.LogError("ResolveAssetKey called with empty key.", nameof(ResourceProviderService));
                throw new ArgumentException("Key is null or empty.", nameof(key));
            }

            if (_catalog == null)
            {
                return key;
            }

            if (type == typeof(GameObject))
            {
                if (_catalog.TryGetEntry(key, ResourceType.GameObject, out var entry))
                {
                    return string.IsNullOrEmpty(entry.AssetKey) ? entry.Key : entry.AssetKey;
                }

                return key;
            }

            if (_catalog.TryGetEntry(key, out var anyEntry) && !string.IsNullOrEmpty(anyEntry.AssetKey))
            {
                return anyEntry.AssetKey;
            }

            return key;
        }
    }
}
