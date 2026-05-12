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
        private const string YandexPackageName = "com.evo.infrastructure.yandex";
        private const string RuntimeGitTag = "v0.3.38";
        private const string YandexGitTag = "v0.3.38";
        private const string RuntimeGitUrl = "https://github.com/illiden228/EvoInfrastructure.git?path=Packages/com.evo.infrastructure.runtime";
        private const string YandexGitUrl = "https://github.com/illiden228/EvoInfrastructure.git?path=Packages/com.evo.infrastructure.yandex";
        private const string OdinPackagePathPrefsKey = "Evo.Infrastructure.Core.OdinPackagePath";
        private const string EmbeddedOdinPackageFolder = "Packages/com.evo.infrastructure.core/Editor/ThirdParty/Odin";
        private const string OdinPackageSearchPattern = "Odin*.unitypackage";
        private const string R3NuGetId = "R3";
        private const string R3NuGetVersion = "1.3.0";
        private const string ObservableCollectionsNuGetId = "ObservableCollections";
        private const string ObservableCollectionsNuGetVersion = "3.3.4";
        private const string ObservableCollectionsR3NuGetId = "ObservableCollections.R3";
        private const string ObservableCollectionsR3NuGetVersion = "3.3.4";
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
        private const string StarterProjectConfigPath = "Assets/_Project/Scripts/Runtime/Config/ProjectConfig.cs";
        private const string StarterLoadingPresenterPath = "Assets/_Project/Scripts/Runtime/Loading/ILoadingPresenter.cs";
        private const string StarterLoadingViewSystemPath = "Assets/_Project/Scripts/Runtime/Loading/LoadingViewSystem.cs";
        private const string TemplatesRootPath = "Packages/com.evo.infrastructure.core/Editor/Templates";
        private const string RuntimeProjectLifetimeScopeTemplateName = "RuntimeProjectLifetimeScope.cs.txt";
        private const string RuntimeEntryPointTemplateName = "RuntimeEntryPoint.cs.txt";
        private const string LoadingSceneLifetimeScopeTemplateName = "LoadingSceneLifetimeScope.cs.txt";
        private const string ProjectConfigTemplateName = "ProjectConfig.cs.txt";
        private const string LoadingPresenterTemplateName = "ILoadingPresenter.cs.txt";
        private const string LoadingViewSystemTemplateName = "LoadingViewSystem.cs.txt";
        private const string ProjectScopeTypeName = "RuntimeProjectLifetimeScope";
        private const string OneClickStateKeyPrefix = "Evo.Infrastructure.Core.OneClickSetup.";
        private static readonly StarterScriptTemplate[] StarterScriptTemplates =
        {
            new(StarterRuntimeProjectLifetimeScopePath, RuntimeProjectLifetimeScopeTemplateName),
            new(StarterRuntimeEntryPointPath, RuntimeEntryPointTemplateName),
            new(StarterLoadingSceneLifetimeScopePath, LoadingSceneLifetimeScopeTemplateName),
            new(StarterProjectConfigPath, ProjectConfigTemplateName),
            new(StarterLoadingPresenterPath, LoadingPresenterTemplateName),
            new(StarterLoadingViewSystemPath, LoadingViewSystemTemplateName)
        };

        private const string VContainerSource = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer";
        private const string UniTaskSource = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask";
        private const string NuGetForUnitySource = "https://github.com/GlitchEnzo/NuGetForUnity.git?path=src/NuGetForUnity";
        private const string AddressablesSource = "com.unity.addressables@2.8.1";
        private const string LocalizationSource = "com.unity.localization@1.5.9";
        private const string InputSystemSource = "com.unity.inputsystem@1.7.0";
        private const string UguiSource = "com.unity.ugui@2.0.0";
        private const string PrimeTweenSource = "com.kyrylokuzyk.primetween@1.3.8";
        private const string R3UnitySource = "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity";
        private const string PrimeTweenPackageName = "com.kyrylokuzyk.primetween";
        private const string R3UnityPackageName = "com.cysharp.r3";
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
            "Assets/_Project/Scripts/Runtime/Config",
            "Assets/_Project/Scripts/Runtime/EntryPoint",
            "Assets/_Project/Scripts/Runtime/Loading"
        };

        private readonly Queue<string> _installQueue = new();
        private AddRequest _addRequest;
        private AddAndRemoveRequest _addAndRemoveRequest;
        private ListRequest _listRequest;
        private bool _dependenciesInstalled;
        private bool _runtimeInstalled;
        private bool _yandexInstalled;
        private bool _odinInstalled;
        private bool _structureReady;
        private bool _bootstrapScopesReady;
        private bool _r3Ready;
        private bool _observableCollectionsReady;
        private bool _observableCollectionsR3Ready;
        private bool _r3UnityInstalled;
        private bool _r3InPackagesConfig;
        private bool _observableCollectionsInPackagesConfig;
        private bool _observableCollectionsR3InPackagesConfig;
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
        private bool _oneClickSetupRequested;
        private bool _scaffoldFinalizeQueued;
        private bool _installVContainer = true;
        private bool _installUniTask = true;
        private bool _installNuGetForUnity = true;
        private bool _installAddressables = true;
        private bool _installLocalization = true;
        private bool _installInputSystem = true;
        private bool _installUgui = true;
        private bool _installPrimeTween = true;
        private bool _installR3Unity = true;
        private bool _installReactiveNuGets = true;
        private bool _installRuntimeModule = true;
        private bool _installYandexModule = true;
        private bool _installOdinPackage;
        private bool _setupStarterScaffold = true;
        private bool _templatesReady;
        private bool _scaffoldScriptsUpToDate;
        private double _refreshStartedAt;
        private string _statusLine = "Ready";
        private Vector2 _scroll;
        private readonly List<string> _templateValidationIssues = new();

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
            ResumeOneClickSetupIfNeeded();
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateInstallQueue;
            EditorUtility.ClearProgressBar();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            DrawProgress();
            DrawState();
            GUILayout.Space(10f);
            DrawInstallPlan();
            GUILayout.Space(10f);
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
            DrawStatusRow("R3.Unity installed", _r3UnityInstalled);
            DrawStatusRow("R3 installed", _r3Ready);
            DrawStatusRow("ObservableCollections installed", _observableCollectionsReady);
            DrawStatusRow("ObservableCollections.R3 installed", _observableCollectionsR3Ready);
            DrawStatusRow("Project structure created", _structureReady);
            DrawStatusRow("Infrastructure runtime installed", _runtimeInstalled);
            DrawStatusRow("Infrastructure Yandex installed", _yandexInstalled);
            DrawStatusRow("Odin installed", _odinInstalled);
            DrawStatusRow("Starter runtime scaffold ready", HasStarterScaffold());
            DrawStatusRow("Scaffold templates valid", _templatesReady);
            DrawStatusRow("Scaffold scripts up to date", _scaffoldScriptsUpToDate);
            DrawStatusRow("Bootstrap scopes valid", _bootstrapScopesReady);

            if (_templateValidationIssues.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Template issues:\n" + string.Join("\n", _templateValidationIssues),
                    MessageType.Warning);
            }
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

        private void DrawInstallPlan()
        {
            EditorGUILayout.LabelField("Install Plan", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Analyze installed packages, choose modules, then run Setup. Installed items are checked and locked. Recommended items are selected by default.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(_isRefreshingState || _isInstalling || _oneClickSetupRequested))
            {
                DrawActionButton(
                    _isRefreshingState ? "Analyzing..." : "Analyze Installed Packages",
                    "Refresh package and scaffold state before setup.",
                    !_isRefreshingState && !_isInstalling && !_oneClickSetupRequested,
                    RefreshState,
                    26f);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Runtime Packages", EditorStyles.boldLabel);
            _installVContainer = DrawInstallPlanRow("VContainer", _installVContainer, _vContainerInstalled, "DI container for project and scene scopes.");
            _installUniTask = DrawInstallPlanRow("UniTask", _installUniTask, _uniTaskInstalled, "Async runtime used by loading and services.");
            _installNuGetForUnity = DrawInstallPlanRow("NuGetForUnity", _installNuGetForUnity, _nuGetForUnityInstalled, "Installs reactive NuGet packages.");
            _installAddressables = DrawInstallPlanRow("Addressables", _installAddressables, _addressablesInstalled, "Startup scene references are created as addressable assets.");
            _installLocalization = DrawInstallPlanRow("Unity Localization", _installLocalization, _localizationInstalled, "Runtime localization package.");
            _installInputSystem = DrawInstallPlanRow("Input System", _installInputSystem, _inputSystemInstalled, "Default input package for gameplay projects.");
            _installUgui = DrawInstallPlanRow("Unity UI", _installUgui, _uguiInstalled, "Base UI package for loading and menus.");
            _installPrimeTween = DrawInstallPlanRow("PrimeTween", _installPrimeTween, _primeTweenInstalled, "Tweening dependency.");
            _installR3Unity = DrawInstallPlanRow("R3.Unity", _installR3Unity, _r3UnityInstalled, "Reactive Unity integration.");
            _installReactiveNuGets = DrawInstallPlanRow("Reactive NuGets", _installReactiveNuGets, _r3Ready && _observableCollectionsReady && _observableCollectionsR3Ready, "R3, ObservableCollections and ObservableCollections.R3.");
            _installRuntimeModule = DrawInstallPlanRow("Evo Infrastructure Runtime", _installRuntimeModule, _runtimeInstalled, "Runtime framework package.");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Optional Modules", EditorStyles.boldLabel);
            _installYandexModule = DrawInstallPlanRow("Evo Infrastructure Yandex", _installYandexModule, _yandexInstalled, "YG2 integration package.");
            _installOdinPackage = DrawInstallPlanRow("Odin Inspector", _installOdinPackage, _odinInstalled, "Import from a selected .unitypackage source.");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Project Runtime", EditorStyles.boldLabel);
            _setupStarterScaffold = DrawInstallPlanRow("Starter scaffold", _setupStarterScaffold, HasStarterScaffold() && _bootstrapScopesReady, "Creates EntryPoint, loading flow, configs, scenes and Addressables entries.");

            using (new EditorGUI.DisabledScope(_isInstalling || _oneClickSetupRequested))
            {

                if (_installOdinPackage && !_odinInstalled)
                {
                    DrawOdinPackagePathField();
                }

                var canInstallSelected = !_isInstalling && !_oneClickSetupRequested && !_isRefreshingState;
                DrawActionButton(
                    "Setup",
                    canInstallSelected
                        ? "Run selected package installs and starter runtime scaffold actions."
                        : "Wait until the current installation process completes.",
                    canInstallSelected,
                    StartOneClickSetup);
            }
        }

        private bool DrawInstallPlanRow(string label, bool selected, bool installed, string details)
        {
            EditorGUILayout.BeginHorizontal();
            var value = installed || selected;
            using (new EditorGUI.DisabledScope(installed || _isInstalling || _oneClickSetupRequested))
            {
                value = EditorGUILayout.ToggleLeft(label, value, GUILayout.Width(260f));
            }

            var old = GUI.color;
            GUI.color = installed
                ? new Color(0.25f, 0.7f, 0.25f)
                : value
                    ? new Color(0.85f, 0.65f, 0.2f)
                    : new Color(0.65f, 0.65f, 0.65f);
            EditorGUILayout.LabelField(installed ? "Installed" : value ? "Selected" : "Skipped", GUILayout.Width(72f));
            GUI.color = old;
            EditorGUILayout.LabelField(details, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();

            if (installed)
            {
                return true;
            }

            return value;
        }

        private void DrawOdinPackagePathField()
        {
            var path = ResolveOdinPackagePath();
            var embeddedPath = FindEmbeddedOdinPackagePath();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Odin package", GUILayout.Width(120f));
            EditorGUILayout.SelectableLabel(string.IsNullOrWhiteSpace(path) ? "Not found" : path, EditorStyles.textField, GUILayout.Height(18f));
            if (GUILayout.Button("Browse", GUILayout.Width(80f)))
            {
                SelectOdinPackagePath();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(embeddedPath) && File.Exists(embeddedPath))
            {
                EditorGUILayout.HelpBox(
                    "Odin will be imported from the unitypackage embedded in the Evo package.",
                    MessageType.Info);
                return;
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                EditorGUILayout.HelpBox(
                    $"Put an Odin .unitypackage into {EmbeddedOdinPackageFolder}, or select it manually. Keep license restrictions in mind if this package is shared outside your team.",
                    MessageType.Warning);
            }
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
            if (_installVContainer && !_vContainerInstalled) _installQueue.Enqueue(VContainerSource);
            if (_installUniTask && !_uniTaskInstalled) _installQueue.Enqueue(UniTaskSource);
            if (_installNuGetForUnity && !_nuGetForUnityInstalled) _installQueue.Enqueue(NuGetForUnitySource);
            if (_installAddressables && !_addressablesInstalled) _installQueue.Enqueue(AddressablesSource);
            if (_installLocalization && !_localizationInstalled) _installQueue.Enqueue(LocalizationSource);
            if (_installInputSystem && !_inputSystemInstalled) _installQueue.Enqueue(InputSystemSource);
            if (_installUgui && !_uguiInstalled) _installQueue.Enqueue(UguiSource);

            _isInstalling = _installQueue.Count > 0;
            _statusLine = "Installing selected dependencies...";
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

        private void InstallR3Unity()
        {
            EnqueueSingleInstall(R3UnitySource, "Installing R3.Unity...");
        }

        private void InstallRuntimePackage()
        {
            _installQueue.Enqueue($"{RuntimeGitUrl}#{RuntimeGitTag}");
            _isInstalling = true;
            _statusLine = $"Installing runtime package from git tag {RuntimeGitTag}...";
        }

        private void InstallYandexPackage()
        {
            _installQueue.Enqueue($"{YandexGitUrl}#{YandexGitTag}");
            _isInstalling = true;
            _statusLine = $"Installing Yandex package from git tag {YandexGitTag}...";
        }

        private void InstallSelectedUpmPackagesBatch()
        {
            var packages = CollectSelectedUpmPackagesToInstall();
            if (packages.Count == 0)
            {
                return;
            }

            if (_installPrimeTween && !_primeTweenInstalled && !EnsurePrimeTweenScopedRegistry())
            {
                _statusLine = "Failed to add scoped registry for PrimeTween in Packages/manifest.json.";
                Debug.LogError("[Evo Setup] Failed to add scoped registry for PrimeTween in Packages/manifest.json.");
                return;
            }

            _isInstalling = true;
            _statusLine = $"Adding selected packages in one batch: {packages.Count}";
            Debug.Log($"[Evo Setup] Adding selected packages in one Package Manager batch:\n{string.Join("\n", packages)}");
            _addAndRemoveRequest = Client.AddAndRemove(packages.ToArray(), Array.Empty<string>());
        }

        private List<string> CollectSelectedUpmPackagesToInstall()
        {
            var packages = new List<string>();
            if (_installVContainer && !_vContainerInstalled) packages.Add(VContainerSource);
            if (_installUniTask && !_uniTaskInstalled) packages.Add(UniTaskSource);
            if (_installNuGetForUnity && !_nuGetForUnityInstalled) packages.Add(NuGetForUnitySource);
            if (_installAddressables && !_addressablesInstalled) packages.Add(AddressablesSource);
            if (_installLocalization && !_localizationInstalled) packages.Add(LocalizationSource);
            if (_installInputSystem && !_inputSystemInstalled) packages.Add(InputSystemSource);
            if (_installUgui && !_uguiInstalled) packages.Add(UguiSource);
            if (_installPrimeTween && !_primeTweenInstalled) packages.Add(PrimeTweenSource);
            if (_installR3Unity && !_r3UnityInstalled) packages.Add(R3UnitySource);
            if (_installRuntimeModule && !_runtimeInstalled) packages.Add($"{RuntimeGitUrl}#{RuntimeGitTag}");
            if (_installYandexModule && !_yandexInstalled) packages.Add($"{YandexGitUrl}#{YandexGitTag}");
            return packages;
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
            EnsureNuGetPackage(root, ObservableCollectionsR3NuGetId, ObservableCollectionsR3NuGetVersion);
            EnsureNuGetPackage(root, BclAsyncInterfacesNuGetId, BclAsyncInterfacesNuGetVersion);
            EnsureNuGetPackage(root, BclTimeProviderNuGetId, BclTimeProviderNuGetVersion);
            EnsureNuGetPackage(root, ComponentAnnotationsNuGetId, ComponentAnnotationsNuGetVersion);
            EnsureNuGetPackage(root, ThreadingChannelsNuGetId, ThreadingChannelsNuGetVersion);
            document.Save(packagesConfigPath);

            AssetDatabase.Refresh();
            TryInvokeNuGetRestore();
            _statusLine = "Added R3, ObservableCollections and ObservableCollections.R3 to packages.config. Waiting for NuGet restore/import...";
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
            ValidateTemplatesAndScaffoldScriptsState();
            if (!_templatesReady)
            {
                _statusLine = "Starter scaffold templates are invalid. Fix template issues before scaffold setup.";
                Debug.LogError("[Evo Setup] Starter scaffold templates are invalid. Fix template issues before scaffold setup.");
                return;
            }

            CreateProjectStructure();
            EnsureFolder("Assets/_Project/Prefabs/Runtime");
            EnsureScene(EntryScenePath, "EntryPointRoot");
            EnsureScene(LoadingScenePath, "LoadingRoot");
            EnsureScene(TransitionScenePath, "TransitionRoot");
            EnsureScene(MenuScenePath, "MainMenuRoot");
            EnsureStarterScripts();
            AssetDatabase.Refresh();

            if (!AreStarterRuntimeTypesReady())
            {
                QueueFinalizeStarterRuntimeScaffold();
                _statusLine = "Starter scripts created. Waiting for Unity to compile before configuring scenes and assets...";
                Debug.Log("[Evo Setup] Starter scripts created. Waiting for Unity to compile before configuring scenes and assets...");
                return;
            }

            FinalizeStarterRuntimeScaffold();
        }

        private void QueueFinalizeStarterRuntimeScaffold()
        {
            if (_scaffoldFinalizeQueued)
            {
                return;
            }

            _scaffoldFinalizeQueued = true;
            EditorApplication.delayCall += FinalizeStarterRuntimeScaffoldWhenReady;
        }

        private void FinalizeStarterRuntimeScaffoldWhenReady()
        {
            _scaffoldFinalizeQueued = false;

            if (this == null)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !AreStarterRuntimeTypesReady())
            {
                QueueFinalizeStarterRuntimeScaffold();
                return;
            }

            FinalizeStarterRuntimeScaffold();
        }

        private void FinalizeStarterRuntimeScaffold()
        {
            EnsureDefaultAssets();
            ConfigureStarterScenes();
            EnsureStarterSceneInAddressables(LoadingScenePath, "LoadingScene");
            EnsureStarterSceneInAddressables(MenuScenePath, "MainMenuScene");
            ConfigureProjectConfigForStarterPipeline();
            EnsureBuildScenes();
            ValidateAndFixBootstrapScopes();
            AssetDatabase.Refresh();
            OpenEntryPointScene();
            _statusLine = "Starter runtime scaffold created.";
            Debug.Log("[Evo Setup] Starter runtime scaffold created.");
            RefreshState();
        }

        private static void OpenEntryPointScene()
        {
            if (!File.Exists(EntryScenePath))
            {
                return;
            }

            EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Single);
        }

        private static bool AreStarterRuntimeTypesReady()
        {
            return FindTypeByName("_Project.Scripts.Runtime.EntryPoint.RuntimeProjectLifetimeScope") != null &&
                   FindTypeByName("_Project.Scripts.Application.Config.ProjectConfig") != null;
        }

        private void EnsureDefaultAssets()
        {
            CreateScriptableAsset(
                "_Project.Scripts.Application.Config.ProjectConfig, Assembly-CSharp",
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

        private static void EnsureStarterSceneInAddressables(string scenePath, string address)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath))
            {
                return;
            }

            var sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
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
                addressProperty.SetValue(entry, string.IsNullOrWhiteSpace(address)
                    ? Path.GetFileNameWithoutExtension(scenePath)
                    : address);
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
            UpdateOneClickProgressBar();

            if (_addAndRemoveRequest != null)
            {
                if (!_addAndRemoveRequest.IsCompleted)
                {
                    _isInstalling = true;
                    return;
                }

                if (_addAndRemoveRequest.Status == StatusCode.Success)
                {
                    _statusLine = "Selected packages were added. Waiting for Unity refresh...";
                    Debug.Log("[Evo Setup] Selected packages were added through Package Manager batch request.");
                }
                else
                {
                    _statusLine = $"Package setup failed: {_addAndRemoveRequest.Error?.message}";
                    Debug.LogError($"[Evo Setup] Package setup failed: {_addAndRemoveRequest.Error?.message}");
                    _installQueue.Clear();
                    _isInstalling = false;
                    _oneClickSetupRequested = false;
                    SessionState.SetBool(GetOneClickStateKey(), false);
                    EditorUtility.ClearProgressBar();
                    _addAndRemoveRequest = null;
                    Repaint();
                    return;
                }

                _addAndRemoveRequest = null;
                _isInstalling = false;
                RefreshState();
                QueueRefreshBurst();
                Repaint();
                return;
            }

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
                    Debug.Log($"[Evo Setup] Installed package: {_addRequest.Result.packageId}");
                }
                else
                {
                    _statusLine = $"Install failed: {_addRequest.Error?.message}";
                    Debug.LogError($"[Evo Setup] Package install failed: {_addRequest.Error?.message}");
                    _installQueue.Clear();
                    _isInstalling = false;
                    _oneClickSetupRequested = false;
                    SessionState.SetBool(GetOneClickStateKey(), false);
                    EditorUtility.ClearProgressBar();
                    _addRequest = null;
                    Repaint();
                    return;
                }

                _addRequest = null;
                RefreshState();
                QueueRefreshBurst();
                Repaint();
                return;
            }

            if (_addRequest == null && _installQueue.Count > 0)
            {
                if (_isRefreshingState || EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    return;
                }

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

            ContinueOneClickSetup();
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
                _r3UnityInstalled = HasAnyPackage(packages, R3UnityPackageName, "r3.unity", "r3") ||
                                    ManifestHasAnyDependency(R3UnityPackageName);
                _dependenciesInstalled = _vContainerInstalled &&
                                         _uniTaskInstalled &&
                                         _nuGetForUnityInstalled &&
                                         _addressablesInstalled &&
                                         _localizationInstalled &&
                                         _inputSystemInstalled &&
                                         _uguiInstalled;
                _runtimeInstalled = HasAnyPackage(packages, RuntimePackageName, "com.evo.infrastructure.runtime") ||
                                    ManifestHasAnyDependency(RuntimePackageName);
                _yandexInstalled = HasAnyPackage(packages, YandexPackageName, "com.evo.infrastructure.yandex") ||
                                   ManifestHasAnyDependency(YandexPackageName);
                _odinInstalled = IsOdinInstalled();
                ReadReactivePackagesConfig(
                    out _r3InPackagesConfig,
                    out _observableCollectionsInPackagesConfig,
                    out _observableCollectionsR3InPackagesConfig);
                _r3Ready = _r3InPackagesConfig || _r3UnityInstalled || IsAssemblyLoaded("R3");
                _observableCollectionsReady = _observableCollectionsInPackagesConfig || IsAssemblyLoaded("ObservableCollections");
                _observableCollectionsR3Ready = _observableCollectionsR3InPackagesConfig || IsAssemblyLoaded("ObservableCollections.R3");
            }

            _structureReady = HasProjectStructure();
            _odinInstalled = IsOdinInstalled();
            ValidateTemplatesAndScaffoldScriptsState();
            _bootstrapScopesReady = AreBootstrapScopesValid();
            _isRefreshingState = false;
            ContinueOneClickSetup();
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
            if (!_isInstalling && _addRequest == null && _addAndRemoveRequest == null && _installQueue.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Processing...", EditorStyles.boldLabel);
            var label = _addAndRemoveRequest != null
                ? "Adding selected packages..."
                : _addRequest != null
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
                   File.Exists(StarterLoadingSceneLifetimeScopePath) &&
                   File.Exists(StarterProjectConfigPath) &&
                   File.Exists(StarterLoadingPresenterPath) &&
                   File.Exists(StarterLoadingViewSystemPath);
        }

        private void DrawReactiveWarning()
        {
            if (_r3Ready && _observableCollectionsReady && _observableCollectionsR3Ready)
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

            if (!_observableCollectionsR3Ready)
            {
                missing.Add("ObservableCollections.R3");
            }

            var message =
                $"Install missing reactive libraries: {string.Join(", ", missing)}.\n" +
                "Use step 10 to add R3, ObservableCollections and ObservableCollections.R3 to packages.config via NuGetForUnity.";
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private void ValidateTemplatesAndScaffoldScriptsState()
        {
            _templateValidationIssues.Clear();
            _templatesReady = true;

            for (var i = 0; i < StarterScriptTemplates.Length; i++)
            {
                var templatePath = GetTemplatePath(StarterScriptTemplates[i].TemplateFileName);
                if (!File.Exists(templatePath))
                {
                    _templatesReady = false;
                    _templateValidationIssues.Add($"Missing template: {templatePath}");
                }
            }

            _scaffoldScriptsUpToDate = _templatesReady && AreScaffoldScriptsUpToDate();
        }

        private static bool AreScaffoldScriptsUpToDate()
        {
            for (var i = 0; i < StarterScriptTemplates.Length; i++)
            {
                if (!IsScaffoldScriptUpToDate(StarterScriptTemplates[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsScaffoldScriptUpToDate(StarterScriptTemplate template)
        {
            if (!File.Exists(template.TargetPath))
            {
                return false;
            }

            var templatePath = GetTemplatePath(template.TemplateFileName);
            if (!File.Exists(templatePath))
            {
                return false;
            }

            try
            {
                var templateText = NormalizeText(File.ReadAllText(templatePath));
                var targetText = NormalizeText(File.ReadAllText(template.TargetPath));
                return string.Equals(templateText, targetText, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("\r\n", "\n").Trim();
        }

        private void UpdateScaffoldScriptsFromTemplates()
        {
            var updated = new List<string>();
            var errors = new List<string>();

            for (var i = 0; i < StarterScriptTemplates.Length; i++)
            {
                var template = StarterScriptTemplates[i];
                var templatePath = GetTemplatePath(template.TemplateFileName);
                if (!File.Exists(templatePath))
                {
                    errors.Add($"Template missing: {template.TemplateFileName}");
                    continue;
                }

                string templateText;
                try
                {
                    templateText = File.ReadAllText(templatePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to read template '{template.TemplateFileName}': {ex.Message}");
                    continue;
                }

                var targetDirectory = Path.GetDirectoryName(template.TargetPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (File.Exists(template.TargetPath))
                {
                    string existingText;
                    try
                    {
                        existingText = File.ReadAllText(template.TargetPath);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to read existing script '{template.TargetPath}': {ex.Message}");
                        continue;
                    }

                    if (string.Equals(NormalizeText(templateText), NormalizeText(existingText), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(template.TargetPath);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to delete old script '{template.TargetPath}': {ex.Message}");
                        continue;
                    }
                }

                try
                {
                    File.WriteAllText(template.TargetPath, templateText);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to write script '{template.TargetPath}': {ex.Message}");
                    continue;
                }

                updated.Add(Path.GetFileName(template.TargetPath));
            }

            AssetDatabase.Refresh();
            ValidateTemplatesAndScaffoldScriptsState();

            if (errors.Count > 0)
            {
                _statusLine = "Scaffold update completed with issues: " + string.Join(" | ", errors);
                Debug.LogError("[Evo Setup] " + _statusLine);
                return;
            }

            _statusLine = updated.Count == 0
                ? "Scaffold scripts are already up to date."
                : "Updated scaffold scripts: " + string.Join(", ", updated);
            Debug.Log("[Evo Setup] " + _statusLine);
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

        private static bool IsOdinInstalled()
        {
            return IsAssemblyLoaded("Sirenix.OdinInspector.Attributes") ||
                   IsAssemblyLoaded("Sirenix.OdinInspector.Editor") ||
                   FindTypeByName("Sirenix.OdinInspector.ShowInInspectorAttribute") != null;
        }

        private bool SelectOdinPackagePath()
        {
            var current = EditorPrefs.GetString(OdinPackagePathPrefsKey, string.Empty);
            var folder = !string.IsNullOrWhiteSpace(current) && File.Exists(current)
                ? Path.GetDirectoryName(current)
                : EmbeddedOdinPackageFolder;
            var path = EditorUtility.OpenFilePanel(
                "Select Odin Inspector unitypackage",
                folder ?? string.Empty,
                "unitypackage");

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            EditorPrefs.SetString(OdinPackagePathPrefsKey, path);
            _statusLine = $"Selected Odin package: {Path.GetFileName(path)}";
            Repaint();
            return true;
        }

        private static string ResolveOdinPackagePath()
        {
            var embeddedPath = FindEmbeddedOdinPackagePath();
            if (!string.IsNullOrWhiteSpace(embeddedPath) && File.Exists(embeddedPath))
            {
                return embeddedPath;
            }

            var storedPath = EditorPrefs.GetString(OdinPackagePathPrefsKey, string.Empty);
            return !string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath)
                ? storedPath
                : string.Empty;
        }

        private static string FindEmbeddedOdinPackagePath()
        {
            if (!Directory.Exists(EmbeddedOdinPackageFolder))
            {
                return string.Empty;
            }

            var files = Directory.GetFiles(EmbeddedOdinPackageFolder, OdinPackageSearchPattern, SearchOption.AllDirectories);
            return files.Length > 0
                ? files.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).First().Replace("\\", "/")
                : string.Empty;
        }

        private bool TryImportOdinPackage(bool interactive)
        {
            if (IsOdinInstalled())
            {
                _odinInstalled = true;
                _statusLine = "Odin Inspector is already installed.";
                return true;
            }

            var path = ResolveOdinPackagePath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                if (!interactive)
                {
                    _statusLine = $"Odin package file not found. Put Odin .unitypackage into {EmbeddedOdinPackageFolder}.";
                    Debug.LogError("[Evo Setup] " + _statusLine);
                    return false;
                }

                if (!SelectOdinPackagePath())
                {
                    _statusLine = "Odin import canceled.";
                    return false;
                }

                path = EditorPrefs.GetString(OdinPackagePathPrefsKey, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _statusLine = "Odin import canceled.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FindEmbeddedOdinPackagePath()))
            {
                EditorPrefs.SetString(OdinPackagePathPrefsKey, path);
            }

            _statusLine = $"Importing Odin package: {Path.GetFileName(path)}";
            Debug.Log("[Evo Setup] " + _statusLine);
            AssetDatabase.ImportPackage(path, false);
            AssetDatabase.Refresh();
            _odinInstalled = IsOdinInstalled();
            return true;
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

        private static void ReadReactivePackagesConfig(
            out bool hasR3,
            out bool hasObservableCollections,
            out bool hasObservableCollectionsR3)
        {
            hasR3 = false;
            hasObservableCollections = false;
            hasObservableCollectionsR3 = false;

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

                    if (string.Equals(id, ObservableCollectionsR3NuGetId, StringComparison.OrdinalIgnoreCase))
                    {
                        hasObservableCollectionsR3 = true;
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
            for (var i = 0; i < StarterScriptTemplates.Length; i++)
            {
                EnsureStarterScriptFromTemplate(
                    StarterScriptTemplates[i].TargetPath,
                    StarterScriptTemplates[i].TemplateFileName);
            }
        }

        private static void EnsureStarterScriptFromTemplate(string targetPath, string templateFileName)
        {
            if (File.Exists(targetPath))
            {
                return;
            }

            var templatePath = GetTemplatePath(templateFileName);
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[Evo Setup] Missing scaffold template: {templatePath}");
                return;
            }

            var templateText = File.ReadAllText(templatePath);
            File.WriteAllText(targetPath, templateText);
        }

        private static string GetTemplatePath(string templateFileName)
        {
            return Path.Combine(TemplatesRootPath, templateFileName).Replace("\\", "/");
        }

        private void QueueRefreshBurst()
        {
            EditorApplication.delayCall += RefreshState;
            EditorApplication.delayCall += () => EditorApplication.delayCall += RefreshState;
        }

        private void StartOneClickSetup()
        {
            if (_installOdinPackage && !_odinInstalled)
            {
                var odinPath = ResolveOdinPackagePath();
                if (string.IsNullOrWhiteSpace(odinPath) || !File.Exists(odinPath))
                {
                    if (!SelectOdinPackagePath())
                    {
                        _statusLine = "Setup canceled: Odin package source is required for Odin installation.";
                        Debug.LogError("[Evo Setup] Setup canceled: Odin package source is required for Odin installation.");
                        return;
                    }
                }
            }

            _oneClickSetupRequested = true;
            SessionState.SetBool(GetOneClickStateKey(), true);
            Debug.Log("[Evo Setup] Setup started.");
            _statusLine = "Setup started.";
            RefreshState();
            ContinueOneClickSetup();
        }

        private void ResumeOneClickSetupIfNeeded()
        {
            _oneClickSetupRequested = SessionState.GetBool(GetOneClickStateKey(), false);
            if (_oneClickSetupRequested)
            {
                _statusLine = "Resuming setup after domain reload...";
                Debug.Log("[Evo Setup] Resumed setup after domain reload.");
            }
        }

        private void ContinueOneClickSetup()
        {
            if (!_oneClickSetupRequested)
            {
                return;
            }

            if (_isInstalling || _addRequest != null || _addAndRemoveRequest != null || _isRefreshingState)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (_scaffoldFinalizeQueued)
            {
                return;
            }

            if (HasSelectedUpmPackagesToInstall())
            {
                InstallSelectedUpmPackagesBatch();
                return;
            }

            if (_installReactiveNuGets && (!_r3Ready || !_observableCollectionsReady || !_observableCollectionsR3Ready))
            {
                if (!_nuGetForUnityInstalled)
                {
                    _oneClickSetupRequested = false;
                    SessionState.SetBool(GetOneClickStateKey(), false);
                    EditorUtility.ClearProgressBar();
                    _statusLine = "Setup stopped: Reactive NuGets require NuGetForUnity. Enable NuGetForUnity or disable Reactive NuGets.";
                    Debug.LogError("[Evo Setup] Reactive NuGets require NuGetForUnity. Enable NuGetForUnity or disable Reactive NuGets.");
                    Repaint();
                    return;
                }

                _statusLine = "Setup: configuring reactive NuGet dependencies...";
                Debug.Log("[Evo Setup] Configuring reactive NuGet dependencies...");
                InstallReactiveFromNuGet();
                RefreshState();
                return;
            }

            if (_setupStarterScaffold && !_templatesReady)
            {
                _oneClickSetupRequested = false;
                SessionState.SetBool(GetOneClickStateKey(), false);
                EditorUtility.ClearProgressBar();
                _statusLine = "Setup stopped: scaffold templates are invalid.";
                Debug.LogError("[Evo Setup] Scaffold templates are invalid.");
                return;
            }

            if (_setupStarterScaffold && !HasStarterScaffold())
            {
                _statusLine = "Setup: creating starter runtime scaffold...";
                Debug.Log("[Evo Setup] Creating starter runtime scaffold...");
                SetupStarterRuntimeScaffold();
                return;
            }

            if (_setupStarterScaffold && !_bootstrapScopesReady)
            {
                _statusLine = "Setup: validating bootstrap scopes...";
                Debug.Log("[Evo Setup] Validating bootstrap scopes...");
                ValidateAndFixBootstrapScopes();
                return;
            }

            if (_installOdinPackage && !_odinInstalled)
            {
                _statusLine = "Setup: importing Odin package...";
                Debug.Log("[Evo Setup] Importing Odin package...");
                if (!TryImportOdinPackage(false))
                {
                    _oneClickSetupRequested = false;
                    SessionState.SetBool(GetOneClickStateKey(), false);
                    EditorUtility.ClearProgressBar();
                    Repaint();
                    return;
                }
            }

            _oneClickSetupRequested = false;
            SessionState.SetBool(GetOneClickStateKey(), false);
            if (_setupStarterScaffold)
            {
                OpenEntryPointScene();
            }

            _statusLine = "Setup completed.";
            Debug.Log("[Evo Setup] Setup completed.");
            EditorUtility.ClearProgressBar();
            Repaint();
        }

        private void UpdateOneClickProgressBar()
        {
            if (!_oneClickSetupRequested)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            var stepIndex = GetOneClickCompletedStepCount();
            var stepCount = GetOneClickStepCount();
            var progress = Mathf.Clamp01(stepIndex / (float)stepCount);
            EditorUtility.DisplayProgressBar(
                "Evo Setup",
                _statusLine,
                progress);
        }

        private int GetOneClickStepCount()
        {
            var count = 0;
            if (CollectSelectedUpmPackagesToInstall().Count > 0 || !IsSelectedUpmPackagesReady()) count++;
            if (_installReactiveNuGets) count++;
            if (_setupStarterScaffold)
            {
                count += 3;
            }

            if (_installOdinPackage)
            {
                count++;
            }

            return Mathf.Max(1, count);
        }

        private int GetOneClickCompletedStepCount()
        {
            var completed = 0;
            if (IsSelectedUpmPackagesReady()) completed++;
            if (_installReactiveNuGets && _r3Ready && _observableCollectionsReady && _observableCollectionsR3Ready) completed++;
            if (_setupStarterScaffold && _templatesReady) completed++;
            if (_setupStarterScaffold && HasStarterScaffold()) completed++;
            if (_setupStarterScaffold && _bootstrapScopesReady) completed++;
            if (_installOdinPackage && _odinInstalled) completed++;
            return completed;
        }

        private bool HasSelectedUpmPackagesToInstall()
        {
            return CollectSelectedUpmPackagesToInstall().Count > 0;
        }

        private bool IsSelectedUpmPackagesReady()
        {
            return (!_installVContainer || _vContainerInstalled) &&
                   (!_installUniTask || _uniTaskInstalled) &&
                   (!_installNuGetForUnity || _nuGetForUnityInstalled) &&
                   (!_installAddressables || _addressablesInstalled) &&
                   (!_installLocalization || _localizationInstalled) &&
                   (!_installInputSystem || _inputSystemInstalled) &&
                   (!_installUgui || _uguiInstalled) &&
                   (!_installPrimeTween || _primeTweenInstalled) &&
                   (!_installR3Unity || _r3UnityInstalled) &&
                   (!_installRuntimeModule || _runtimeInstalled) &&
                   (!_installYandexModule || _yandexInstalled);
        }

        private static string GetOneClickStateKey()
        {
            return OneClickStateKeyPrefix + Application.dataPath.GetHashCode();
        }

        private readonly struct StarterScriptTemplate
        {
            public readonly string TargetPath;
            public readonly string TemplateFileName;

            public StarterScriptTemplate(string targetPath, string templateFileName)
            {
                TargetPath = targetPath;
                TemplateFileName = templateFileName;
            }
        }
    }
}
#endif
