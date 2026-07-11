using System;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseFulfillmentResult
    {
        public PurchaseFulfillmentResult(bool success, string error = null)
        {
            Success = success;
            Error = error;
        }
        public bool Success { get; }
        public string Error { get; }
        public static PurchaseFulfillmentResult Succeeded() => new(true);
        public static PurchaseFulfillmentResult Failed(string error) => new(false, error);
    }
}

