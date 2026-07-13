using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    [Serializable]
    public sealed class PurchaseAdapterBinding
    {
        [SerializeField] private string adapterId = string.Empty;
        [SerializeField] private bool enabled = true;
        [CatalogDropdown(CatalogDropdownKind.PlatformId)]
        [SerializeField] private List<string> platformIds = new();
        [SerializeField] private bool editorMock;
        [SerializeField] private int priority;
        public string AdapterId => adapterId?.Trim() ?? string.Empty;
        public bool Enabled => enabled;
        public IReadOnlyList<string> PlatformIds => platformIds;
        public bool EditorMock => editorMock;
        public int Priority => priority;
    }
}

