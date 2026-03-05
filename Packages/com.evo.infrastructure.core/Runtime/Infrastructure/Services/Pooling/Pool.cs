using System;
using System.Collections.Generic;

namespace _Project.Scripts.Infrastructure.Services.Pooling
{
    public sealed class Pool<T>
    {
        private readonly Func<T> _create;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly Stack<T> _items = new();
        private readonly int _maxSize;

        public Pool(
            Func<T> create,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int prewarmCount = 0,
            int maxSize = 0)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = Math.Max(0, maxSize);

            for (var i = 0; i < prewarmCount; i++)
            {
                var item = _create();
                _onRelease?.Invoke(item);
                _items.Push(item);
            }
        }

        public T Get()
        {
            T item = default;
            while (_items.Count > 0 && EqualityComparer<T>.Default.Equals(item, default))
            {
                item = _items.Pop();
            }

            if (EqualityComparer<T>.Default.Equals(item, default))
            {
                item = _create();
            }

            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (EqualityComparer<T>.Default.Equals(item, default))
            {
                return;
            }

            if (_maxSize > 0 && _items.Count >= _maxSize)
            {
                _onDestroy?.Invoke(item);
                return;
            }

            _onRelease?.Invoke(item);
            _items.Push(item);
        }
    }
}
