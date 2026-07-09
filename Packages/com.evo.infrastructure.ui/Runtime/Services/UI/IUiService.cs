using System;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Runtime.UI;
using Evo.Infrastructure.Runtime.UI.Views;

namespace Evo.Infrastructure.Services.UI
{
    public interface IUiService
    {
        UiOpenBuilder<TViewModel> Open<TViewModel>(string viewId = null, UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel;
        UniTask<UiHandle> OpenAsync<TViewModel>(string viewId = null, UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel;
        UniTask<UiHandle> OpenAsync<TViewModel, TContext>(
            TContext context,
            string viewId = null,
            UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel, IUiContextReceiver<TContext>;
        UniTask<UiHandle> OpenAsync(Type viewModelType, string viewId = null, UiOpenOptions options = null);
        UniTask<UiHandle> OpenAsync(UiViewBase view, UiOpenOptions options = null);
        void RegisterSceneView(UiViewBase view);
        UniTask<UiHandle> ShowAsync(UiViewBase view, UiOpenOptions options = null);
        UniTask HideAsync(UiViewBase view);
        void Reload();
    }
}
