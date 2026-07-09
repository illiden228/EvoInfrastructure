using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Pooling
{
    public sealed class KeyedGameObjectPool<TKey> : IDisposable
    {
        private readonly Func<TKey, GameObject> _resolvePrefab;
        private readonly Func<TKey, int> _resolvePrewarmCount;
        private readonly Func<TKey, int> _resolveMaxInactive;
        private readonly Transform _root;
        private readonly Dictionary<TKey, GameObjectPool> _pools;
        private readonly Dictionary<GameObject, TKey> _activeKeysByInstance = new();
        private bool _disposed;

        public KeyedGameObjectPool(
            Func<TKey, GameObject> resolvePrefab,
            Func<TKey, int> resolvePrewarmCount = null,
            Func<TKey, int> resolveMaxInactive = null,
            Transform root = null,
            IEqualityComparer<TKey> comparer = null)
        {
            _resolvePrefab = resolvePrefab ?? throw new ArgumentNullException(nameof(resolvePrefab));
            _resolvePrewarmCount = resolvePrewarmCount;
            _resolveMaxInactive = resolveMaxInactive;
            _root = root;
            _pools = new Dictionary<TKey, GameObjectPool>(comparer);
        }

        public GameObject Get(TKey key)
        {
            ThrowIfDisposed();
            var instance = GetOrCreatePool(key).Get();
            TrackActive(key, instance);
            return instance;
        }

        public GameObject Get(TKey key, Vector3 position, Quaternion rotation)
        {
            ThrowIfDisposed();
            var instance = GetOrCreatePool(key).Get(position, rotation);
            TrackActive(key, instance);
            return instance;
        }

        public bool Release(TKey key, GameObject instance)
        {
            if (_disposed || !_pools.TryGetValue(key, out var pool))
            {
                return false;
            }

            _activeKeysByInstance.Remove(instance);
            return pool.Release(instance);
        }

        public bool Release(GameObject instance)
        {
            if (_disposed || instance == null || !_activeKeysByInstance.TryGetValue(instance, out var key))
            {
                return false;
            }

            return Release(key, instance);
        }

        public bool TryGetPool(TKey key, out GameObjectPool pool)
        {
            return _pools.TryGetValue(key, out pool);
        }

        public GameObjectPool GetOrCreatePool(TKey key)
        {
            ThrowIfDisposed();
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool;
            }

            var prefab = _resolvePrefab(key);
            var prewarmCount = Math.Max(0, _resolvePrewarmCount?.Invoke(key) ?? 0);
            var maxInactive = Math.Max(0, _resolveMaxInactive?.Invoke(key) ?? 0);
            pool = new GameObjectPool(prefab, prewarmCount, maxInactive, _root);
            _pools.Add(key, pool);
            return pool;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _activeKeysByInstance.Clear();
            _pools.Clear();
        }

        private void TrackActive(TKey key, GameObject instance)
        {
            if (instance != null)
            {
                _activeKeysByInstance[instance] = key;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KeyedGameObjectPool<TKey>));
            }
        }
    }
}
