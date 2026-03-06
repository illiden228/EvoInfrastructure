using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace _Project.Scripts.Editor.EvoTools
{
    public static class MoveStartSceneMenu
    {
        private const string MENU_PATH = "EvoTools/Move Start Scene";
        private const string PLAY_MENU_PATH = "EvoTools/Play Start Scene";
        private const string FALLBACK_NAME = "EntryPointScene";

        [MenuItem(MENU_PATH, false, 20)]
        private static void MoveStartScene()
        {
            var scenePath = GetScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        [MenuItem(PLAY_MENU_PATH, false, 21)]
        private static void PlayStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scenePath = GetScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        private static string GetScenePath()
        {
            var buildScenes = EditorBuildSettings.scenes;
            if (buildScenes != null && buildScenes.Length > 0)
            {
                var first = buildScenes[0];
                if (first != null && !string.IsNullOrEmpty(first.path))
                {
                    return first.path;
                }
            }

            var fallback = EditorBuildSettings.scenes
                .FirstOrDefault(scene => scene != null &&
                                         !string.IsNullOrEmpty(scene.path) &&
                                         scene.path.EndsWith($"{FALLBACK_NAME}.unity"));

            if (fallback != null)
            {
                return fallback.path;
            }

            return null;
        }
    }
}
