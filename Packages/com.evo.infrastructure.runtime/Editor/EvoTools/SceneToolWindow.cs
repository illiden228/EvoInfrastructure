using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _Project.Scripts.Editor.EvoTools
{
    public sealed class SceneToolWindow : OdinEditorWindow
    {
        private const string DEFAULT_SCENES_FOLDER = "Assets/_Project/Scenes";

        [MenuItem("EvoTools/Scenes", false, 30)]
        private static void Open()
        {
            GetWindow<SceneToolWindow>(EvoToolsLocalization.Get("scene_tools.window_title", "Scene Tools"));
        }

        [Title("@EvoToolsLocalization.Get(\"scene_tools.title.filters\", \"Filters\")")]
        [LabelText("@EvoToolsLocalization.Get(\"scene_tools.search\", \"Search\")")]
        public string Search = "";

        [FolderPath(AbsolutePath = false, RequireExistingPath = false)]
        [LabelText("@EvoToolsLocalization.Get(\"scene_tools.scenes_folder\", \"Scenes Folder\")")]
        public string ScenesFolder = DEFAULT_SCENES_FOLDER;

        [LabelText("@EvoToolsLocalization.Get(\"scene_tools.only_in_build\", \"Only In Build\")")]
        public bool OnlyInBuild = false;

        private List<SceneEntryView> _scenes;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(ScenesFolder))
            {
                ScenesFolder = DEFAULT_SCENES_FOLDER;
            }
            Refresh();
        }

        [Button(ButtonSizes.Medium, Name = "@EvoToolsLocalization.Get(\"scene_tools.button.refresh\", \"Refresh\")")]
        private void Refresh()
        {
            var allScenes = FindAllScenes(ScenesFolder);
            var buildMap = BuildMap();

            _scenes = allScenes
                .Select(path => new SceneEntryView(path, buildMap.TryGetValue(path, out var info) && info.Enabled))
                .OrderBy(s => s.Name)
                .ToList();
        }

        [OnInspectorGUI]
        private void DrawScenes()
        {
            if (_scenes == null)
            {
                Refresh();
            }

            var search = (Search ?? string.Empty).Trim();
            var list = _scenes;

            if (OnlyInBuild)
            {
                list = list.Where(s => s.InBuild).ToList();
            }

            if (search.Length > 0)
            {
                list = list
                    .Where(s => s.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                s.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            foreach (var scene in list)
            {
                DrawSceneRow(scene);
            }
        }

        private void DrawSceneRow(SceneEntryView scene)
        {
            var isOpen = EditorSceneManager.GetSceneByPath(scene.Path).isLoaded;
            var prevColor = GUI.color;
            var buildInfo = GetBuildInfo(scene.Path);

            EditorGUILayout.BeginHorizontal();
            if (isOpen)
            {
                GUI.color = new Color(0.55f, 0.9f, 0.6f);
                GUILayout.Label("●", GUILayout.Width(12));
            GUILayout.Label(scene.Name, GUILayout.Width(188));
                GUI.color = prevColor;
            }
            else
            {
                GUILayout.Label(" ", GUILayout.Width(12));
                GUILayout.Label(scene.Name, GUILayout.Width(188));
            }
            GUILayout.Label(scene.Path, GUILayout.ExpandWidth(true));

            if (buildInfo.Enabled)
            {
                GUILayout.Label(buildInfo.Index.ToString(), GUILayout.Width(22));
            }
            else
            {
                GUILayout.Label("-", GUILayout.Width(22));
            }

            var inBuild = GUILayout.Toggle(
                scene.InBuild,
                EvoToolsLocalization.Get("scene_tools.in_build", "In Build"),
                GUILayout.Width(70));
            if (inBuild != scene.InBuild)
            {
                scene.InBuild = inBuild;
                SetBuildState(scene.Path, inBuild);
            }

            using (new EditorGUI.DisabledScope(isOpen))
            {
                var openLabel = EvoToolsLocalization.Get("scene_tools.button.open", "Open");
                if (GUILayout.Button(openLabel, GUILayout.Width(GetButtonWidth(openLabel))))
                {
                    EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Single);
                }

                var additiveLabel = EvoToolsLocalization.Get("scene_tools.button.additive", "Additive");
                if (GUILayout.Button(additiveLabel, GUILayout.Width(GetButtonWidth(additiveLabel))))
                {
                    EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Additive);
                }
            }

            using (new EditorGUI.DisabledScope(!isOpen || EditorSceneManager.sceneCount <= 1))
            {
                var closeLabel = EvoToolsLocalization.Get("scene_tools.button.close", "Close");
                if (GUILayout.Button(closeLabel, GUILayout.Width(GetButtonWidth(closeLabel))))
                {
                    CloseScene(scene.Path);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.color = prevColor;
        }

        private static void CloseScene(string path)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (scene.isDirty)
            {
                if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene }))
                {
                    return;
                }
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void SetBuildState(string path, bool enabled)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FirstOrDefault(s => s.path == path);
            if (existing != null)
            {
                existing.enabled = enabled;
            }
            else if (enabled)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private readonly struct BuildInfo
        {
            public readonly bool Enabled;
            public readonly int Index;

            public BuildInfo(bool enabled, int index)
            {
                Enabled = enabled;
                Index = index;
            }
        }

        private static BuildInfo GetBuildInfo(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (string.Equals(scenes[i].path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return new BuildInfo(scenes[i].enabled, i);
                }
            }

            return new BuildInfo(false, -1);
        }

        private static Dictionary<string, BuildInfo> BuildMap()
        {
            var map = new Dictionary<string, BuildInfo>(StringComparer.OrdinalIgnoreCase);
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                map[scenes[i].path] = new BuildInfo(scenes[i].enabled, i);
            }

            return map;
        }

        private static float GetButtonWidth(string label)
        {
            var content = new GUIContent(label ?? string.Empty);
            var size = GUI.skin.button.CalcSize(content);
            return size.x + 6f;
        }

        private static List<string> FindAllScenes(string folder)
        {
            var searchFolders = string.IsNullOrEmpty(folder) ? null : new[] { folder };
            var guids = AssetDatabase.FindAssets("t:Scene", searchFolders);
            var list = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(path);
                }
            }

            return list;
        }

        private sealed class SceneEntryView
        {
            public readonly string Path;
            public readonly string Name;
            public bool InBuild;

            public SceneEntryView(string path, bool inBuild)
            {
                Path = path;
                Name = PathToName(path);
                InBuild = inBuild;
            }

            private static string PathToName(string path)
            {
                var file = System.IO.Path.GetFileNameWithoutExtension(path);
                return string.IsNullOrEmpty(file) ? path : file;
            }
        }
    }
}
