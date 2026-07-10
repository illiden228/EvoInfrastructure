using System.Collections.Generic;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools
{
    public sealed class EvoToolsSettings : ScriptableObject
    {
        [SerializeField] private string saveFileName = Evo.Infrastructure.Services.Save.SaveStorageDefaults.FileName;
        [SerializeField] private string[] playerPrefsSaveKeys =
        {
            Evo.Infrastructure.Services.Save.SaveStorageDefaults.PlayerPrefsKey
        };

        public string SaveFileName => saveFileName;
        public IReadOnlyList<string> PlayerPrefsSaveKeys => playerPrefsSaveKeys;
    }
}
