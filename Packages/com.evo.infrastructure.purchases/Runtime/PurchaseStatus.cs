using System;

namespace Evo.Infrastructure.Services.Purchases
{
    public enum PurchaseStatus
    {
        Succeeded = 0,
        NotInitialized = 1,
        Unavailable = 2,
        InvalidProduct = 3,
        ProductUnavailable = 4,
        Cancelled = 5,
        Deferred = 6,
        Timeout = 7,
        StoreFailure = 8,
        FulfillmentUnavailable = 9,
        FulfillmentFailed = 10,
        SdkException = 11
    }
}

