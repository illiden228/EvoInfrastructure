using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Purchases
{
    public interface IPurchaseAdapter : IDisposable
    {
        string AdapterId { get; }
        bool IsInitialized { get; }
        bool IsAvailable { get; }
        IReadOnlyList<PurchaseStoreProduct> Products { get; }
        UniTask InitializeAsync(IReadOnlyList<PurchaseAdapterProductDefinition> products, CancellationToken cancellationToken);
        UniTask<PurchaseAdapterResult> PurchaseAsync(string offerId, string storeProductId, CancellationToken cancellationToken);
        UniTask<IReadOnlyList<PurchaseTransaction>> RestoreAsync(CancellationToken cancellationToken);
        UniTask<bool> ConfirmAsync(PurchaseTransaction transaction, CancellationToken cancellationToken);
    }
}

