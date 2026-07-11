using System;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseResult
    {
        public PurchaseResult(PurchaseStatus status, PurchaseTransaction transaction = default, string error = null)
        {
            Status = status;
            Transaction = transaction;
            Error = error;
        }
        public PurchaseStatus Status { get; }
        public PurchaseTransaction Transaction { get; }
        public string Error { get; }
        public bool Success => Status == PurchaseStatus.Succeeded;
    }
}

