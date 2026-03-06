using System.Linq;
using _Project.Scripts.Application.UI;
using _Project.Scripts.Infrastructure.Services.UI;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor.EvoTools
{
    public static class UiSystemMenu
    {
        private const string MENU_OPEN_CONFIG = "EvoTools/UI Config";
        private const string MENU_RELOAD_UI = "EvoTools/Reload UI";

        [MenuItem(MENU_OPEN_CONFIG)]
        private static void OpenConfig()
        {
            var config = FindConfig();
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }

        [MenuItem(MENU_RELOAD_UI)]
        private static void ReloadUi()
        {
            var config = FindConfig();
            if (config != null)
            {
                config.RebuildViewsFromMenu();
                EditorGUIUtility.PingObject(config);
            }

            // Reload in play mode should be triggered via DI entry points, not static access.
        }

        private static UiSystemConfig FindConfig()
        {
            var guid = AssetDatabase.FindAssets("t:UiSystemConfig").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<UiSystemConfig>(path);
        }
    }
}
