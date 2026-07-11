using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [Serializable]
    public sealed class PurchaseGrantDefinition
    {
        [SerializeField] private string type = string.Empty;
        [SerializeField] private string id = string.Empty;
        [SerializeField] private long quantity = 1;
        [SerializeField, TextArea] private string payload = string.Empty;

        public string Type => type?.Trim() ?? string.Empty;
        public string Id => id?.Trim() ?? string.Empty;
        public long Quantity => quantity;
        public string Payload => payload ?? string.Empty;
    }
}

