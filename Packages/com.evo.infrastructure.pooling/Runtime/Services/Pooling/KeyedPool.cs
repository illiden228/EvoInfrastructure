using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Pooling
{
    public sealed class KeyedPool<TKey, TValue> : IDisposable
    {
        private readonly Func<TKey, Pool<TValue>> _createPool;
        private readonly Dictionary<TKey, Pool<TValue>> _pools;
        private bool _disposed;

        public KeyedPool(Func<TKey, Pool<TValue>> createPool, IEqualityComparer<TKey> comparer = null)
        {
            _createPool = createPool ?? throw new ArgumentNullException(nameof(createPool));
            _pools = new Dictionary<TKey, Pool<TValue>>(comparer);
        }

        public IReadOnlyDictionary<TKey, Pool<TValue>> Pools => _pools;

        public TValue Get(TKey key)
        {
            ThrowIfDisposed();
            return GetOrCreatePool(key).Get();
        }

        public bool Release(TKey key, TValue value)
        {
            if (_disposed || !_pools.TryGetValue(key, out var pool))
            {
                return false;
            }

            return pool.Release(value);
        }

        public bool TryGetPool(TKey key, out Pool<TValue> pool)
        {
            return _pools.TryGetValue(key, out pool);
        }

        public Pool<TValue> GetOrCreatePool(TKey key)
        {
            ThrowIfDisposed();
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool;
            }

            pool = _createPool(key) ?? throw new InvalidOperationException($"Pool factory returned null for key '{key}'.");
            _pools.Add(key, pool);
            return pool;
        }

        public void ClearInactive()
        {
            foreach (var pool in _pools.Values)
            {
                pool.ClearInactive();
            }
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

            _pools.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KeyedPool<TKey, TValue>));
            }
        }
    }
}
