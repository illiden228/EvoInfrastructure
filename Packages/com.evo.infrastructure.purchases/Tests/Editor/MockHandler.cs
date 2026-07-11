using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Purchases;

namespace Evo.Infrastructure.Purchases.Tests
{
    internal sealed class MockHandler : IPurchaseFulfillmentHandler
    {
        private readonly MockAdapter _adapter;
        public MockHandler(MockAdapter adapter) => _adapter = adapter;
        public bool WasCalled { get; private set; }
        public bool CanFulfill(string key) => key == "starter";
        public UniTask<PurchaseFulfillmentResult> FulfillAsync(PurchaseOffer offer,
            PurchaseTransaction transaction, CancellationToken token)
        { WasCalled = true; _adapter.Fulfilled = true; return UniTask.FromResult(PurchaseFulfillmentResult.Succeeded()); }
    }
}
