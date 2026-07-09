using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public sealed class PrefabUiItemViewFactory<TItemView> : IUiItemViewFactory<TItemView>, IDisposable
        where TItemView : Component
    {
        private readonly TItemView _prefab;
        private readonly IUiItemViewPool<TItemView> _pool;

        public PrefabUiItemViewFactory(TItemView prefab, bool usePooling = false)
        {
            _prefab = prefab;
            _pool = usePooling && prefab != null
                ? new UiItemViewPool<TItemView>(prefab)
                : null;
        }

        public UniTask<TItemView> CreateAsync(Transform parent, int siblingIndex)
        {
            if (parent == null)
            {
                return UniTask.FromResult<TItemView>(null);
            }

            var view = _pool != null
                ? _pool.Get(parent)
                : CreateFromPrefab(parent);

            if (view != null)
            {
                view.transform.SetParent(parent, false);
                view.transform.SetSiblingIndex(siblingIndex);
                view.gameObject.SetActive(true);
            }

            return UniTask.FromResult(view);
        }

        public void Release(TItemView view)
        {
            if (view == null)
            {
                return;
            }

            if (_pool != null)
            {
                _pool.Release(view);
                return;
            }

            UnityEngine.Object.Destroy(view.gameObject);
        }

        public void Dispose()
        {
            _pool?.Clear();
        }

        private TItemView CreateFromPrefab(Transform parent)
        {
            if (_prefab == null)
            {
                return null;
            }

            var view = UnityEngine.Object.Instantiate(_prefab, parent, false);
            view.gameObject.SetActive(true);
            return view;
        }
    }
}
