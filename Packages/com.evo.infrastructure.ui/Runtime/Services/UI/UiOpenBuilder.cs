using System;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Runtime.UI;

namespace Evo.Infrastructure.Services.UI
{
    public readonly struct UiOpenBuilder<TViewModel>
        where TViewModel : class, IUiViewModel
    {
        private readonly IUiService _uiService;
        private readonly string _viewId;
        private readonly UiOpenOptions _options;

        internal UiOpenBuilder(IUiService uiService, string viewId, UiOpenOptions options)
        {
            _uiService = uiService;
            _viewId = viewId;
            _options = options;
        }

        public UiOpenBuilder<TViewModel> WithViewId(string viewId)
        {
            return new UiOpenBuilder<TViewModel>(_uiService, viewId, _options);
        }

        public UiOpenBuilder<TViewModel> WithOptions(UiOpenOptions options)
        {
            return new UiOpenBuilder<TViewModel>(_uiService, _viewId, options);
        }

        public UiOpenBuilder<TViewModel> KeepAcrossSceneLoads(bool keepAcrossSceneLoads = true)
        {
            var options = CloneOptions(_options);
            options.KeepAcrossSceneLoadsOverride = keepAcrossSceneLoads;
            return new UiOpenBuilder<TViewModel>(_uiService, _viewId, options);
        }

        public UiOpenBuilder<TViewModel> WithViewModelFactory(Func<TViewModel> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var options = CloneOptions(_options);
            options.ViewModelFactory = () => factory();
            return new UiOpenBuilder<TViewModel>(_uiService, _viewId, options);
        }

        public UniTask<UiHandle> OpenAsync()
        {
            return _uiService.OpenAsync<TViewModel>(_viewId, _options);
        }

        public UniTask<UiHandle> WithContextAsync<TContext>(TContext context)
        {
            if (!typeof(IUiContextReceiver<TContext>).IsAssignableFrom(typeof(TViewModel)))
            {
                throw new InvalidOperationException(
                    $"ViewModel '{typeof(TViewModel).Name}' must implement '{typeof(IUiContextReceiver<TContext>).Name}' to receive context '{typeof(TContext).Name}'.");
            }

            return _uiService.OpenAsync<TViewModel>(_viewId, WithContext(_options, context));
        }

        private static UiOpenOptions CloneOptions(UiOpenOptions options)
        {
            return new UiOpenOptions
            {
                LayerOverride = options?.LayerOverride,
                OpenModeOverride = options?.OpenModeOverride,
                KeepAliveOverride = options?.KeepAliveOverride,
                KeepHistory = options?.KeepHistory ?? false,
                KeepAcrossSceneLoadsOverride = options?.KeepAcrossSceneLoadsOverride,
                Context = options?.Context,
                ContextType = options?.ContextType,
                ContextPayload = options?.ContextPayload,
                ViewModelFactory = options?.ViewModelFactory
            };
        }

        private static UiOpenOptions WithContext<TContext>(UiOpenOptions options, TContext context)
        {
            return new UiOpenOptions
            {
                LayerOverride = options?.LayerOverride,
                OpenModeOverride = options?.OpenModeOverride,
                KeepAliveOverride = options?.KeepAliveOverride,
                KeepHistory = options?.KeepHistory ?? false,
                KeepAcrossSceneLoadsOverride = options?.KeepAcrossSceneLoadsOverride,
                ViewModelFactory = options?.ViewModelFactory,
                ContextType = typeof(TContext),
                ContextPayload = new UiContextPayload<TContext>(context)
            };
        }
    }
}
