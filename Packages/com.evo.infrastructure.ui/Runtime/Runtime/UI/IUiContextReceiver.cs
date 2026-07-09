namespace Evo.Infrastructure.Runtime.UI
{
    public interface IUiContextReceiver<in TContext>
    {
        void SetContext(TContext context);
    }
}
