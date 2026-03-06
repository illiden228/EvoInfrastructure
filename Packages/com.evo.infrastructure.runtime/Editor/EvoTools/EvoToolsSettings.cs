using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class EvoToolsSettings : ScriptableObject
    {
        [SerializeField] private Locale locale;
        [SerializeField] private string saveFileName = "save.json";
        [SerializeField] private string[] playerPrefsSaveKeys =
        {
            "BLINDSHOT_SAVE_FULL_PREFS",
            "BLINDSHOT_SAVE_FULL_YG_CACHE"
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
