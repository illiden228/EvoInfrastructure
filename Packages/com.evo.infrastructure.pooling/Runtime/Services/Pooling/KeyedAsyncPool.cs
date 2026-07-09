using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Pooling
{
    public sealed class KeyedAsyncPool<TKey, TValue> : IDisposable
    {
        private readonly Func<TKey, CancellationToken, UniTask<TValue>> _createAsync;
        private readonly Action<TKey, TValue> _onGet;
        private readonly Action<TKey, TValue> _onRelease;
        private readonly Action<TKey, TValue> _onDestroy;
        private readonly Func<TKey, int> _resolveMaxInactive;
        private readonly Dictionary<TKey, Bucket> _buckets;
        private readonly Dictionary<TValue, TKey> _activeKeysByValue;
        private readonly bool _trackActive;
        private bool _disposed;

        public KeyedAsyncPool(
            Func<TKey, CancellationToken, UniTask<TValue>> createAsync,
            Action<TKey, TValue> onGet = null,
            Action<TKey, TValue> onRelease = null,
            Action<TKey, TValue> onDestroy = null,
            Func<TKey, int> resolveMaxInactive = null,
            IEqualityComparer<TKey> comparer = null,
            bool trackActive = true)
        {
            _createAsync = createAsync ?? throw new ArgumentNullException(nameof(createAsync));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _resolveMaxInactive = resolveMaxInactive;
            _buckets = new Dictionary<TKey, Bucket>(comparer);
            _trackActive = trackActive;
            _activeKeysByValue = _trackActive ? new Dictionary<TValue, TKey>() : null;
        }

        public IReadOnlyCollection<TKey> Keys => _buckets.Keys;

        public async UniTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var bucket = GetOrCreateBucket(key);

            var value = PopInactive(bucket);
            if (IsValid(value))
            {
                TrackGet(key, value, bucket);
                return value;
            }

            await bucket.CreateLock.WaitAsync(cancellationToken);
            try
            {
                ThrowIfDisposed();
                value = PopInactive(bucket);
                if (!IsValid(value))
                {
                    value = await _createAsync(key, cancellationToken);
                    if (IsValid(value))
                    {
                        bucket.CreatedCount++;
                    }
                }

                if (!IsValid(value))
                {
                    return default;
                }

                TrackGet(key, value, bucket);
                return value;
            }
            finally
            {
                bucket.CreateLock.Release();
            }
        }

        public bool Release(TKey key, TValue value)
        {
            if (_disposed || !IsValid(value) || !_buckets.TryGetValue(key, out var bucket))
            {
                return false;
            }

            if (_trackActive)
            {
                if (!_activeKeysByValue.TryGetValue(value, out var activeKey) ||
                    !EqualityComparer<TKey>.Default.Equals(activeKey, key))
                {
                    return false;
                }

                _activeKeysByValue.Remove(value);
                bucket.Active.Remove(value);
            }

            var maxInactive = Math.Max(0, _resolveMaxInactive?.Invoke(key) ?? 0);
            if (maxInactive > 0 && bucket.Inactive.Count >= maxInactive)
            {
                DestroyValue(key, value, bucket);
                return true;
            }

            _onRelease?.Invoke(key, value);
            bucket.Inactive.Push(value);
            return true;
        }

        public bool Release(TValue value)
        {
            if (!_trackActive || _disposed || !IsValid(value) || !_activeKeysByValue.TryGetValue(value, out var key))
            {
                return false;
            }

            return Release(key, value);
        }

        public bool TryGetStatistics(TKey key, out PoolStatistics statistics)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                statistics = CreateStatistics(bucket);
                return true;
            }

            statistics = default;
            return false;
        }

        public Dictionary<TKey, PoolStatistics> GetStatistics()
        {
            var result = new Dictionary<TKey, PoolStatistics>(_buckets.Comparer);
            foreach (var pair in _buckets)
            {
                result[pair.Key] = CreateStatistics(pair.Value);
            }

            return result;
        }

        public void ClearInactive()
        {
            foreach (var pair in _buckets)
            {
                var bucket = pair.Value;
                while (bucket.Inactive.Count > 0)
                {
                    DestroyValue(pair.Key, bucket.Inactive.Pop(), bucket);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var pair in _buckets)
            {
                var bucket = pair.Value;
                while (bucket.Inactive.Count > 0)
                {
                    DestroyValue(pair.Key, bucket.Inactive.Pop(), bucket);
                }

                if (bucket.Active != null)
                {
                    foreach (var value in bucket.Active)
                    {
                        DestroyValue(pair.Key, value, bucket);
                    }

                    bucket.Active.Clear();
                }

                bucket.CreateLock.Dispose();
            }

            _activeKeysByValue?.Clear();
            _buckets.Clear();
        }

        private Bucket GetOrCreateBucket(TKey key)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                return bucket;
            }

            bucket = new Bucket(_trackActive);
            _buckets.Add(key, bucket);
            return bucket;
        }

        private TValue PopInactive(Bucket bucket)
        {
            while (bucket.Inactive.Count > 0)
            {
                var value = bucket.Inactive.Pop();
                if (IsValid(value))
                {
                    return value;
                }
            }

            return default;
        }

        private void TrackGet(TKey key, TValue value, Bucket bucket)
        {
            if (_trackActive)
            {
                bucket.Active.Add(value);
                _activeKeysByValue[value] = key;
            }

            _onGet?.Invoke(key, value);
        }

        private void DestroyValue(TKey key, TValue value, Bucket bucket)
        {
            if (!IsValid(value))
            {
                return;
            }

            _onDestroy?.Invoke(key, value);
            bucket.DestroyedCount++;
        }

        private static PoolStatistics CreateStatistics(Bucket bucket)
        {
            return new PoolStatistics(
                bucket.Active?.Count ?? 0,
                bucket.Inactive.Count,
                bucket.CreatedCount,
                bucket.DestroyedCount);
        }

        private static bool IsValid(TValue value)
        {
            return !EqualityComparer<TValue>.Default.Equals(value, default);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KeyedAsyncPool<TKey, TValue>));
            }
        }

        private sealed class Bucket
        {
            public readonly Stack<TValue> Inactive = new();
            public readonly HashSet<TValue> Active;
            public readonly SemaphoreSlim CreateLock = new(1, 1);
            public int CreatedCount;
            public int DestroyedCount;

            public Bucket(bool trackActive)
            {
                Active = trackActive ? new HashSet<TValue>() : null;
            }
        }
    }
}
