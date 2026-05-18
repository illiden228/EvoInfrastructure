using Evo.Infrastructure.Runtime.UI;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public abstract class PrefabBindableListView<TItemViewModel, TItemView> : BindableListView<TItemViewModel, TItemView>
        where TItemView : Component, IUiBindable<TItemViewModel>
    {
        [SerializeField] private TItemView _itemPrefab;
        [SerializeField] private bool _usePooling = true;

        protected override IUiItemViewFactory<TItemView> CreateFactory()
        {
            return new PrefabUiItemViewFactory<TItemView>(_itemPrefab, _usePooling);
        }
    }
}
