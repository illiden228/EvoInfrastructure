using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Pooling
{
    public sealed class ListPool<T>
    {
        private readonly Stack<List<T>> _inactive = new();
        private readonly int _maxInactive;
        private int _createdCount;

        public ListPool(int maxInactive = 32, int prewarmCount = 0)
        {
            _maxInactive = Math.Max(0, maxInactive);
            for (var i = 0; i < prewarmCount; i++)
            {
                _inactive.Push(new List<T>());
                _createdCount++;
            }
        }

        public int InactiveCount => _inactive.Count;
        public int CreatedCount => _createdCount;

        public List<T> Get()
        {
            while (_inactive.Count > 0)
            {
                var list = _inactive.Pop();
                if (list != null)
                {
                    return list;
                }
            }

            _createdCount++;
            return new List<T>();
        }

        public bool Release(List<T> list)
        {
            if (list == null)
            {
                return false;
            }

            list.Clear();
            if (_maxInactive > 0 && _inactive.Count >= _maxInactive)
            {
                return false;
            }

            _inactive.Push(list);
            return true;
        }

        public void Clear()
        {
            _inactive.Clear();
        }
    }
}
