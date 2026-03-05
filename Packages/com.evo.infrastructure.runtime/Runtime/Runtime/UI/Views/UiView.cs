namespace _Project.Scripts.Application.UI.Views
{
    public abstract class UiView<TViewModel> : UiViewBase where TViewModel : class, IUiViewModel
    {
        protected TViewModel TypedViewModel { get; private set; }

        public override void Bind(IUiViewModel viewModel)
        {
            base.Bind(viewModel);
            TypedViewModel = viewModel as TViewModel;
            OnBind(TypedViewModel);
        }

        public override void Unbind()
        {
            OnUnbind(TypedViewModel);
            TypedViewModel = null;
            base.Unbind();
        }

        protected virtual void OnBind(TViewModel viewModel)
        {
        }

        protected virtual void OnUnbind(TViewModel viewModel)
        {
        }
    }
}
