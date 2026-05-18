using R3;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI
{
    public abstract class UiBindableViewBase<TViewModel> : MonoBehaviour, IUiLifecycleView<TViewModel>
    {
        private readonly CompositeDisposable _disposables = new();
        private bool _isDisposed;

        protected TViewModel ViewModel { get; private set; }
        protected CompositeDisposable Disposables => _disposables;
        protected bool IsBound { get; private set; }

        public void Bind(TViewModel viewModel)
        {
            if (_isDisposed)
            {
                return;
            }

            Unbind();
            ViewModel = viewModel;
            IsBound = true;
            OnBind(viewModel);
        }

        public void Unbind()
        {
            if (!IsBound)
            {
                return;
            }

            var viewModel = ViewModel;
            OnUnbind(viewModel);
            _disposables.Clear();
            ViewModel = default;
            IsBound = false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Unbind();
            _disposables.Dispose();
            OnDispose();
        }

        protected abstract void OnBind(TViewModel viewModel);

        protected virtual void OnUnbind(TViewModel viewModel)
        {
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }
    }
}
