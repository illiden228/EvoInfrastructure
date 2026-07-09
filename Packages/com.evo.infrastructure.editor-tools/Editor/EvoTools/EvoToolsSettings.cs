using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Evo.Infrastructure.Editor.EvoTools
{
    public sealed class EvoToolsSettings : ScriptableObject
    {
        [SerializeField] private Locale locale;
        [SerializeField] private string saveFileName = Evo.Infrastructure.Services.Save.SaveStorageDefaults.FileName;
        [SerializeField] private string[] playerPrefsSaveKeys =
        {
            Evo.Infrastructure.Services.Save.SaveStorageDefaults.PlayerPrefsKey
        };

        public Locale Locale => locale;
        public string SaveFileName => saveFileName;
        public IReadOnlyList<string> PlayerPrefsSaveKeys => playerPrefsSaveKeys;

        public void SetLocale(Locale newLocale)
        {
            locale = newLocale;
            ApplyLocale();
        }

        public void ApplyLocale()
        {
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }
    }
}
