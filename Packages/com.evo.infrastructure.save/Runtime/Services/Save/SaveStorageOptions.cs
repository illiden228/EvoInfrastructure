using System;

namespace Evo.Infrastructure.Services.Save
{
    [Serializable]
    public sealed class SaveStorageOptions
    {
        public string playerPrefsKey = SaveStorageDefaults.PlayerPrefsKey;
        public string fileName = SaveStorageDefaults.FileName;
        public SaveBackendSelectionPolicy backendSelection = new();
    }
}
