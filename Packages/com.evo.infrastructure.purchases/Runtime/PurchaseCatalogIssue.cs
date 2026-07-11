using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases
{
    public readonly struct PurchaseCatalogIssue
    {
        public PurchaseCatalogIssue(PurchaseCatalogIssueSeverity severity, string offerId, string message)
        {
            Severity = severity;
            OfferId = offerId;
            Message = message;
        }
        public PurchaseCatalogIssueSeverity Severity { get; }
        public string OfferId { get; }
        public string Message { get; }
        public override string ToString() => string.IsNullOrEmpty(OfferId) ? Message : $"{OfferId}: {Message}";
    }
}

