using System;

namespace Evo.Infrastructure.Services.Save
{
    [Serializable]
    public sealed class SaveBackendSelectionRule
    {
        public string backendId = string.Empty;
        public SaveBackendUsage usage = SaveBackendUsage.Enabled;
    }
}
