namespace Evo.Infrastructure.Runtime.UI
{
    public interface IUiBindable<in TViewModel>
    {
        void Bind(TViewModel viewModel);
    }
}
