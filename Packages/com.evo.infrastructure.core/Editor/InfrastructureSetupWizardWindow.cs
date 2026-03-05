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
        private const string RuntimeGitTag = "v0.3.2";
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

        private const string VContainerSource = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer";
        private const string UniTaskSource = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask";
        private const string NuGetForUnitySource = "https://github.com/GlitchEnzo/NuGetForUnity.git?path=src/NuGetForUnity";
        private const string AddressablesSource = "com.unity.addressables@2.8.1";
        private const string LocalizationSource = "com.unity.localization@1.5.9";
        private const string InputSystemSource = "com.unity.inputsystem@1.7.0";
        private const string UguiSource = "com.unity.ugui@2.0.0";

        private static readonly string[] StructureFolders =
        {
            "Assets/_Project",
            "Assets/_Project/Animations",
            "Assets/_Project/Audio",
            "Assets/_Project/Scenes",
            "Assets/_Project/Configs",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/Materials",
            "Assets/_Project/Models",
            "Assets/_Project/Sprites",
            "Assets/_Project/Textures",
            "Assets/_Project/VFX",
            "Assets/_Project/Fonts",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Runtime",
            "Assets/_Project/Scripts/Runtime/EntryPoint"
        };

        private readonly Queue<string> _installQueue = new();
        private AddRequest _addRequest;
        private ListRequest _listRequest;
        private bool _dependenciesInstalled;
        private bool _runtimeInstalled;
        private bool _structureReady;
        private bool _r3Ready;
        private bool _observableCollectionsReady;
        private bool _vContainerInstalled;
        private bool _uniTaskInstalled;
        private bool _nuGetForUnityInstalled;
        private bool _addressablesInstalled;
        private bool _localizationInstalled;
        private bool _inputSystemInstalled;
        private bool _uguiInstalled;
        private bool _isRefreshingState;
        private bool _isInstalling;
        private string _statusLine = "Ready";

        [MenuItem("EvoTools/Setup")]
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
            DrawProgress();
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
            DrawStatusRow("VContainer installed", _vContainerInstalled);
            DrawStatusRow("UniTask installed", _uniTaskInstalled);
            DrawStatusRow("NuGetForUnity installed", _nuGetForUnityInstalled);
            DrawStatusRow("Addressables installed", _addressablesInstalled);
            DrawStatusRow("Localization installed", _localizationInstalled);
            DrawStatusRow("Input System installed", _inputSystemInstalled);
            DrawStatusRow("UGUI installed", _uguiInstalled);
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
            var depsDone = _dependenciesInstalled;
            var canInstallDeps = !_isInstalling;
            DrawActionButton(
                depsDone ? "1) Install Dependencies (Already done)" : "1) Install Dependencies",
                depsDone
                    ? "Dependencies already installed."
                    : "Install all missing dependencies in sequence (add -> wait -> next): VContainer, UniTask, NuGetForUnity, Addressables, Localization, InputSystem, UGUI.",
                !depsDone && canInstallDeps,
                InstallDependencies);

            DrawActionButton(
                _vContainerInstalled ? "1.1) Install VContainer (Already done)" : "1.1) Install VContainer",
                _vContainerInstalled
                    ? "VContainer is already installed."
                    : "Install VContainer from Git URL.",
                !_vContainerInstalled && canInstallDeps,
                InstallVContainer);

            DrawActionButton(
                _uniTaskInstalled ? "1.2) Install UniTask (Already done)" : "1.2) Install UniTask",
                _uniTaskInstalled
                    ? "UniTask is already installed."
                    : "Install UniTask from Git URL.",
                !_uniTaskInstalled && canInstallDeps,
                InstallUniTask);

            DrawActionButton(
                _nuGetForUnityInstalled ? "1.3) Install NuGetForUnity (Already done)" : "1.3) Install NuGetForUnity",
                _nuGetForUnityInstalled
                    ? "NuGetForUnity is already installed."
                    : "Install NuGetForUnity from Git URL.",
                !_nuGetForUnityInstalled && canInstallDeps,
                InstallNuGetForUnity);

            var structureDone = _structureReady;
            DrawActionButton(
                structureDone ? "2) Create Project Structure (Already done)" : "2) Create Project Structure",
                structureDone
                    ? "Project folder structure is already ready."
                    : "Create base folders under Assets/_Project.",
                !structureDone,
                CreateProjectStructure);

            var r3Done = _r3Ready;
            var canInstallR3 = _dependenciesInstalled && !r3Done && !_isInstalling;
            DrawActionButton(
                r3Done ? "3) Install R3 (Git URL) (Already done)" : "3) Install R3 (Git URL)",
                r3Done
                    ? "R3 is already installed."
                    : canInstallR3
                        ? "Install R3 Unity package from Git URL."
                        : "Requires: Step 1 (Install Dependencies).",
                canInstallR3,
                InstallR3FromGit);

            var runtimeDone = _runtimeInstalled;
            var canInstallRuntime = _dependenciesInstalled && _r3Ready && _observableCollectionsReady && !runtimeDone && !_isInstalling;
            DrawActionButton(
                runtimeDone ? "4) Install Infrastructure Runtime (Already done)" : "4) Install Infrastructure Runtime",
                runtimeDone
                    ? "Infrastructure runtime is already installed."
                    : canInstallRuntime
                        ? "Install runtime package from Git tag."
                        : "Requires: Step 1 + R3 + ObservableCollections.",
                canInstallRuntime,
                InstallRuntimePackage);

            var scaffoldDone = HasStarterScaffold();
            var canSetupScaffold = _dependenciesInstalled && _r3Ready && _observableCollectionsReady && _runtimeInstalled && !scaffoldDone && !_isInstalling;
            DrawActionButton(
                scaffoldDone ? "5) Setup Starter Runtime Scaffold (Already done)" : "5) Setup Starter Runtime Scaffold",
                scaffoldDone
                    ? "Starter runtime scaffold is already created."
                    : canSetupScaffold
                        ? "Create starter scenes, configs and build settings."
                        : "Requires: Step 4 (Infrastructure Runtime installed).",
                canSetupScaffold,
                SetupStarterRuntimeScaffold);

            DrawActionButton(
                "Refresh State",
                "Re-check installed packages and setup status.",
                true,
                RefreshState,
                26f);

            DrawReactiveWarning();
        }

        private void DrawActionButton(string label, string tooltip, bool enabled, Action onClick, float height = 34f)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(height)))
                {
                    onClick?.Invoke();
                }
            }

            var rect = GUILayoutUtility.GetLastRect();
            if (rect.Contains(Event.current.mousePosition))
            {
                _statusLine = tooltip;
                Repaint();
            }
        }

        private void InstallDependencies()
        {
            _installQueue.Clear();
            if (!_vContainerInstalled) _installQueue.Enqueue(VContainerSource);
            if (!_uniTaskInstalled) _installQueue.Enqueue(UniTaskSource);
            if (!_nuGetForUnityInstalled) _installQueue.Enqueue(NuGetForUnitySource);
            if (!_addressablesInstalled) _installQueue.Enqueue(AddressablesSource);
            if (!_localizationInstalled) _installQueue.Enqueue(LocalizationSource);
            if (!_inputSystemInstalled) _installQueue.Enqueue(InputSystemSource);
            if (!_uguiInstalled) _installQueue.Enqueue(UguiSource);

            _isInstalling = _installQueue.Count > 0;
            _statusLine = "Installing dependencies (VContainer, UniTask, NuGetForUnity, Addressables, Localization, InputSystem, UGUI)...";
        }

        private void InstallVContainer()
        {
            EnqueueSingleInstall(VContainerSource, "Installing VContainer...");
        }

        private void InstallUniTask()
        {
            EnqueueSingleInstall(UniTaskSource, "Installing UniTask...");
        }

        private void InstallNuGetForUnity()
        {
            EnqueueSingleInstall(NuGetForUnitySource, "Installing NuGetForUnity...");
        }

        private void InstallRuntimePackage()
        {
            _installQueue.Enqueue($"{RuntimeGitUrl}#{RuntimeGitTag}");
            _isInstalling = true;
            _statusLine = $"Installing runtime package from git tag {RuntimeGitTag}...";
        }

        private void InstallR3FromGit()
        {
            _installQueue.Enqueue(R3GitUrl);
            _isInstalling = true;
            _statusLine = "Installing R3 from Git URL...";
        }

        private void EnqueueSingleInstall(string source, string status)
        {
            if (_isInstalling)
            {
                return;
            }

            _installQueue.Enqueue(source);
            _isInstalling = true;
            _statusLine = status;
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
                    _isInstalling = true;
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
                _statusLine = $"Installing package: {source}";
                _isInstalling = true;
                _addRequest = Client.Add(source);
                Repaint();
                return;
            }

            if (_addRequest == null && _installQueue.Count == 0)
            {
                _isInstalling = false;
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
                _vContainerInstalled = HasAnyPackage(names, "jp.hadashikick.vcontainer", "vcontainer");
                _uniTaskInstalled = HasAnyPackage(names, "com.cysharp.unitask", "unitask");
                _nuGetForUnityInstalled = HasAnyPackage(names, "com.github-glitchenzo.nugetforunity", "com.github-glitchenzo.nuget-for-unity", "nugetforunity");
                _addressablesInstalled = HasAnyPackage(names, "com.unity.addressables", "addressables");
                _localizationInstalled = HasAnyPackage(names, "com.unity.localization", "localization");
                _inputSystemInstalled = HasAnyPackage(names, "com.unity.inputsystem", "inputsystem");
                _uguiInstalled = HasAnyPackage(names, "com.unity.ugui", "ugui");
                _dependenciesInstalled = _vContainerInstalled &&
                                         _uniTaskInstalled &&
                                         _nuGetForUnityInstalled &&
                                         _addressablesInstalled &&
                                         _localizationInstalled &&
                                         _inputSystemInstalled &&
                                         _uguiInstalled;
                _runtimeInstalled = names.Contains(RuntimePackageName);
                _r3Ready = IsAssemblyLoaded("R3");
                _observableCollectionsReady = IsAssemblyLoaded("ObservableCollections");
            }

            _structureReady = HasProjectStructure();
            _isRefreshingState = false;
            Repaint();
        }

        private void DrawProgress()
        {
            if (!_isInstalling && _addRequest == null && _installQueue.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Processing...", EditorStyles.boldLabel);
            var label = _addRequest != null
                ? "Installing package..."
                : _installQueue.Count > 0
                    ? $"Queued packages: {_installQueue.Count}"
                    : "Finalizing...";
            EditorGUILayout.HelpBox(label, MessageType.None);
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
                $"Before steps 4 and 5 install missing reactive libraries: {string.Join(", ", missing)}.\n" +
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

        private static bool HasAnyPackage(HashSet<string> names, params string[] candidates)
        {
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (names.Contains(candidate))
                {
                    return true;
                }

                foreach (var name in names)
                {
                    if (name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
#endif
