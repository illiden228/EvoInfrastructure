using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Purchases;

namespace Evo.Infrastructure.Purchases.Tests
{
    internal sealed class MockAdapter : IPurchaseAdapter
    {
        public string AdapterId => "mock";
        public bool IsInitialized { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool Fulfilled { get; set; }
        public bool ConfirmedAfterFulfillment { get; private set; }
        public IReadOnlyList<PurchaseStoreProduct> Products { get; private set; } = Array.Empty<PurchaseStoreProduct>();
        public UniTask InitializeAsync(IReadOnlyList<PurchaseAdapterProductDefinition> products, CancellationToken token)
        {
            Products = new[] { new PurchaseStoreProduct("store.starter", true) };
            IsAvailable = true;
            IsInitialized = true;
            return UniTask.CompletedTask;
        }
        public UniTask<PurchaseAdapterResult> PurchaseAsync(string offerId, string storeProductId, CancellationToken token) =>
            UniTask.FromResult(new PurchaseAdapterResult(PurchaseStatus.Succeeded,
                new PurchaseTransaction("tx-1", offerId, storeProductId, AdapterId)));
        public UniTask<IReadOnlyList<PurchaseTransaction>> RestoreAsync(CancellationToken token) =>
            UniTask.FromResult<IReadOnlyList<PurchaseTransaction>>(Array.Empty<PurchaseTransaction>());
        public UniTask<bool> ConfirmAsync(PurchaseTransaction transaction, CancellationToken token)
        { ConfirmedAfterFulfillment = Fulfilled; return UniTask.FromResult(true); }
        public void Dispose() { }
    }
}
