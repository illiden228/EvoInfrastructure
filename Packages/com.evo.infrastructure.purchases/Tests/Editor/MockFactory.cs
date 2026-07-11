using Evo.Infrastructure.Services.Purchases;

namespace Evo.Infrastructure.Purchases.Tests
{
    internal sealed class MockFactory : IPurchaseAdapterFactory
    {
        private readonly IPurchaseAdapter _adapter;
        public MockFactory(IPurchaseAdapter adapter) => _adapter = adapter;
        public string AdapterId => "mock";
        public IPurchaseAdapter Create() => _adapter;
    }
}
