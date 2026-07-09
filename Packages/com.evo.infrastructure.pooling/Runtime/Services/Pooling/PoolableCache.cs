using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Pooling
{
    internal sealed class PoolableCache
    {
        private readonly Dictionary<GameObject, IPoolable[]> _poolablesByInstance = new();

        public IPoolable[] Get(GameObject instance)
        {
            if (instance == null)
            {
                return System.Array.Empty<IPoolable>();
            }

            if (_poolablesByInstance.TryGetValue(instance, out var poolables))
            {
                return poolables;
            }

            poolables = CollectPoolables(instance);
            _poolablesByInstance[instance] = poolables;
            return poolables;
        }

        public void Remove(GameObject instance)
        {
            if (instance != null)
            {
                _poolablesByInstance.Remove(instance);
            }
        }

        public void Clear()
        {
            _poolablesByInstance.Clear();
        }

        private static IPoolable[] CollectPoolables(GameObject instance)
        {
            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours.Length == 0)
            {
                return System.Array.Empty<IPoolable>();
            }

            var poolables = new List<IPoolable>(behaviours.Length);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPoolable poolable)
                {
                    poolables.Add(poolable);
                }
            }

            return poolables.Count == 0 ? System.Array.Empty<IPoolable>() : poolables.ToArray();
        }
    }
}
