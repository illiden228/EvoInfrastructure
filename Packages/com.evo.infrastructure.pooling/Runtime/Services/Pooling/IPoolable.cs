namespace Evo.Infrastructure.Services.Pooling
{
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolRelease();
        void OnPoolDestroy();
    }
}
