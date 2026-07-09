using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public interface IUiItemViewFactory<TItemView>
        where TItemView : Component
    {
        UniTask<TItemView> CreateAsync(Transform parent, int siblingIndex);
        void Release(TItemView view);
    }
}
