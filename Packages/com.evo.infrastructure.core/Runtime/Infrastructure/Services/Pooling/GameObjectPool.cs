using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Pooling
{
    public sealed class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Pool<GameObject> _pool;

        public GameObjectPool(GameObject prefab, int prewarmCount, int maxSize, Transform root = null)
        {
            _prefab = prefab;
            _root = root;
            _pool = _prefab == null
                ? null
                : new Pool<GameObject>(
                    CreateInstance,
                    OnGet,
                    OnRelease,
                    OnDestroy,
                    prewarmCount,
                    maxSize);
        }

        public GameObject Get()
        {
            return _pool?.Get();
        }

        public void Release(GameObject instance)
        {
            _pool?.Release(instance);
        }

        private GameObject CreateInstance()
        {
            return Object.Instantiate(_prefab, _root);
        }

        private void OnGet(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.SetParent(null, true);
            instance.SetActive(true);
        }

        private void OnRelease(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_root, true);
        }

        private static void OnDestroy(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Object.Destroy(instance);
        }
    }
}
