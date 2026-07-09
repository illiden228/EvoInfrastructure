using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.ResourceProvider;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public sealed class ResourceProviderUiItemViewFactory<TItemView> : IUiItemViewFactory<TItemView>
        where TItemView : Component
    {
        private readonly IResourceProviderService _resourceProvider;
        private readonly string _resourceId;

        public ResourceProviderUiItemViewFactory(IResourceProviderService resourceProvider, string resourceId)
        {
            _resourceProvider = resourceProvider;
            _resourceId = resourceId;
        }

        public async UniTask<TItemView> CreateAsync(Transform parent, int siblingIndex)
        {
            if (_resourceProvider == null || string.IsNullOrWhiteSpace(_resourceId) || parent == null)
            {
                return null;
            }

            var instance = await _resourceProvider.InstantiateAsync(_resourceId);
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, false);
            instance.transform.SetSiblingIndex(siblingIndex);
            if (instance.TryGetComponent<TItemView>(out var view))
            {
                return view;
            }

            _resourceProvider.DestroyInstance(instance);
            return null;
        }

        public void Release(TItemView view)
        {
            if (view == null)
            {
                return;
            }

            _resourceProvider?.DestroyInstance(view.gameObject);
        }
    }
}
