using System;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Application.UI;
using _Project.Scripts.Application.UI.Views;

namespace _Project.Scripts.Infrastructure.Services.UI
{
    public interface IUiService
    {
        UniTask<UiHandle> OpenAsync<TViewModel>(string viewId = null, UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel;
        UniTask<UiHandle> OpenAsync(Type viewModelType, string viewId = null, UiOpenOptions options = null);
        UniTask<UiHandle> OpenAsync(UiViewBase view, UiOpenOptions options = null);
        void RegisterSceneView(UiViewBase view);
        UniTask<UiHandle> ShowAsync(UiViewBase view, UiOpenOptions options = null);
        UniTask HideAsync(UiViewBase view);
        void Reload();
    }
}
