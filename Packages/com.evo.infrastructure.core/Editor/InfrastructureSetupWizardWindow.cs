#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Evo.Infrastructure.Core.Editor
{
    public sealed class InfrastructureSetupWizardWindow : EditorWindow
    {
        private const string RuntimePackageName = "com.evo.infrastructure.runtime";
        private const string RuntimeGitTag = "v0.3.23";
        private const string RuntimeGitUrl = "https://github.com/illiden228/EvoInfrastructure.git?path=Packages/com.evo.infrastructure.runtime";
        private const string R3NuGetId = "R3";
        private const string R3NuGetVersion = "1.3.0";
        private const string ObservableCollectionsNuGetId = "ObservableCollections";
        private const string ObservableCollectionsNuGetVersion = "3.3.4";
        private const string BclAsyncInterfacesNuGetId = "Microsoft.Bcl.AsyncInterfaces";
        private const string BclAsyncInterfacesNuGetVersion = "6.0.0";
        private const string BclTimeProviderNuGetId = "Microsoft.Bcl.TimeProvider";
        private const string BclTimeProviderNuGetVersion = "8.0.0";
        private const string ComponentAnnotationsNuGetId = "System.ComponentModel.Annotations";
        private const string ComponentAnnotationsNuGetVersion = "5.0.0";
        private const string ThreadingChannelsNuGetId = "System.Threading.Channels";
        private const string ThreadingChannelsNuGetVersion = "8.0.0";
        private const string EntryScenePath = "Assets/_Project/Scenes/EntryPointScene.unity";
        private const string LoadingScenePath = "Assets/_Project/Scenes/LoadingScene.unity";
        private const string TransitionScenePath = "Assets/_Project/Scenes/TransitionScene.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
        private const string ProjectConfigPath = "Assets/_Project/Configs/ProjectConfig.asset";
        private const string UiSystemConfigPath = "Assets/_Project/Configs/UiSystemConfig.asset";
        private const string ConfigCatalogPath = "Assets/_Project/Configs/ScriptableConfigCatalog.asset";
        private const string LifetimeScopePrefabPath = "Assets/_Project/Prefabs/Runtime/InfrastructureProjectLifetimeScope.prefab";
        private const string StarterRuntimeProjectLifetimeScopePath = "Assets/_Project/Scripts/Runtime/EntryPoint/RuntimeProjectLifetimeScope.cs";
        private const string StarterRuntimeEntryPointPath = "Assets/_Project/Scripts/Runtime/EntryPoint/RuntimeEntryPoint.cs";
        private const string StarterLoadingSceneLifetimeScopePath = "Assets/_Project/Scripts/Runtime/Loading/LoadingSceneLifetimeScope.cs";
        private const string ProjectScopeTypeName = "RuntimeProjectLifetimeScope";

        private const string VContainerSource = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer";
        private const string UniTaskSource = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask";
        private const string NuGetForUnitySource = "https://github.com/GlitchEnzo/NuGetForUnity.git?path=src/NuGetForUnity";
        private const string AddressablesSource = "com.unity.addressables@2.8.1";
        private const string LocalizationSource = "com.unity.localization@1.5.9";
        private const string InputSystemSource = "com.unity.inputsystem@1.7.0";
        private const string UguiSource = "com.unity.ugui@2.0.0";
        private const string PrimeTweenSource = "com.kyrylokuzyk.primetween@1.3.8";
        private const string PrimeTweenPackageName = "com.kyrylokuzyk.primetween";
        private const string PrimeTweenScope = "com.kyrylokuzyk";
        private const string NpmRegistryUrl = "https://registry.npmjs.org";

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
            "Assets/_Project/Scripts/Runtime/EntryPoint",
            "Assets/_Project/Scripts/Runtime/Loading"
        };

        private readonly Queue<string> _installQueue = new();
        private AddRequest _addRequest;
        private ListRequest _listRequest;
        private bool _dependenciesInstalled;
        private bool _runtimeInstalled;
        private bool _structureReady;
        private bool _bootstrapScopesReady;
        private bool _r3Ready;
        private bool _observableCollectionsReady;
        private bool _r3InPackagesConfig;
        private bool _observableCollectionsInPackagesConfig;
        private bool _vContainerInstalled;
        private bool _uniTaskInstalled;
        private bool _nuGetForUnityInstalled;
        private bool _addressablesInstalled;
        private bool _localizationInstalled;
        private bool _inputSystemInstalled;
        private bool _uguiInstalled;
        private bool _primeTweenInstalled;
        private bool _isRefreshingState;
        private bool _isInstalling;
        private double _refreshStartedAt;
        private string _statusLine = "Ready";
        private Vector2 _scroll;

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
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            DrawProgress();
            DrawState();
            GUILayout.Space(10f);
            DrawActions();
            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(_statusLine, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Evo Infrastructure Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Step-by-step project bootstrap.", EditorStyles.wordWrappedLabel);
        }

        private void DrawState()
        {
            EditorGUILayout.Space(8f);
            DrawStatusRow("VContainer installed", _vContainerInstalled);
            DrawStatusRow("UniTask installed", _uniTaskInstalled);
            DrawStatusRow("NuGetForUnity installed", _nuGetForUnityInstalled);
            DrawStatusRow("Addressables installed", _addressablesInstalled);
            DrawStatusRow("Localization installed", _localizationInstalled);
            DrawStatusRow("Input System installed", _inputSystemInstalled);
            DrawStatusRow("UGUI installed", _uguiInstalled);
            DrawStatusRow("PrimeTween installed", _primeTweenInstalled);
            DrawStatusRow("R3 installed", _r3Ready);
            DrawStatusRow("ObservableCollections installed", _observableCollectionsReady);
            DrawStatusRow("Project structure created", _structureReady);
            DrawStatusRow("Infrastructure runtime installed", _runtimeInstalled);
            DrawStatusRow("Starter runtime scaffold ready", HasStarterScaffold());
            DrawStatusRow("Bootstrap scopes valid", _bootstrapScopesReady);
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
            var canInstallDeps = !_isInstalling;

            DrawActionButton(
                _vContainerInstalled ? "1) Install VContainer (Already done)" : "1) Install VContainer",
                _vContainerInstalled
                    ? "VContainer is already installed."
                    : "Install VContainer from Git URL.",
                !_vContainerInstalled && canInstallDeps,
                InstallVContainer);

            DrawActionButton(
                _uniTaskInstalled ? "2) Install UniTask (Already done)" : "2) Install UniTask",
                _uniTaskInstalled
                    ? "UniTask is already installed."
                    : "Install UniTask from Git URL.",
                !_uniTaskInstalled && canInstallDeps,
                InstallUniTask);

            DrawActionButton(
                _nuGetForUnityInstalled ? "3) Install NuGetForUnity (Already done)" : "3) Install NuGetForUnity",
                _nuGetForUnityInstalled
                    ? "NuGetForUnity is already installed."
                    : "Install NuGetForUnity from Git URL.",
                !_nuGetForUnityInstalled && canInstallDeps,
                InstallNuGetForUnity);

            DrawActionButton(
                _addressablesInstalled ? "4) Install Addressables (Already done)" : "4) Install Addressables",
                _addressablesInstalled
                    ? "Addressables is already installed."
                    : "Install Unity Addressables package.",
                !_addressablesInstalled && canInstallDeps,
                InstallAddressables);

            DrawActionButton(
                _localizationInstalled ? "5) Install Localization (Already done)" : "5) Install Localization",
                _localizationInstalled
                    ? "Localization is already installed."
                    : "Install Unity Localization package.",
                !_localizationInstalled && canInstallDeps,
                InstallLocalization);

            DrawActionButton(
                _inputSystemInstalled ? "6) Install Input System (Already done)" : "6) Install Input System",
                _inputSystemInstalled
                    ? "Input System is already installed."
                    : "Install Unity Input System package.",
                !_inputSystemInstalled && canInstallDeps,
                InstallInputSystem);

            DrawActionButton(
                _uguiInstalled ? "7) Install UGUI (Already done)" : "7) Install UGUI",
                _uguiInstalled
                    ? "UGUI is already installed."
                    : "Install Unity UGUI package.",
                !_uguiInstalled && canInstallDeps,
                InstallUgui);

            DrawActionButton(
                _primeTweenInstalled ? "8) Install PrimeTween (Already done)" : "8) Install PrimeTween",
                _primeTweenInstalled
                    ? "PrimeTween is already installed."
                    : "Install PrimeTween and ensure scoped registry.",
                !_primeTweenInstalled && canInstallDeps,
                InstallPrimeTween);

            var structureDone = _structureReady;
            DrawActionButton(
                structureDone ? "9) Create Project Structure (Already done)" : "9) Create Project Structure",
                structureDone
                    ? "Project folder structure is already ready."
                    : "Create base folders under Assets/_Project.",
                !structureDone,
                CreateProjectStructure);

            var reactiveRequested = _r3InPackagesConfig && _observableCollectionsInPackagesConfig;
            var r3Done = _r3Ready && _observableCollectionsReady;
            var canInstallR3 = _nuGetForUnityInstalled && !reactiveRequested && !r3Done && !_isInstalling;
            DrawActionButton(
                r3Done || reactiveRequested ? "10) Install R3 + ObservableCollections (NuGet) (Already done)" : "10) Install R3 + ObservableCollections (NuGet)",
                r3Done
                    ? "R3 and ObservableCollections are installed."
                    : reactiveRequested
                        ? "R3 and ObservableCollections are already requested in packages.config."
                    : canInstallR3
                        ? "Add R3 and ObservableCollections to packages.config for NuGetForUnity restore."
                        : "Requires: NuGetForUnity installed.",
                canInstallR3,
                InstallReactiveFromNuGet);

            var runtimeDone = _runtimeInstalled;
            var canInstallRuntime = _dependenciesInstalled && _primeTweenInstalled && _r3Ready && _observableCollectionsReady && !runtimeDone && !_isInstalling;
            DrawActionButton(
                runtimeDone ? "11) Install Infrastructure Runtime (Already done)" : "11) Install Infrastructure Runtime",
                runtimeDone
                    ? "Infrastructure runtime is already installed."
                    : canInstallRuntime
                        ? "Install runtime package from Git tag."
                        : "Requires: base dependencies + PrimeTween + R3 + ObservableCollections.",
                canInstallRuntime,
                InstallRuntimePackage);

            var scaffoldDone = HasStarterScaffold();
            var canSetupScaffold = _dependenciesInstalled && _primeTweenInstalled && _r3Ready && _observableCollectionsReady && _runtimeInstalled && !scaffoldDone && !_isInstalling;
            DrawActionButton(
                scaffoldDone ? "12) Setup Starter Runtime Scaffold (Already done)" : "12) Setup Starter Runtime Scaffold",
                scaffoldDone
                    ? "Starter runtime scaffold is already created."
                    : canSetupScaffold
                        ? "Create starter scenes, configs and build settings."
                        : "Requires: Step 11 (Infrastructure Runtime installed).",
                canSetupScaffold,
                SetupStarterRuntimeScaffold);

            DrawActionButton(
                "Refresh State",
                "Re-check installed packages and setup status.",
                true,
                RefreshState,
                26f);

            var canValidateBootstrapScopes = !_isInstalling && HasStarterScaffold();
            DrawActionButton(
                "Validate/Fix Bootstrap Scopes",
                canValidateBootstrapScopes
                    ? "Validate EntryPoint/MainMenu/Loading scopes and auto-fix missing parent scope links."
                    : "Requires starter scaffold scenes/scripts.",
                canValidateBootstrapScopes,
                ValidateAndFixBootstrapScopes);

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

        private void InstallAddressables()
        {
            EnqueueSingleInstall(AddressablesSource, "Installing Addressables...");
        }

        private void InstallLocalization()
        {
            EnqueueSingleInstall(LocalizationSource, "Installing Localization...");
        }

        private void InstallInputSystem()
        {
            EnqueueSingleInstall(InputSystemSource, "Installing Input System...");
        }

        private void InstallUgui()
        {
            EnqueueSingleInstall(UguiSource, "Installing UGUI...");
        }

        private void InstallPrimeTween()
        {
            if (!EnsurePrimeTweenScopedRegistry())
            {
                _statusLine = "Failed to add scoped registry for PrimeTween in Packages/manifest.json.";
                return;
            }

            EnqueueSingleInstall(PrimeTweenSource, "Installing PrimeTween...");
        }

        private void InstallRuntimePackage()
        {
            _installQueue.Enqueue($"{RuntimeGitUrl}#{RuntimeGitTag}");
            _isInstalling = true;
            _statusLine = $"Installing runtime package from git tag {RuntimeGitTag}...";
        }

        private void InstallReactiveFromNuGet()
        {
            if (!_nuGetForUnityInstalled)
            {
                _statusLine = "NuGetForUnity is not installed.";
                return;
            }

            var packagesConfigPath = GetNuGetPackagesConfigPath();
            var document = LoadOrCreatePackagesConfig(packagesConfigPath);
            var root = document.Root;
            if (root == null)
            {
                root = new XElement("packages");
                document.Add(root);
            }

            EnsureNuGetPackage(root, R3NuGetId, R3NuGetVersion);
            EnsureNuGetPackage(root, ObservableCollectionsNuGetId, ObservableCollectionsNuGetVersion);
            EnsureNuGetPackage(root, BclAsyncInterfacesNuGetId, BclAsyncInterfacesNuGetVersion);
            EnsureNuGetPackage(root, BclTimeProviderNuGetId, BclTimeProviderNuGetVersion);
            EnsureNuGetPackage(root, ComponentAnnotationsNuGetId, ComponentAnnotationsNuGetVersion);
            EnsureNuGetPackage(root, ThreadingChannelsNuGetId, ThreadingChannelsNuGetVersion);
            document.Save(packagesConfigPath);

            AssetDatabase.Refresh();
            TryInvokeNuGetRestore();
            _statusLine = "Added R3 and ObservableCollections to packages.config. Waiting for NuGet restore/import...";
            RefreshState();
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
            EnsureStarterScripts();
            AssetDatabase.Refresh();
            EnsureDefaultAssets();
            ConfigureStarterScenes();
            EnsureMainMenuInAddressables();
            ConfigureProjectConfigForStarterPipeline();
            EnsureBuildScenes();
            ValidateAndFixBootstrapScopes();
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

            var type = FindTypeByName("_Project.Scripts.Runtime.EntryPoint.RuntimeProjectLifetimeScope") ??
                       FindTypeByName("_Project.Scripts.Runtime.Bootstrap.RuntimeProjectLifetimeScope");
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
                TransitionScenePath
            };

            var current = new List<EditorBuildSettingsScene>();
            for (var i = 0; i < required.Length; i++)
            {
                var path = required[i];
                if (!File.Exists(path))
                {
                    continue;
                }

                current.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = current.ToArray();
        }

        private void ConfigureStarterScenes()
        {
            ConfigureEntryPointScene();
            ConfigureLoadingScene();
            ConfigureMainMenuScene();
        }

        private void ConfigureEntryPointScene()
        {
            if (!File.Exists(EntryScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Single);
            var root = GetOrCreateRoot(scene, "EntryPointRoot", null);
            RemoveCanvasObjects(scene);
            AddAnyProjectLifetimeScope(root);
            EditorSceneManager.SaveScene(scene);
        }

        private void ConfigureLoadingScene()
        {
            if (!File.Exists(LoadingScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
            DeleteRootByName(scene, "LoadingRoot");
            EnsureCanvasRoot(scene);
            EnsureSceneScopeParent(
                scene,
                "Context",
                null,
                "_Project.Scripts.Runtime.Loading.LoadingSceneLifetimeScope",
                "SceneLifetimeScope");
            EditorSceneManager.SaveScene(scene);
        }

        private void ConfigureMainMenuScene()
        {
            if (!File.Exists(MenuScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var context = GetOrCreateRoot(scene, "Context", "MainMenuRoot");
            var scope = GetOrAddScopeComponent(
                context,
                "_Project.Scripts.Runtime.MainMenu.MainMenuSceneLifetimeScope",
                "SceneLifetimeScope");
            EnsureParentReferenceTypeName(scope, ProjectScopeTypeName);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name, string legacyName)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (string.Equals(root.name, name, StringComparison.Ordinal))
                {
                    return root;
                }

                if (!string.IsNullOrEmpty(legacyName) && string.Equals(root.name, legacyName, StringComparison.Ordinal))
                {
                    root.name = name;
                    return root;
                }
            }

            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static void DeleteRootByName(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    DestroyImmediate(roots[i]);
                }
            }
        }

        private static void EnsureCanvasRoot(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var canvas = root.GetComponent<Canvas>();
                if (canvas == null)
                {
                    continue;
                }

                if (!string.Equals(root.name, "Canvas", StringComparison.Ordinal))
                {
                    root.name = "Canvas";
                }

                EnsureCanvasSetup(root, canvas);
                return;
            }

            var canvasRoot = new GameObject("Canvas");
            var createdCanvas = canvasRoot.AddComponent<Canvas>();
            EnsureCanvasSetup(canvasRoot, createdCanvas);
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
        }

        private static void EnsureCanvasSetup(GameObject root, Canvas canvas)
        {
            if (root == null || canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = root.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                DestroyImmediate(raycaster);
            }
        }

        private static void RemoveCanvasObjects(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (root.GetComponent<Canvas>() != null || string.Equals(root.name, "Canvas", StringComparison.Ordinal))
                {
                    DestroyImmediate(root);
                }
            }
        }

        private static void AddComponentIfMissing(GameObject target, string typeName)
        {
            if (target == null || string.IsNullOrWhiteSpace(typeName))
            {
                return;
            }

            var type = FindTypeByName(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return;
            }

            if (target.GetComponent(type) == null)
            {
                target.AddComponent(type);
            }
        }

        private static Component GetOrAddScopeComponent(GameObject target, params string[] preferredTypeNames)
        {
            if (target == null || preferredTypeNames == null || preferredTypeNames.Length == 0)
            {
                return null;
            }

            var existingComponents = target.GetComponents<Component>();
            for (var i = 0; i < existingComponents.Length; i++)
            {
                var component = existingComponents[i];
                if (component == null)
                {
                    continue;
                }

                var typeName = component.GetType().Name;
                if (!typeName.EndsWith("LifetimeScope", StringComparison.Ordinal))
                {
                    continue;
                }

                return component;
            }

            for (var i = 0; i < preferredTypeNames.Length; i++)
            {
                var preferredTypeName = preferredTypeNames[i];
                if (string.IsNullOrWhiteSpace(preferredTypeName))
                {
                    continue;
                }

                var type = FindTypeByName(preferredTypeName);
                if (type == null || !typeof(Component).IsAssignableFrom(type))
                {
                    continue;
                }

                var existing = target.GetComponent(type);
                if (existing != null)
                {
                    return existing;
                }

                return target.AddComponent(type);
            }

            return null;
        }

        private static bool EnsureParentReferenceTypeName(Component scope, string parentTypeName)
        {
            if (scope == null || string.IsNullOrWhiteSpace(parentTypeName))
            {
                return false;
            }

            var serialized = new SerializedObject(scope);
            var parentReference = serialized.FindProperty("parentReference");
            if (parentReference == null)
            {
                return false;
            }

            var typeName = parentReference.FindPropertyRelative("TypeName");
            if (typeName == null || string.Equals(typeName.stringValue, parentTypeName, StringComparison.Ordinal))
            {
                return false;
            }

            typeName.stringValue = parentTypeName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);
            return true;
        }

        private static bool EnsureSceneScopeParent(
            Scene scene,
            string rootName,
            string legacyRootName,
            params string[] preferredScopeTypeNames)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            var changed = false;
            var root = GetOrCreateRoot(scene, rootName, legacyRootName);
            var scope = GetOrAddScopeComponent(root, preferredScopeTypeNames);
            if (scope != null && EnsureParentReferenceTypeName(scope, ProjectScopeTypeName))
            {
                changed = true;
            }

            return changed;
        }

        private void ValidateAndFixBootstrapScopes()
        {
            var fixedItems = new List<string>();
            var issues = new List<string>();

            ValidateAndFixEntryPointScope(fixedItems, issues);
            ValidateAndFixSceneScope(
                MenuScenePath,
                "Context",
                "MainMenuRoot",
                fixedItems,
                issues,
                "_Project.Scripts.Runtime.MainMenu.MainMenuSceneLifetimeScope",
                "SceneLifetimeScope");
            ValidateAndFixSceneScope(
                LoadingScenePath,
                "Context",
                "LoadingRoot",
                fixedItems,
                issues,
                "_Project.Scripts.Runtime.Loading.LoadingSceneLifetimeScope",
                "SceneLifetimeScope");

            if (issues.Count == 0)
            {
                _statusLine = fixedItems.Count == 0
                    ? "Bootstrap scope validation passed. No changes required."
                    : $"Bootstrap scope validation passed. Auto-fixed: {string.Join(", ", fixedItems)}.";
            }
            else
            {
                _statusLine = "Bootstrap scope validation found issues: " + string.Join(" | ", issues);
            }

            RefreshState();
        }

        private static void ValidateAndFixEntryPointScope(ICollection<string> fixedItems, ICollection<string> issues)
        {
            if (!File.Exists(EntryScenePath))
            {
                issues.Add("EntryPoint scene is missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Single);
            var root = GetOrCreateRoot(scene, "EntryPointRoot", null);
            var projectScope = root.GetComponent(ProjectScopeTypeName);
            if (projectScope == null)
            {
                AddAnyProjectLifetimeScope(root);
                projectScope = root.GetComponent(ProjectScopeTypeName);
                if (projectScope != null)
                {
                    fixedItems.Add("EntryPoint project scope");
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (projectScope == null)
            {
                issues.Add("EntryPointRoot does not contain RuntimeProjectLifetimeScope.");
            }
        }

        private static void ValidateAndFixSceneScope(
            string scenePath,
            string rootName,
            string legacyRootName,
            ICollection<string> fixedItems,
            ICollection<string> issues,
            params string[] preferredScopeTypeNames)
        {
            if (!File.Exists(scenePath))
            {
                issues.Add($"{Path.GetFileNameWithoutExtension(scenePath)} scene is missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var changed = EnsureSceneScopeParent(scene, rootName, legacyRootName, preferredScopeTypeNames);
            if (changed)
            {
                fixedItems.Add(Path.GetFileNameWithoutExtension(scenePath));
                EditorSceneManager.SaveScene(scene);
            }

            var context = GetOrCreateRoot(scene, rootName, legacyRootName);
            var scope = GetOrAddScopeComponent(context, preferredScopeTypeNames);
            if (scope == null)
            {
                issues.Add($"{Path.GetFileNameWithoutExtension(scenePath)} has no scene lifetime scope.");
                return;
            }

            var serialized = new SerializedObject(scope);
            var typeName = serialized.FindProperty("parentReference")?.FindPropertyRelative("TypeName");
            if (typeName == null || !string.Equals(typeName.stringValue, ProjectScopeTypeName, StringComparison.Ordinal))
            {
                issues.Add($"{Path.GetFileNameWithoutExtension(scenePath)} scope parent is not '{ProjectScopeTypeName}'.");
            }
        }

        private static void AddAnyProjectLifetimeScope(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (target.GetComponent("RuntimeProjectLifetimeScope") != null)
            {
                return;
            }

            if (TryAddComponentByTypeName(target, "_Project.Scripts.Runtime.EntryPoint.RuntimeProjectLifetimeScope"))
            {
                return;
            }

            TryAddComponentByTypeName(target, "_Project.Scripts.Runtime.Bootstrap.RuntimeProjectLifetimeScope");
        }

        private static bool TryAddComponentByTypeName(GameObject target, string typeName)
        {
            var type = FindTypeByName(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return false;
            }

            if (target.GetComponent(type) == null)
            {
                target.AddComponent(type);
            }

            return true;
        }

        private static void EnsureMainMenuInAddressables()
        {
            if (!File.Exists(MenuScenePath))
            {
                return;
            }

            var sceneGuid = AssetDatabase.AssetPathToGUID(MenuScenePath);
            if (string.IsNullOrEmpty(sceneGuid))
            {
                return;
            }

            var settingsType = Type.GetType(
                "UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            if (settingsType == null)
            {
                return;
            }

            var settingsProperty = settingsType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Static);
            var settings = settingsProperty?.GetValue(null);
            if (settings == null)
            {
                var getSettingsMethod = settingsType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (!string.Equals(m.Name, "GetSettings", StringComparison.Ordinal))
                        {
                            return false;
                        }

                        var p = m.GetParameters();
                        return p.Length == 1 && p[0].ParameterType == typeof(bool);
                    });

                settings = getSettingsMethod?.Invoke(null, new object[] { true });
            }
            if (settings == null)
            {
                return;
            }

            var defaultGroupProperty = settings.GetType().GetProperty("DefaultGroup", BindingFlags.Public | BindingFlags.Instance);
            var defaultGroup = defaultGroupProperty?.GetValue(settings);
            if (defaultGroup == null)
            {
                var groupsProperty = settings.GetType().GetProperty("groups", BindingFlags.Public | BindingFlags.Instance);
                var groups = groupsProperty?.GetValue(settings) as System.Collections.IEnumerable;
                if (groups != null)
                {
                    foreach (var group in groups)
                    {
                        if (group != null)
                        {
                            defaultGroup = group;
                            break;
                        }
                    }
                }
            }

            if (defaultGroup == null)
            {
                return;
            }

            var createOrMoveEntryMethod = settings.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "CreateOrMoveEntry", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var p = m.GetParameters();
                    return p.Length >= 2 &&
                           p[0].ParameterType == typeof(string) &&
                           p[1].ParameterType.IsInstanceOfType(defaultGroup);
                });

            if (createOrMoveEntryMethod == null)
            {
                return;
            }

            var parameters = createOrMoveEntryMethod.GetParameters();
            object[] args;
            if (parameters.Length == 2)
            {
                args = new object[] { sceneGuid, defaultGroup };
            }
            else if (parameters.Length == 3)
            {
                args = new object[] { sceneGuid, defaultGroup, false };
            }
            else
            {
                args = new object[] { sceneGuid, defaultGroup, false, false };
            }

            var entry = createOrMoveEntryMethod.Invoke(settings, args);
            if (entry == null)
            {
                return;
            }

            var addressProperty = entry.GetType().GetProperty("address", BindingFlags.Public | BindingFlags.Instance);
            if (addressProperty != null && addressProperty.CanWrite)
            {
                addressProperty.SetValue(entry, "MainMenuScene");
            }

            EditorUtility.SetDirty((UnityEngine.Object)settings);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureProjectConfigForStarterPipeline()
        {
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ProjectConfigPath);
            if (config == null)
            {
                return;
            }

            var startupGuid = AssetDatabase.AssetPathToGUID(MenuScenePath);
            var loadingGuid = AssetDatabase.AssetPathToGUID(LoadingScenePath);
            if (string.IsNullOrEmpty(startupGuid) || string.IsNullOrEmpty(loadingGuid))
            {
                return;
            }

            var so = new SerializedObject(config);
            SetAssetReferenceGuid(so.FindProperty("startupScene"), startupGuid);
            SetAssetReferenceGuid(so.FindProperty("gameplayScene"), startupGuid);
            SetAssetReferenceGuid(so.FindProperty("loadingScene"), loadingGuid);
            var transitionName = so.FindProperty("transitionSceneName");
            if (transitionName != null)
            {
                transitionName.stringValue = Path.GetFileNameWithoutExtension(TransitionScenePath);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static void SetAssetReferenceGuid(SerializedProperty property, string guid)
        {
            if (property == null || string.IsNullOrEmpty(guid))
            {
                return;
            }

            var guidProperty = property.FindPropertyRelative("m_AssetGUID");
            if (guidProperty != null)
            {
                guidProperty.stringValue = guid;
            }
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
                QueueRefreshBurst();
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
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                _statusLine = "Waiting for Unity to finish compile/update before refresh...";
                EditorApplication.delayCall += RefreshState;
                return;
            }

            if (_isRefreshingState)
            {
                var elapsed = EditorApplication.timeSinceStartup - _refreshStartedAt;
                if (elapsed <= 10d)
                {
                    return;
                }

                // Recover from stale/hanging PackageManager request.
                EditorApplication.update -= WaitForList;
                _isRefreshingState = false;
                _listRequest = null;
                _statusLine = "Recovered from stale package state request. Retrying...";
            }

            if (_isRefreshingState)
            {
                return;
            }

            _isRefreshingState = true;
            _refreshStartedAt = EditorApplication.timeSinceStartup;
            _listRequest = Client.List(false, false);
            EditorApplication.update += WaitForList;
        }

        private void WaitForList()
        {
            if (_listRequest == null)
            {
                _isRefreshingState = false;
                EditorApplication.update -= WaitForList;
                return;
            }

            if (!_listRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= WaitForList;
            if (_listRequest.Status == StatusCode.Success)
            {
                var packages = _listRequest.Result.ToList();
                _vContainerInstalled = HasAnyPackage(packages, "jp.hadashikick.vcontainer", "vcontainer") ||
                                       ManifestHasAnyDependency("jp.hadashikick.vcontainer");
                _uniTaskInstalled = HasAnyPackage(packages, "com.cysharp.unitask", "unitask") ||
                                    ManifestHasAnyDependency("com.cysharp.unitask");
                _nuGetForUnityInstalled = HasAnyPackage(packages, "com.github-glitchenzo.nugetforunity", "com.github-glitchenzo.nuget-for-unity", "nugetforunity") ||
                                          ManifestHasAnyDependency("com.github-glitchenzo.nugetforunity", "com.github-glitchenzo.nuget-for-unity");
                _addressablesInstalled = HasAnyPackage(packages, "com.unity.addressables", "addressables") ||
                                         ManifestHasAnyDependency("com.unity.addressables");
                _localizationInstalled = HasAnyPackage(packages, "com.unity.localization", "localization") ||
                                         ManifestHasAnyDependency("com.unity.localization");
                _inputSystemInstalled = HasAnyPackage(packages, "com.unity.inputsystem", "inputsystem") ||
                                        ManifestHasAnyDependency("com.unity.inputsystem");
                _uguiInstalled = HasAnyPackage(packages, "com.unity.ugui", "ugui") ||
                                 ManifestHasAnyDependency("com.unity.ugui");
                _primeTweenInstalled = HasAnyPackage(packages, PrimeTweenPackageName, "primetween") ||
                                       ManifestHasAnyDependency(PrimeTweenPackageName);
                _dependenciesInstalled = _vContainerInstalled &&
                                         _uniTaskInstalled &&
                                         _nuGetForUnityInstalled &&
                                         _addressablesInstalled &&
                                         _localizationInstalled &&
                                         _inputSystemInstalled &&
                                         _uguiInstalled;
                _runtimeInstalled = HasAnyPackage(packages, RuntimePackageName, "com.evo.infrastructure.runtime") ||
                                    ManifestHasAnyDependency(RuntimePackageName);
                ReadReactivePackagesConfig(out _r3InPackagesConfig, out _observableCollectionsInPackagesConfig);
                _r3Ready = _r3InPackagesConfig || IsAssemblyLoaded("R3");
                _observableCollectionsReady = _observableCollectionsInPackagesConfig || IsAssemblyLoaded("ObservableCollections");
            }

            _structureReady = HasProjectStructure();
            _bootstrapScopesReady = AreBootstrapScopesValid();
            _isRefreshingState = false;
            Repaint();
        }

        private static bool AreBootstrapScopesValid()
        {
            return HasEntryPointScope() &&
                   HasSceneScopeWithParent(MenuScenePath, "Context", "MainMenuRoot") &&
                   HasSceneScopeWithParent(LoadingScenePath, "Context", "LoadingRoot");
        }

        private static bool HasEntryPointScope()
        {
            if (!File.Exists(EntryScenePath))
            {
                return false;
            }

            var text = SafeReadAllText(EntryScenePath);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.IndexOf("m_Name: EntryPointRoot", StringComparison.Ordinal) >= 0 &&
                   text.IndexOf("RuntimeProjectLifetimeScope", StringComparison.Ordinal) >= 0;
        }

        private static bool HasSceneScopeWithParent(string scenePath, string rootName, string legacyRootName)
        {
            if (!File.Exists(scenePath))
            {
                return false;
            }

            var text = SafeReadAllText(scenePath);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var rootMarker = $"m_Name: {rootName}";
            var legacyMarker = string.IsNullOrWhiteSpace(legacyRootName) ? null : $"m_Name: {legacyRootName}";
            var rootExists = text.IndexOf(rootMarker, StringComparison.Ordinal) >= 0 ||
                             (!string.IsNullOrEmpty(legacyMarker) && text.IndexOf(legacyMarker, StringComparison.Ordinal) >= 0);
            if (!rootExists)
            {
                return false;
            }

            return text.IndexOf("TypeName: RuntimeProjectLifetimeScope", StringComparison.Ordinal) >= 0;
        }

        private static string SafeReadAllText(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch
            {
                return string.Empty;
            }
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
                   File.Exists(MenuScenePath) &&
                   File.Exists(StarterRuntimeProjectLifetimeScopePath) &&
                   File.Exists(StarterRuntimeEntryPointPath) &&
                   File.Exists(StarterLoadingSceneLifetimeScopePath);
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
                $"Install missing reactive libraries: {string.Join(", ", missing)}.\n" +
                "Use step 10 to add R3 and ObservableCollections to packages.config via NuGetForUnity.";
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

        private static bool ManifestHasAnyDependency(params string[] packageNames)
        {
            if (packageNames == null || packageNames.Length == 0)
            {
                return false;
            }

            var path = Path.Combine(GetProjectRootPath(), "Packages", "manifest.json");
            if (!File.Exists(path))
            {
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            for (var i = 0; i < packageNames.Length; i++)
            {
                var packageName = packageNames[i];
                if (string.IsNullOrWhiteSpace(packageName))
                {
                    continue;
                }

                if (json.IndexOf($"\"{packageName}\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyPackage(IReadOnlyList<UnityEditor.PackageManager.PackageInfo> packages, params string[] candidates)
        {
            if (packages == null || packages.Count == 0 || candidates == null || candidates.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < packages.Count; i++)
            {
                var package = packages[i];
                var name = package != null ? package.name : string.Empty;
                var packageId = package != null ? package.packageId : string.Empty;
                var resolvedPath = package != null ? package.resolvedPath : string.Empty;

                for (var j = 0; j < candidates.Length; j++)
                {
                    var candidate = candidates[j];
                    if (string.IsNullOrWhiteSpace(candidate))
                    {
                        continue;
                    }

                    if ((!string.IsNullOrEmpty(name) && name.Equals(candidate, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(name) && name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(packageId) && packageId.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(resolvedPath) && resolvedPath.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static XDocument LoadOrCreatePackagesConfig(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    return XDocument.Load(path);
                }
                catch
                {
                    // Fall through and recreate if file is malformed.
                }
            }

            return new XDocument(new XElement("packages"));
        }

        private static void EnsureNuGetPackage(XElement root, string id, string version)
        {
            if (root == null || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
            {
                return;
            }

            var existing = root.Elements("package")
                .FirstOrDefault(x => string.Equals((string)x.Attribute("id"), id, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                root.Add(new XElement("package",
                    new XAttribute("id", id),
                    new XAttribute("version", version),
                    new XAttribute("manuallyInstalled", "true")));
                return;
            }

            existing.SetAttributeValue("version", version);
            existing.SetAttributeValue("manuallyInstalled", "true");
        }

        private static void ReadReactivePackagesConfig(out bool hasR3, out bool hasObservableCollections)
        {
            hasR3 = false;
            hasObservableCollections = false;

            var path = GetNuGetPackagesConfigPath();
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                var document = XDocument.Load(path);
                var root = document.Root;
                if (root == null)
                {
                    return;
                }

                foreach (var package in root.Elements("package"))
                {
                    var id = (string)package.Attribute("id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (string.Equals(id, R3NuGetId, StringComparison.OrdinalIgnoreCase))
                    {
                        hasR3 = true;
                    }

                    if (string.Equals(id, ObservableCollectionsNuGetId, StringComparison.OrdinalIgnoreCase))
                    {
                        hasObservableCollections = true;
                    }
                }
            }
            catch
            {
                // Keep defaults as false.
            }
        }

        private static void TryInvokeNuGetRestore()
        {
            EditorApplication.delayCall += () =>
            {
                var submenus = Unsupported.GetSubmenus("NuGet");
                if (submenus == null || submenus.Length == 0)
                {
                    Debug.Log("[Evo Setup] NuGet menu not found. Run restore manually in NuGetForUnity if needed.");
                    return;
                }

                string restoreMenuPath = null;
                for (var i = 0; i < submenus.Length; i++)
                {
                    var menu = submenus[i];
                    if (string.IsNullOrWhiteSpace(menu))
                    {
                        continue;
                    }

                    if (menu.IndexOf("restore", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        restoreMenuPath = menu;
                        break;
                    }
                }

                var invoked = !string.IsNullOrWhiteSpace(restoreMenuPath) &&
                              EditorApplication.ExecuteMenuItem(restoreMenuPath);

                if (!invoked)
                {
                    Debug.Log("[Evo Setup] NuGet restore menu item not found. Run restore manually in NuGetForUnity if needed.");
                }
            };
        }

        private static string GetProjectRootPath()
        {
            var assetsPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(assetsPath))
            {
                return Directory.GetCurrentDirectory();
            }

            var root = Path.GetDirectoryName(assetsPath);
            return string.IsNullOrWhiteSpace(root) ? Directory.GetCurrentDirectory() : root;
        }

        private static string GetNuGetPackagesConfigPath()
        {
            var assetsPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(assetsPath))
            {
                return Path.Combine(GetProjectRootPath(), "Assets", "packages.config");
            }

            return Path.Combine(assetsPath, "packages.config");
        }

        private static bool EnsurePrimeTweenScopedRegistry()
        {
            var manifestPath = Path.Combine(GetProjectRootPath(), "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            var json = File.ReadAllText(manifestPath);
            if (json.IndexOf($"\"{PrimeTweenScope}\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var registryObject = BuildPrimeTweenScopedRegistryJson();
            if (json.IndexOf("\"scopedRegistries\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var inserted = InsertIntoScopedRegistriesArray(json, registryObject);
                if (inserted == null)
                {
                    return false;
                }

                File.WriteAllText(manifestPath, inserted);
                AssetDatabase.Refresh();
                return true;
            }

            var dependenciesIndex = json.IndexOf("\"dependencies\"", StringComparison.OrdinalIgnoreCase);
            if (dependenciesIndex < 0)
            {
                return false;
            }

            var block = $"  \"scopedRegistries\": [\n{registryObject}\n  ],\n";
            var withScoped = json.Insert(dependenciesIndex, block);
            File.WriteAllText(manifestPath, withScoped);
            AssetDatabase.Refresh();
            return true;
        }

        private static string InsertIntoScopedRegistriesArray(string json, string registryObject)
        {
            var keyIndex = json.IndexOf("\"scopedRegistries\"", StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
            {
                return null;
            }

            var arrayStart = json.IndexOf('[', keyIndex);
            if (arrayStart < 0)
            {
                return null;
            }

            var depth = 0;
            var arrayEnd = -1;
            for (var i = arrayStart; i < json.Length; i++)
            {
                var ch = json[i];
                if (ch == '[')
                {
                    depth++;
                }
                else if (ch == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        arrayEnd = i;
                        break;
                    }
                }
            }

            if (arrayEnd < 0)
            {
                return null;
            }

            var inner = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            var hasAny = !string.IsNullOrWhiteSpace(inner);
            var insertion = hasAny
                ? $"\n{registryObject},\n"
                : $"\n{registryObject}\n";
            return json.Insert(arrayStart + 1, insertion);
        }

        private static string BuildPrimeTweenScopedRegistryJson()
        {
            var sb = new StringBuilder();
            sb.Append("    {\n");
            sb.Append("      \"name\": \"npm\",\n");
            sb.Append($"      \"url\": \"{NpmRegistryUrl}\",\n");
            sb.Append("      \"scopes\": [\n");
            sb.Append($"        \"{PrimeTweenScope}\"\n");
            sb.Append("      ]\n");
            sb.Append("    }");
            return sb.ToString();
        }

        private static Type FindTypeByName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void EnsureStarterScripts()
        {
            EnsureStarterScript(StarterRuntimeProjectLifetimeScopePath, BuildRuntimeProjectLifetimeScopeStarterScript());
            EnsureStarterScript(StarterRuntimeEntryPointPath, BuildRuntimeEntryPointStarterScript());
            EnsureStarterScript(StarterLoadingSceneLifetimeScopePath, BuildLoadingSceneLifetimeScopeStarterScript());
        }

        private static void EnsureStarterScript(string path, string content)
        {
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllText(path, content);
        }

        private static string BuildRuntimeProjectLifetimeScopeStarterScript()
        {
            return
"using System;\n" +
"using System.Collections.Generic;\n" +
"using _Project.Scripts.Application.Loading;\n" +
"using _Project.Scripts.Application.UI;\n" +
"using _Project.Scripts.Infrastructure.Services.Audio;\n" +
"using _Project.Scripts.Infrastructure.Services.Config;\n" +
"using _Project.Scripts.Infrastructure.Services.Focus;\n" +
"using _Project.Scripts.Infrastructure.Services.Localization;\n" +
"using _Project.Scripts.Infrastructure.Services.PlatformInfo;\n" +
"using _Project.Scripts.Infrastructure.Services.ResourceCatalog;\n" +
"using _Project.Scripts.Infrastructure.Services.ResourceLoader;\n" +
"using _Project.Scripts.Infrastructure.Services.ResourceProvider;\n" +
"using _Project.Scripts.Infrastructure.Services.SceneLoader;\n" +
"using _Project.Scripts.Infrastructure.Services.ScenePayload;\n" +
"using _Project.Scripts.Infrastructure.Services.UI;\n" +
"using UnityEngine;\n" +
"using VContainer;\n" +
"using VContainer.Unity;\n" +
"\n" +
"namespace _Project.Scripts.Runtime.EntryPoint\n" +
"{\n" +
"    public sealed class RuntimeProjectLifetimeScope : LifetimeScope\n" +
"    {\n" +
"        [SerializeField] private ResourceCatalog resourceCatalog;\n" +
"        [SerializeField] private ScriptableConfigCatalog[] configCatalogs;\n" +
"        [SerializeField] private UiSystemConfig uiSystemConfig;\n" +
"\n" +
"        protected override void Awake()\n" +
"        {\n" +
"            base.Awake();\n" +
"            DontDestroyOnLoad(gameObject);\n" +
"        }\n" +
"\n" +
"        protected override void Configure(IContainerBuilder builder)\n" +
"        {\n" +
"            if (resourceCatalog != null)\n" +
"            {\n" +
"                builder.RegisterInstance<IResourceCatalog>(resourceCatalog);\n" +
"            }\n" +
"\n" +
"            builder.RegisterInstance<IReadOnlyList<ScriptableConfigCatalog>>(configCatalogs ?? Array.Empty<ScriptableConfigCatalog>());\n" +
"            builder.Register<IConfigProvider, ScriptableObjectConfigProvider>(Lifetime.Singleton);\n" +
"            builder.Register<IConfigService, ConfigService>(Lifetime.Singleton);\n" +
"\n" +
"            builder.Register<IPlatformInfoProvider, UnityPlatformInfoProvider>(Lifetime.Singleton);\n" +
"            builder.Register<IPlatformInfoService, PlatformInfoService>(Lifetime.Singleton);\n" +
"            builder.Register<IFocusService, FocusService>(Lifetime.Singleton);\n" +
"\n" +
"            builder.Register<IResourceLoaderService, AddressablesResourceLoaderService>(Lifetime.Singleton);\n" +
"            builder.Register<ISceneLoaderService, SceneLoaderService>(Lifetime.Singleton);\n" +
"            builder.Register<IResourceProviderService, ResourceProviderService>(Lifetime.Singleton);\n" +
"            builder.Register<IScenePayloadService, ScenePayloadService>(Lifetime.Singleton);\n" +
"            builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton);\n" +
"            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);\n" +
"\n" +
"            if (uiSystemConfig != null)\n" +
"            {\n" +
"                builder.RegisterInstance(uiSystemConfig);\n" +
"            }\n" +
"\n" +
"            builder.Register<IUiService, UiService>(Lifetime.Singleton);\n" +
"            builder.Register<ILoadingProgress, LoadingProgressReporter>(Lifetime.Singleton);\n" +
"            builder.Register<ISceneLoadingPipeline, SceneLoadingPipeline>(Lifetime.Singleton);\n" +
"            builder.Register<ILoadingStep, TargetFrameRateStep>(Lifetime.Singleton);\n" +
"            builder.RegisterEntryPoint<RuntimeEntryPoint>();\n" +
"        }\n" +
"    }\n" +
"}\n";
        }

        private static string BuildRuntimeEntryPointStarterScript()
        {
            return
"using System.Collections.Generic;\n" +
"using System.Threading;\n" +
"using _Project.Scripts.Application.Config;\n" +
"using _Project.Scripts.Application.Loading;\n" +
"using _Project.Scripts.Infrastructure.AddressablesExtension;\n" +
"using _Project.Scripts.Infrastructure.Services.Config;\n" +
"using Cysharp.Threading.Tasks;\n" +
"using UnityEngine.SceneManagement;\n" +
"using VContainer.Unity;\n" +
"\n" +
"namespace _Project.Scripts.Runtime.EntryPoint\n" +
"{\n" +
"    public sealed class RuntimeEntryPoint : IAsyncStartable\n" +
"    {\n" +
"        private readonly IReadOnlyList<ILoadingStep> _steps;\n" +
"        private readonly ILoadingProgress _progress;\n" +
"        private readonly LoadingRunner _runner;\n" +
"        private readonly ISceneLoadingPipeline _sceneLoadingPipeline;\n" +
"        private readonly IConfigService _configService;\n" +
"\n" +
"        public RuntimeEntryPoint(\n" +
"            IReadOnlyList<ILoadingStep> steps,\n" +
"            ISceneLoadingPipeline sceneLoadingPipeline,\n" +
"            IConfigService configService,\n" +
"            ILoadingProgress progress)\n" +
"        {\n" +
"            _steps = steps;\n" +
"            _progress = progress;\n" +
"            _runner = new LoadingRunner();\n" +
"            _sceneLoadingPipeline = sceneLoadingPipeline;\n" +
"            _configService = configService;\n" +
"        }\n" +
"\n" +
"        public async Cysharp.Threading.Tasks.UniTask StartAsync(CancellationToken cancellationToken)\n" +
"        {\n" +
"            var allSteps = new List<ILoadingStep>();\n" +
"            if (_steps != null)\n" +
"            {\n" +
"                allSteps.AddRange(_steps);\n" +
"            }\n" +
"\n" +
"            var startupScene = GetStartupScene();\n" +
"            if (startupScene != null && !string.IsNullOrEmpty(startupScene.AssetGUID) && _sceneLoadingPipeline != null)\n" +
"            {\n" +
"                allSteps.AddRange(_sceneLoadingPipeline.CreateSteps(startupScene, LoadSceneMode.Single));\n" +
"            }\n" +
"\n" +
"            await _runner.RunAsync(allSteps, _progress, cancellationToken);\n" +
"        }\n" +
"\n" +
"        private AssetReferenceScene GetStartupScene()\n" +
"        {\n" +
"            if (_configService != null && _configService.TryGet<ProjectConfig>(out var config))\n" +
"            {\n" +
"                return config?.StartupScene;\n" +
"            }\n" +
"\n" +
"            return null;\n" +
"        }\n" +
"    }\n" +
"}\n";
        }

        private static string BuildLoadingSceneLifetimeScopeStarterScript()
        {
            return
"using VContainer;\n" +
"using VContainer.Unity;\n" +
"\n" +
"namespace _Project.Scripts.Runtime.Loading\n" +
"{\n" +
"    public sealed class LoadingSceneLifetimeScope : LifetimeScope\n" +
"    {\n" +
"        protected override void Configure(IContainerBuilder builder)\n" +
"        {\n" +
"        }\n" +
"    }\n" +
"}\n";
        }

        private void QueueRefreshBurst()
        {
            EditorApplication.delayCall += RefreshState;
            EditorApplication.delayCall += () => EditorApplication.delayCall += RefreshState;
        }
    }
}
#endif
