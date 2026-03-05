using Cysharp.Threading.Tasks;
using _Project.Scripts.Application.UI;
using _Project.Scripts.Application.UI.Views;

namespace _Project.Scripts.Infrastructure.Services.UI
{
    public sealed class UiHandle
    {
        private readonly UniTaskCompletionSource _closedTcs = new();
        private readonly UiService _service;

        internal UiHandle(UiService service, UiViewBase view, IUiViewModel viewModel, UiLayer layer, bool keepAlive)
        {
            _service = service;
            View = view;
            ViewModel = viewModel;
            Layer = layer;
            KeepAlive = keepAlive;
        }

        public UiViewBase View { get; }
        public IUiViewModel ViewModel { get; }
        public UiLayer Layer { get; }
        public bool KeepAlive { get; }

        public UniTask Closed => _closedTcs.Task;

        public UniTask CloseAsync()
        {
            return _service != null ? _service.CloseAsync(this) : UniTask.CompletedTask;
        }

        internal void MarkClosed()
        {
            _closedTcs.TrySetResult();
        }
    }
}
