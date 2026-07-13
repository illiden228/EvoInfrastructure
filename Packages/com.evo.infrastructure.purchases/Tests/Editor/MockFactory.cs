using Evo.Infrastructure.Services.Purchases;

namespace Evo.Infrastructure.Purchases.Tests
{
    internal sealed class MockFactory : IPurchaseAdapterFactory
    {
        private readonly IPurchaseAdapter _adapter;
        public MockFactory(IPurchaseAdapter adapter, string adapterId = "mock")
        {
            _adapter = adapter;
            AdapterId = adapterId;
        }
        public string AdapterId { get; }
        public IPurchaseAdapter Create() => _adapter;
    }
}
