#if ODIN_INSPECTOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using EvoDebug = _Project.Scripts.Infrastructure.Services.Debug.EvoDebug;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class EvoToolsSettingsWindow : OdinEditorWindow
    {
        private const string SETTINGS_FOLDER = "Assets/_Project/Configs/Editor";
        private const string SETTINGS_ASSET_NAME = "EvoToolsSettings.asset";

        [MenuItem("EvoTools/Settings", false, 60)]
        private static void Open()
        {
            GetWindow<EvoToolsSettingsWindow>(EvoToolsLocalization.Get("evotools_settings.window_title", "EvoTools Settings"));
        }

        [InlineEditor(Expanded = true)]
        [ReadOnly]
        [ShowInInspector]
        private EvoToolsSettings Settings;

        [Title("@EvoToolsLocalization.Get(\"evotools_settings.title.localization\", \"Localization\")")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("@EvoToolsLocalization.Get(\"evotools_settings.locale\", \"Language\")")]
        private string CurrentLocaleName =>
            Settings != null && Settings.Locale != null ? Settings.Locale.LocaleName : "-";

        [Title("@EvoToolsLocalization.Get(\"evotools_settings.title.save\", \"Save Tools\")")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("@EvoToolsLocalization.Get(\"evotools_settings.save_file_name\", \"Save File Name\")")]
        private string CurrentSaveFileName => Settings != null ? Settings.SaveFileName : "-";

        [ShowInInspector]
        [ReadOnly]
        [LabelText("@EvoToolsLocalization.Get(\"evotools_settings.save_keys\", \"Save Keys\")")]
        private string CurrentSaveKeys =>
            Settings?.PlayerPrefsSaveKeys != null ? string.Join(", ", Settings.PlayerPrefsSaveKeys) : "-";

        [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"evotools_settings.button.open_tables\", \"Open Localization Tables\")")]
        private void OpenLocalizationTables()
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Localization Tables");
        }

        [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"evotools_settings.button.ping_table\", \"Ping EvoTools Table\")")]
        private void PingEvoToolsTable()
        {
            var tables = AssetDatabase.FindAssets("t:StringTableCollection EvoTools");
            if (tables.Length == 0)
            {
                EvoDebug.LogWarning(
                    EvoToolsLocalization.Get("evotools_settings.warning.table_not_found", "EvoTools table not found."),
                    nameof(EvoToolsSettingsWindow));
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(tables[0]);
            var asset = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Settings = LoadOrCreateSettings();
            if (Settings != null)
            {
                Settings.ApplyLocale();
            }
        }

        [OnInspectorGUI]
        private void DrawLocaleSelector()
        {
            if (Settings == null)
            {
                return;
            }

            var locales = EvoToolsLocalization.GetAvailableLocales();
            if (locales == null || locales.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    EvoToolsLocalization.Get("evotools_settings.warning.no_locales", "No locales found."),
                    MessageType.Warning);
                return;
            }

            GUILayout.Space(4);
            GUILayout.Label(
                EvoToolsLocalization.Get("evotools_settings.title.localization", "Localization"),
                EditorStyles.boldLabel);

            var names = locales.Select(l => l.LocaleName).ToArray();
            var currentIndex = GetLocaleIndex(locales, Settings.Locale);
            var columns = Mathf.Min(3, names.Length);
            var newIndex = GUILayout.SelectionGrid(currentIndex, names, columns);

            if (newIndex != currentIndex && newIndex >= 0 && newIndex < locales.Count)
            {
                Undo.RecordObject(Settings, "Change EvoTools Locale");
                Settings.SetLocale(locales[newIndex]);
                EditorUtility.SetDirty(Settings);
            }
        }

        private static int GetLocaleIndex(IReadOnlyList<Locale> locales, Locale current)
        {
            if (current == null)
            {
                return 0;
            }

            for (var i = 0; i < locales.Count; i++)
            {
                if (locales[i] == current)
                {
                    return i;
                }
            }

            return 0;
        }

        internal static EvoToolsSettings LoadOrCreateSettings()
        {
            var assetPath = $"{SETTINGS_FOLDER}/{SETTINGS_ASSET_NAME}";
            var settings = AssetDatabase.LoadAssetAtPath<EvoToolsSettings>(assetPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolderExists(SETTINGS_FOLDER);
            settings = CreateInstance<EvoToolsSettings>();
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
#endif
