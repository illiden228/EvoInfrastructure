using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Purchases
{
    public interface IPurchaseService : IDisposable
    {
        bool IsInitialized { get; }
        bool IsAvailable { get; }
        string ActiveAdapterId { get; }
        IReadOnlyList<PurchaseProduct> Products { get; }
        event Action CatalogChanged;
        event Action<PurchaseTransaction> PurchaseCompleted;
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        bool TryGetProduct(string productId, out PurchaseProduct product);
        UniTask<PurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default);
        UniTask<IReadOnlyList<PurchaseResult>> RestoreAsync(CancellationToken cancellationToken = default);
    }
}

