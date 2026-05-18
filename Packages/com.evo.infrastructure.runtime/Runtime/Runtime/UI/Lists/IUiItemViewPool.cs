using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public interface IUiItemViewPool<TItemView>
        where TItemView : Component
    {
        TItemView Get(Transform parent);
        void Release(TItemView view);
        void Clear();
    }
}
