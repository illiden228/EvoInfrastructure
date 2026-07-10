using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Evo.Infrastructure.Editor.EvoTools
{
    public static class EvoToolsLocalization
    {
        private const string LANGUAGE_PREFS_KEY = "Evo.Infrastructure.EditorTools.Language";
        private const string ENGLISH_CODE = "en";
        private const string RUSSIAN_CODE = "ru";
        private const string LOCALIZATION_ROOT =
            "Packages/com.evo.infrastructure.editor-tools/Editor/EvoTools/Localization";

        private static readonly string[] AvailableLanguageCodes =
        {
            ENGLISH_CODE,
            RUSSIAN_CODE
        };

        private static readonly Dictionary<string, StringTable> Tables =
            new(StringComparer.OrdinalIgnoreCase);

        public static string CurrentLanguageCode
        {
            get
            {
                var fallback = Application.systemLanguage == SystemLanguage.Russian
                    ? RUSSIAN_CODE
                    : ENGLISH_CODE;
                return NormalizeLanguageCode(EditorPrefs.GetString(LANGUAGE_PREFS_KEY, fallback));
            }
        }

        public static IReadOnlyList<string> GetAvailableLanguageCodes()
        {
            return AvailableLanguageCodes;
        }

        public static string GetLanguageDisplayName(string languageCode)
        {
            return NormalizeLanguageCode(languageCode) == RUSSIAN_CODE ? "Русский" : "English";
        }

        public static void SetLanguage(string languageCode)
        {
            EditorPrefs.SetString(LANGUAGE_PREFS_KEY, NormalizeLanguageCode(languageCode));
        }

        public static string Get(string key)
        {
            return Get(key, key);
        }

        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            var languageCode = CurrentLanguageCode;
            var value = GetFromTable(languageCode, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (!string.Equals(languageCode, ENGLISH_CODE, StringComparison.OrdinalIgnoreCase))
            {
                value = GetFromTable(ENGLISH_CODE, key);
            }

            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string GetFromTable(string languageCode, string key)
        {
            var table = LoadTable(languageCode);
            return table?.GetEntry(key)?.LocalizedValue;
        }

        private static StringTable LoadTable(string languageCode)
        {
            languageCode = NormalizeLanguageCode(languageCode);
            if (Tables.TryGetValue(languageCode, out var cached) && cached != null)
            {
                return cached;
            }

            var path = $"{LOCALIZATION_ROOT}/EvoTools_{languageCode}.asset";
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(path);
            Tables[languageCode] = table;
            return table;
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.Equals(languageCode, RUSSIAN_CODE, StringComparison.OrdinalIgnoreCase)
                ? RUSSIAN_CODE
                : ENGLISH_CODE;
        }
    }
}
