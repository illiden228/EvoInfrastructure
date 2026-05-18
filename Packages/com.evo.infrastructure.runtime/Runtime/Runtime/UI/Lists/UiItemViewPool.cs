using System.Collections.Generic;
using Evo.Infrastructure.Runtime.UI;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public sealed class UiItemViewPool<TItemView> : IUiItemViewPool<TItemView>
        where TItemView : Component
    {
        private readonly TItemView _prefab;
        private readonly Stack<TItemView> _pool = new();

        public UiItemViewPool(TItemView prefab)
        {
            _prefab = prefab;
        }

        public TItemView Get(Transform parent)
        {
            while (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                if (pooled == null)
                {
                    continue;
                }

                pooled.transform.SetParent(parent, false);
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            if (_prefab == null)
            {
                return null;
            }

            var view = Object.Instantiate(_prefab, parent, false);
            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(TItemView view)
        {
            if (view == null)
            {
                return;
            }

            if (view is IUiUnbindable unbindable)
            {
                unbindable.Unbind();
            }

            view.gameObject.SetActive(false);
            view.transform.SetParent(null, false);
            _pool.Push(view);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var view = _pool.Pop();
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
        }
    }
}
