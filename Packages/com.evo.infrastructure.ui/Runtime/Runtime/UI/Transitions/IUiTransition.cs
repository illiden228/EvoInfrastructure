using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Runtime.UI.Views;

namespace Evo.Infrastructure.Runtime.UI.Transitions
{
    public interface IUiTransition
    {
        UniTask ShowAsync(UiViewBase view);
        UniTask HideAsync(UiViewBase view);
    }
}
