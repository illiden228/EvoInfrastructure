using Cysharp.Threading.Tasks;
using _Project.Scripts.Application.UI.Views;

namespace _Project.Scripts.Application.UI.Transitions
{
    public interface IUiTransition
    {
        UniTask ShowAsync(UiViewBase view);
        UniTask HideAsync(UiViewBase view);
    }
}
