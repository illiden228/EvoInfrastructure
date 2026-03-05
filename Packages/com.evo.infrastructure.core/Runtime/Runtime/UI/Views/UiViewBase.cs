using _Project.Scripts.Application.UI.Transitions;
using R3;
using UnityEngine;

namespace _Project.Scripts.Application.UI.Views
{
    public abstract class UiViewBase : MonoBehaviour
    {
        [SerializeReference] private IUiTransition transition;
        private IUiViewModel _viewModel;
        protected CompositeDisposable Disposables { get; } = new();

        public IUiViewModel ViewModel => _viewModel;

        public virtual void Bind(IUiViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public virtual void Unbind()
        {
            Disposables.Clear();
            _viewModel = null;
        }

        public IUiTransition GetTransition() => transition;

        protected virtual void OnDestroy()
        {
            Disposables.Clear();
        }
    }
}
