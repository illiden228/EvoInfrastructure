using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public sealed class AddressableUiItemViewFactory<TItemView> : IUiItemViewFactory<TItemView>
        where TItemView : Component
    {
        private readonly AssetReferenceT<GameObject> _prefabReference;

        public AddressableUiItemViewFactory(AssetReferenceT<GameObject> prefabReference)
        {
            _prefabReference = prefabReference;
        }

        public async UniTask<TItemView> CreateAsync(Transform parent, int siblingIndex)
        {
            if (_prefabReference == null || !_prefabReference.RuntimeKeyIsValid() || parent == null)
            {
                return null;
            }

            var handle = _prefabReference.InstantiateAsync(parent);
            await handle.Task;

            var instance = handle.Result;
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetSiblingIndex(siblingIndex);
            if (instance.TryGetComponent<TItemView>(out var view))
            {
                return view;
            }

            Addressables.ReleaseInstance(instance);
            return null;
        }

        public void Release(TItemView view)
        {
            if (view == null)
            {
                return;
            }

            Addressables.ReleaseInstance(view.gameObject);
        }
    }
}
