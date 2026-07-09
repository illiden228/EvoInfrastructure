using System;
using UnityEngine;

namespace Evo.Infrastructure.Services.Pooling
{
    public sealed class ComponentPool<T> : IDisposable where T : Component
    {
        private readonly Transform _root;
        private readonly T _prefab;
        private readonly PoolableCache _poolableCache = new();
        private readonly Pool<T> _pool;

        public ComponentPool(T prefab, int prewarmCount, int maxSize, Transform root = null)
        {
            _prefab = prefab;
            _root = root;
            _pool = _prefab == null
                ? null
                : new Pool<T>(
                    CreateInstance,
                    null,
                    OnRelease,
                    OnDestroy,
                    prewarmCount,
                    maxSize);
        }

        public int InactiveCount => _pool?.InactiveCount ?? 0;
        public int ActiveCount => _pool?.ActiveCount ?? 0;
        public PoolStatistics Statistics => _pool?.Statistics ?? default;

        public T Get()
        {
            var instance = _pool?.Get();
            PrepareForGet(instance, false, default, default);
            return instance;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var instance = _pool?.Get();
            PrepareForGet(instance, true, position, rotation);
            return instance;
        }

        public bool Release(T instance)
        {
            return _pool?.Release(instance) == true;
        }

        public void ClearInactive()
        {
            _pool?.ClearInactive();
        }

        public void Dispose()
        {
            _pool?.Dispose();
            _poolableCache.Clear();
        }

        private T CreateInstance()
        {
            return UnityEngine.Object.Instantiate(_prefab, _root);
        }

        private void PrepareForGet(T instance, bool hasPose, Vector3 position, Quaternion rotation)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.SetParent(null, true);
            if (hasPose)
            {
                instance.transform.SetPositionAndRotation(position, rotation);
            }

            instance.gameObject.SetActive(true);
            InvokePoolGet(instance.gameObject);
        }

        private void OnRelease(T instance)
        {
            if (instance == null)
            {
                return;
            }

            InvokePoolRelease(instance.gameObject);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_root, true);
        }

        private void OnDestroy(T instance)
        {
            if (instance == null)
            {
                return;
            }

            InvokePoolDestroy(instance.gameObject);
            _poolableCache.Remove(instance.gameObject);
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        private void InvokePoolGet(GameObject instance)
        {
            var poolables = _poolableCache.Get(instance);
            for (var i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnPoolGet();
            }
        }

        private void InvokePoolRelease(GameObject instance)
        {
            var poolables = _poolableCache.Get(instance);
            for (var i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnPoolRelease();
            }
        }

        private void InvokePoolDestroy(GameObject instance)
        {
            var poolables = _poolableCache.Get(instance);
            for (var i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnPoolDestroy();
            }
        }
    }
}
