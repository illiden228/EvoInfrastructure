using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Pooling
{
    public sealed class Pool<T> : IDisposable
    {
        private readonly Func<T> _create;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly Stack<T> _inactive = new();
        private readonly HashSet<T> _active;
        private readonly int _maxInactive;
        private readonly bool _trackActive;
        private bool _disposed;
        private int _createdCount;
        private int _destroyedCount;

        public Pool(
            Func<T> create,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int prewarmCount = 0,
            int maxSize = 0,
            bool trackActive = true)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxInactive = Math.Max(0, maxSize);
            _trackActive = trackActive;
            _active = _trackActive ? new HashSet<T>() : null;

            Prewarm(prewarmCount);
        }

        public int InactiveCount => _inactive.Count;
        public int ActiveCount => _active?.Count ?? 0;
        public int CreatedCount => _createdCount;
        public int DestroyedCount => _destroyedCount;
        public PoolStatistics Statistics => new(ActiveCount, InactiveCount, CreatedCount, DestroyedCount);

        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            for (var i = 0; i < count; i++)
            {
                var item = CreateItem();
                if (IsDefault(item))
                {
                    continue;
                }

                _onRelease?.Invoke(item);
                _inactive.Push(item);
            }
        }

        public T Get()
        {
            ThrowIfDisposed();

            var item = PopInactive();
            if (IsDefault(item))
            {
                item = CreateItem();
            }

            if (IsDefault(item))
            {
                return default;
            }

            _active?.Add(item);
            _onGet?.Invoke(item);
            return item;
        }

        public bool Release(T item)
        {
            if (_disposed || IsDefault(item))
            {
                return false;
            }

            if (_trackActive && !_active.Remove(item))
            {
                return false;
            }

            if (_maxInactive > 0 && _inactive.Count >= _maxInactive)
            {
                DestroyItem(item);
                return true;
            }

            _onRelease?.Invoke(item);
            _inactive.Push(item);
            return true;
        }

        public void ClearInactive()
        {
            while (_inactive.Count > 0)
            {
                DestroyItem(_inactive.Pop());
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ClearInactive();

            if (_active == null)
            {
                return;
            }

            foreach (var item in _active)
            {
                DestroyItem(item);
            }

            _active.Clear();
        }

        private T CreateItem()
        {
            var item = _create();
            if (!IsDefault(item))
            {
                _createdCount++;
            }

            return item;
        }

        private T PopInactive()
        {
            while (_inactive.Count > 0)
            {
                var item = _inactive.Pop();
                if (!IsDefault(item))
                {
                    return item;
                }
            }

            return default;
        }

        private void DestroyItem(T item)
        {
            if (IsDefault(item))
            {
                return;
            }

            _onDestroy?.Invoke(item);
            _destroyedCount++;
        }

        private static bool IsDefault(T item)
        {
            return EqualityComparer<T>.Default.Equals(item, default);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Pool<T>));
            }
        }
    }
}
