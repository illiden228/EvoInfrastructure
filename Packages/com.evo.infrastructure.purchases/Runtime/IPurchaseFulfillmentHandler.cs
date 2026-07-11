using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Purchases
{
    public interface IPurchaseFulfillmentHandler
    {
        bool CanFulfill(string fulfillmentKey);
        UniTask<PurchaseFulfillmentResult> FulfillAsync(PurchaseOffer offer, PurchaseTransaction transaction,
            CancellationToken cancellationToken);
    }
}

