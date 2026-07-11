using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseCatalogIssue
    {
        public PurchaseCatalogIssue(PurchaseCatalogIssueSeverity severity, string productId, string message)
        {
            Severity = severity;
            ProductId = productId;
            Message = message;
        }
        public PurchaseCatalogIssueSeverity Severity { get; }
        public string ProductId { get; }
        public string Message { get; }
        public override string ToString() => string.IsNullOrEmpty(ProductId) ? Message : $"{ProductId}: {Message}";
    }
}

