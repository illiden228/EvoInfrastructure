#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Evo.Infrastructure.Core.Editor
{
    public sealed class InfrastructureSetupWizardWindow : EditorWindow
    {
        private const string RuntimePackageName = "com.evo.infrastructure.runtime";
        private const string RuntimeGitTag = "v0.2.0";
        private const string RuntimeGitUrl = "https://github.com/illiden228/EvoInfrastructure.git?path=Packages/com.evo.infrastructure.runtime";
        private const string R3GitUrl = "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity";
        private const string EntryScenePath = "Assets/_Project/Scenes/EntryPointScene.unity";
        private const string LoadingScenePath = "Assets/_Project/Scenes/LoadingScene.unity";
        private const string TransitionScenePath = "Assets/_Project/Scenes/TransitionScene.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
        private const string ProjectConfigPath = "Assets/_Project/Configs/ProjectConfig.asset";
        private const string UiSystemConfigPath = "Assets/_Project/Configs/UiSystemConfig.asset";
        private const string ConfigCatalogPath = "Assets/_Project/Configs/ScriptableConfigCatalog.asset";
        private const string LifetimeScopePrefabPath = "Assets/_Project/Prefabs/Runtime/InfrastructureProjectLifetimeScope.prefab";

        private static readonly string[] RequiredDependencyIds =
        {
            "jp.hadashikick.vcontainer",
            "com.cysharp.unitask",
            "com.github-glitchenzo.nugetforunity"
        };

        private static readonly string[] RequiredDependencySources =
        {
            "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
            "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
            "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity"
        };

        private static readonly string[] StructureFolders =
        {
            "Assets/_Project",
            "Assets/_Project/Scenes",
            "Assets/_Project/Configs",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Runtime",
            "Assets/_Project/Scripts/Runtime/Bootstrap"
        };

        private readonly Queue<string> _installQueue = new();
        private AddRequest _addRequest;
        private ListRequest _listRequest;
        private bool _dependenciesInstalled;
        private bool _runtimeInstalled;
        private bool _structureReady;
        private bool _r3Ready;
        private bool _observableCollectionsReady;
        private bool _isRefreshingState;
        private string _statusLine = "Ready";

        [MenuItem("Tools/Evo/Infrastructure Setup Wizard")]
        public static void OpenWindow()
        {
            var window = GetWindow<InfrastructureSetupWizardWindow>("Evo Setup");
            window.minSize = new Vector2(620f, 420f);
            window.RefreshState();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshState();
            EditorApplication.update += UpdateInstallQueue;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateInstallQueue;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawState();
            GUILayout.Space(10f);
            DrawActions();
            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(_statusLine, MessageType.Info);
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Evo Infrastructure Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Step-by-step project bootstrap.", EditorStyles.wordWrappedLabel);
        }

        private void DrawState()
        {
            EditorGUILayout.Space(8f);
            DrawStatusRow("Dependencies installed", _dependenciesInstalled);
            DrawStatusRow("R3 installed", _r3Ready);
            DrawStatusRow("ObservableCollections installed", _observableCollectionsReady);
            DrawStatusRow("Project structure created", _structureReady);
            DrawStatusRow("Infrastructure runtime installed", _runtimeInstalled);
            DrawStatusRow("Starter runtime scaffold ready", HasStarterScaffold());
        }

        private static void DrawStatusRow(string label, bool ready)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(320f));
            var old = GUI.color;
            GUI.color = ready ? new Color(0.25f, 0.7f, 0.25f) : new Color(0.8f, 0.3f, 0.3f);
            EditorGUILayout.LabelField(ready ? "Ready" : "Missing", GUILayout.Width(80f));
            GUI.color = old;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            if (GUILayout.Button("1) Install Dependencies", GUILayout.Height(34f)))
            {
                InstallDependencies();
            }

            if (GUILayout.Button("2) Create Project Structure", GUILayout.Height(34f)))
            {
                CreateProjectStructure();
            }

            if (GUILayout.Button("Install R3 (Git URL)", GUILayout.Height(30f)))
            {
                InstallR3FromGit();
            }

            using (new EditorGUI.DisabledScope(!_dependenciesInstalled || !_r3Ready || !_observableCollectionsReady))
            {
                if (GUILayout.Button("3) Install Infrastructure Runtime", GUILayout.Height(34f)))
                {
                    InstallRuntimePackage();
                }
            }

            using (new EditorGUI.DisabledScope(!_dependenciesInstalled || !_r3Ready || !_observableCollectionsReady || !_runtimeInstalled))
            {
                if (GUILayout.Button("4) Setup Starter Runtime Scaffold", GUILayout.Height(34f)))
                {
                    SetupStarterRuntimeScaffold();
                }
            }

            if (GUILayout.Button("Refresh State", GUILayout.Height(26f)))
            {
                RefreshState();
            }

            DrawReactiveWarning();
        }

        private void InstallDependencies()
        {
            _installQueue.Clear();
            for (var i = 0; i < RequiredDependencySources.Length; i++)
            {
                _installQueue.Enqueue(RequiredDependencySources[i]);
            }

            _statusLine = "Installing dependencies (VContainer, UniTask, NuGetForUnity)...";
        }

        private void InstallRuntimePackage()
        {
            _installQueue.Enqueue($"{RuntimeGitUrl}#{RuntimeGitTag}");
            _statusLine = $"Installing runtime package from git tag {RuntimeGitTag}...";
        }

        private void InstallR3FromGit()
        {
            _installQueue.Enqueue(R3GitUrl);
            _statusLine = "Installing R3 from Git URL...";
        }

        private void CreateProjectStructure()
        {
            for (var i = 0; i < StructureFolders.Length; i++)
            {
                EnsureFolder(StructureFolders[i]);
            }

            AssetDatabase.Refresh();
            _statusLine = "Project structure created.";
            RefreshState();
        }

        private void SetupStarterRuntimeScaffold()
        {
            CreateProjectStructure();
            EnsureFolder("Assets/_Project/Prefabs/Runtime");
            EnsureScene(EntryScenePath, "EntryPointRoot");
            EnsureScene(LoadingScenePath, "LoadingRoot");
            EnsureScene(TransitionScenePath, "TransitionRoot");
            EnsureScene(MenuScenePath, "MainMenuRoot");
            EnsureDefaultAssets();
            EnsureBuildScenes();
            AssetDatabase.Refresh();
            _statusLine = "Starter runtime scaffold created.";
            RefreshState();
        }

        private void EnsureDefaultAssets()
        {
            CreateScriptableAsset(
                "_Project.Scripts.Application.Config.ProjectConfig, Evo.Infrastructure.Runtime",
                ProjectConfigPath);
            CreateScriptableAsset(
                "_Project.Scripts.Application.UI.UiSystemConfig, Evo.Infrastructure.Runtime",
                UiSystemConfigPath);
            CreateScriptableAsset(
                "_Project.Scripts.Infrastructure.Services.Config.ScriptableConfigCatalog, Evo.Infrastructure.Runtime",
                ConfigCatalogPath);
            CreateLifetimeScopePrefab();
        }

        private void CreateScriptableAsset(string typeName, string assetPath)
        {
            if (File.Exists(assetPath))
            {
                return;
            }

            var type = Type.GetType(typeName);
            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return;
            }

            var instance = ScriptableObject.CreateInstance(type);
            if (instance == null)
            {
                return;
            }

            AssetDatabase.CreateAsset(instance, assetPath);
        }

        private void CreateLifetimeScopePrefab()
        {
            if (File.Exists(LifetimeScopePrefabPath))
            {
                return;
            }

            var type = Type.GetType("_Project.Scripts.Runtime.Bootstrap.RuntimeProjectLifetimeScope, Evo.Infrastructure.Runtime");
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return;
            }

            var go = new GameObject("InfrastructureProjectLifetimeScope");
            go.AddComponent(type);
            PrefabUtility.SaveAsPrefabAsset(go, LifetimeScopePrefabPath);
            DestroyImmediate(go);
        }

        private void EnsureScene(string path, string rootName)
        {
            if (File.Exists(path))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private void EnsureBuildScenes()
        {
            var required = new[]
            {
                EntryScenePath,
                LoadingScenePath,
                TransitionScenePath,
                MenuScenePath
            };

            var current = EditorBuildSettings.scenes?.ToList() ?? new List<EditorBuildSettingsScene>();
            for (var i = 0; i < required.Length; i++)
            {
                var path = required[i];
                if (current.Any(x => string.Equals(x.path, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                current.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = current.ToArray();
        }

        private void EnsureFolder(string path)
        {
            var normalized = path.Replace("\\", "/").TrimEnd('/');
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

        private void UpdateInstallQueue()
        {
            if (_addRequest != null)
            {
                if (!_addRequest.IsCompleted)
                {
                    return;
                }

                if (_addRequest.Status == StatusCode.Success)
                {
                    _statusLine = $"Installed: {_addRequest.Result.packageId}";
                }
                else
                {
                    _statusLine = $"Install failed: {_addRequest.Error?.message}";
                }

                _addRequest = null;
                RefreshState();
            }

            if (_addRequest == null && _installQueue.Count > 0)
            {
                var source = _installQueue.Dequeue();
                _addRequest = Client.Add(source);
            }
        }

        private void RefreshState()
        {
            if (_isRefreshingState)
            {
                return;
            }

            _isRefreshingState = true;
            _listRequest = Client.List(true, false);
            EditorApplication.update += WaitForList;
        }

        private void WaitForList()
        {
            if (_listRequest == null || !_listRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= WaitForList;
            if (_listRequest.Status == StatusCode.Success)
            {
                var names = new HashSet<string>(_listRequest.Result.Select(x => x.name), StringComparer.OrdinalIgnoreCase);
                _dependenciesInstalled = RequiredDependencyIds.All(names.Contains);
                _runtimeInstalled = names.Contains(RuntimePackageName);
                _r3Ready = IsAssemblyLoaded("R3");
                _observableCollectionsReady = IsAssemblyLoaded("ObservableCollections");
            }

            _structureReady = HasProjectStructure();
            _isRefreshingState = false;
            Repaint();
        }

        private bool HasProjectStructure()
        {
            for (var i = 0; i < StructureFolders.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(StructureFolders[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasStarterScaffold()
        {
            return File.Exists(EntryScenePath) &&
                   File.Exists(LoadingScenePath) &&
                   File.Exists(TransitionScenePath) &&
                   File.Exists(MenuScenePath);
        }

        private void DrawReactiveWarning()
        {
            if (_r3Ready && _observableCollectionsReady)
            {
                return;
            }

            var missing = new List<string>(2);
            if (!_r3Ready)
            {
                missing.Add("R3");
            }

            if (!_observableCollectionsReady)
            {
                missing.Add("ObservableCollections");
            }

            var message =
                $"Before steps 3 and 4 install missing reactive libraries: {string.Join(", ", missing)}.\n" +
                "Use 'Install R3 (Git URL)' for R3. Install ObservableCollections via NuGetForUnity.";
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var name = assemblies[i].GetName().Name;
                if (string.Equals(name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
