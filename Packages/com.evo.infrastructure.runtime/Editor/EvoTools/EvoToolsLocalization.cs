using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace _Project.Scripts.Editor.EvoTools
{
    public static class EvoToolsLocalization
    {
        private const string TABLE_NAME = "EvoTools";
        private static bool _initialized;
        private static bool _initInProgress;
        private static AsyncOperationHandle _initHandle;

        public static Locale CurrentLocale => LocalizationSettings.SelectedLocale;

        public static IReadOnlyList<Locale> GetAvailableLocales()
        {
            EnsureInitialized();
            return LocalizationSettings.AvailableLocales.Locales;
        }

        public static void SetLocale(Locale locale)
        {
            EnsureInitialized();
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        public static string Get(string key)
        {
            return Get(key, key);
        }

        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            EnsureInitialized();
            var value = LocalizationSettings.StringDatabase.GetLocalizedString(TABLE_NAME, key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var handle = LocalizationSettings.InitializationOperation;
            if (handle.IsValid() && !handle.IsDone)
            {
                HookInitialize(handle);
                return;
            }

            _initialized = true;
        }

        private static void HookInitialize(AsyncOperationHandle handle)
        {
            if (_initInProgress && _initHandle.Equals(handle))
            {
                return;
            }

            _initInProgress = true;
            _initHandle = handle;
            handle.Completed += _ =>
            {
                _initInProgress = false;
                _initialized = true;
            };
        }
    }
}
