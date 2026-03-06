using System.IO;
using _Project.Scripts.Infrastructure.Services.Debug;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor.EvoTools
{
    public static class SaveToolsMenu
    {
        private const string MENU_CLEAR_SAVE = "EvoTools/Clear Save";
        private const int MAX_DIALOG_BUTTON_TEXT_LENGTH = 63;
        private const string SOURCE = nameof(SaveToolsMenu);
        private const string SETTINGS_FOLDER = "Assets/_Project/Configs/Editor";
        private const string SETTINGS_ASSET_NAME = "EvoToolsSettings.asset";
        private static readonly string[] DefaultSaveKeys =
        {
            "BLINDSHOT_SAVE_FULL_PREFS",
            "BLINDSHOT_SAVE_FULL_YG_CACHE"
        };

        [MenuItem(MENU_CLEAR_SAVE, false, 80)]
        private static void ClearSave()
        {
            var title = EvoToolsLocalization.Get("save_tools.clear.title", "Clear Save");
            var message = EvoToolsLocalization.Get(
                "save_tools.clear.confirm",
                "Delete local profile save file and cached save keys?");
            var confirm = GetDialogButtonText("save_tools.clear.confirm_button", "Clear");
            var cancel = GetDialogButtonText("save_tools.clear.cancel_button", "Cancel");

            if (!EditorUtility.DisplayDialog(title, message, confirm, cancel))
            {
                return;
            }

            var settings = LoadOrCreateSettings();
            var saveFileName = settings != null && !string.IsNullOrWhiteSpace(settings.SaveFileName)
                ? settings.SaveFileName
                : "save.json";
            var saveKeys = settings?.PlayerPrefsSaveKeys != null && settings.PlayerPrefsSaveKeys.Count > 0
                ? settings.PlayerPrefsSaveKeys
                : DefaultSaveKeys;

            var filePath = Path.Combine(UnityEngine.Application.persistentDataPath, saveFileName);
            var deletedFile = false;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                deletedFile = true;
            }

            var deletedPrefs = false;
            for (var i = 0; i < saveKeys.Count; i++)
            {
                var key = saveKeys[i];
                if (string.IsNullOrWhiteSpace(key) || !PlayerPrefs.HasKey(key))
                {
                    continue;
                }

                PlayerPrefs.DeleteKey(key);
                deletedPrefs = true;
            }

            if (deletedPrefs)
            {
                PlayerPrefs.Save();
            }

            AssetDatabase.Refresh();

            var result = deletedFile || deletedPrefs
                ? EvoToolsLocalization.Get("save_tools.clear.done", "Save cleared.")
                : EvoToolsLocalization.Get("save_tools.clear.nothing", "Save was already clean.");

            EvoDebug.Log($"{title}: {result}", SOURCE);
        }

        private static string GetDialogButtonText(string key, string fallback)
        {
            var localized = EvoToolsLocalization.Get(key, fallback);
            if (string.IsNullOrWhiteSpace(localized) || localized.Length > MAX_DIALOG_BUTTON_TEXT_LENGTH)
            {
                return fallback;
            }

            return localized;
        }

        private static EvoToolsSettings LoadOrCreateSettings()
        {
            var assetPath = $"{SETTINGS_FOLDER}/{SETTINGS_ASSET_NAME}";
            var settings = AssetDatabase.LoadAssetAtPath<EvoToolsSettings>(assetPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolderExists(SETTINGS_FOLDER);
            settings = ScriptableObject.CreateInstance<EvoToolsSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void EnsureFolderExists(string folder)
        {
            var normalized = folder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var segments = normalized.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
