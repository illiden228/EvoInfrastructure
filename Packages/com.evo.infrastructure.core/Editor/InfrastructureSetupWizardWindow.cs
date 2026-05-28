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
        private const string RuntimeGitTag = "v0.4.3";
        private const string YandexGitTag = "v0.4.3";
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
        private const string ConfigCatalogPath = "Assets/_Project/Configs/ConfigCatalog.asset";
        private const string LegacyConfigCatalogPath = "Assets/_Project/Configs/ScriptableConfigCatalog.asset";
        private const string ResourceCatalogPath = "Assets/_Project/Configs/ResourceCatalog.asset";
        private const string YandexRuntimeConfigPath = "Assets/_Project/Configs/YandexRuntimeConfig.asset";
        private const string LifetimeScopePrefabPath = "Assets/_Project/Prefabs/Runtime/InfrastructureProjectLifetimeScope.prefab";
        private const string StarterRuntimeProjectLifetimeScopePath = "Assets/_Project/Scripts/Runtime/EntryPoint/RuntimeProjectLifetimeScope.cs";
        private const string StarterRuntimeEntryPointPath = "Assets/_Project/Scripts/Runtime/EntryPoint/RuntimeEntryPoint.cs";
        private const string StarterLoadingSceneLifetimeScopePath = "Assets/_Project/Scripts/Runtime/Loading/LoadingSceneLifetimeScope.cs";
        private const string StarterProjectConfigPath = "Assets/_Project/Scripts/Runtime/Config/ProjectConfig.cs";
        private const string StarterLoadingViewSystemPath = "Assets/_Project/Scripts/Runtime/Loading/LoadingViewSystem.cs";
        private const string StarterLoadingViewModelPath = "Assets/_Project/Scripts/Runtime/Loading/LoadingViewModel.cs";
        private const string StarterLoadingScreenViewPath = "Assets/_Project/Scripts/Runtime/Loading/LoadingScreenView.cs";
        private const string TemplatesRootPath = "Packages/com.evo.infrastructure.core/Editor/Templates";
        private const string RuntimeProjectLifetimeScopeTemplateName = "RuntimeProjectLifetimeScope.cs.txt";
        private const string RuntimeEntryPointTemplateName = "RuntimeEntryPoint.cs.txt";
        private const string LoadingSceneLifetimeScopeTemplateName = "LoadingSceneLifetimeScope.cs.txt";
        private const string ProjectConfigTemplateName = "ProjectConfig.cs.txt";
        private const string LoadingViewSystemTemplateName = "LoadingViewSystem.cs.txt";
        private const string LoadingViewModelTemplateName = "LoadingViewModel.cs.txt";
        private const string LoadingScreenViewTemplateName = "LoadingScreenView.cs.txt";
        private const string ProjectScopeTypeName = "RuntimeProjectLifetimeScope";
        private const string ProjectScopeFullTypeName = "Game.Runtime.EntryPoint.RuntimeProjectLifetimeScope";
        private const string LegacyProjectScopeFullTypeName = "Game.Runtime.Bootstrap.RuntimeProjectLifetimeScope";
        private const string OneClickStateKeyPrefix = "Evo.Infrastructure.Core.OneClickSetup.";
        private const string ScaffoldStateKeyPrefix = "Evo.Infrastructure.Core.ScaffoldSetup.";
        private const string OdinImportStateKeyPrefix = "Evo.Infrastructure.Core.OdinImportRequested.";
        private const string SelectionStateKeyPrefix = "Evo.Infrastructure.Core.SetupSelection.";
        private static readonly StarterScriptTemplate[] StarterScriptTemplates =
        {
            new(StarterRuntimeProjectLifetimeScopePath, RuntimeProjectLifetimeScopeTemplateName),
            new(StarterRuntimeEntryPointPath, RuntimeEntryPointTemplateName),
            new(StarterLoadingSceneLifetimeScopePath, LoadingSceneLifetimeScopeTemplateName),
            new(StarterProjectConfigPath, ProjectConfigTemplateName),
            new(StarterLoadingViewSystemPath, LoadingViewSystemTemplateName),
            new(StarterLoadingViewModelPath, LoadingViewModelTemplateName),
            new(StarterLoadingScreenViewPath, LoadingScreenViewTemplateName)
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
        private const string VContainerPackageName = "jp.hadashikick.vcontainer";
        private const string UniTaskPackageName = "com.cysharp.unitask";
        private const string NuGetForUnityPackageName = "com.github-glitchenzo.nugetforunity";
        private const string AddressablesPackageName = "com.unity.addressables";
        private const string LocalizationPackageName = "com.unity.localization";
        private const string InputSystemPackageName = "com.unity.inputsystem";
        private const string UguiPackageName = "com.unity.ugui";
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

        [NonSerialized] private readonly Queue<string> _installQueue = new();
        [NonSerialized] private AddRequest _addRequest;
        [NonSerialized] private AddAndRemoveRequest _addAndRemoveRequest;
        [NonSerialized] private RemoveRequest _removeRequest;
        [NonSerialized] private ListRequest _listRequest;
        private bool _dependenciesInstalled;
        private bool _runtimeInstalled;
        private bool _yandexInstalled;
        private bool _odinInstalled;
        private bool _structureReady;
        private bool _bootstrapScopesReady;
        private bool _starterAddressablesReady;
        private bool _starterBuildScenesReady;
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
        private EvoPackageUpdateState _runtimeUpdateState;
        private string _runtimeInstalledVersion = string.Empty;
        private string _runtimeInstalledPackageId = string.Empty;
        private string _runtimeManifestDependency = string.Empty;
        private bool _oneClickSetupRequested;
        private bool _scaffoldSetupRequested;
        private bool _scaffoldFinalizeQueued;
        private bool _reactiveRestoreRequested;
        private bool _stateAnalyzed;
        private bool _odinImportRequested;
        private bool _bootstrapValidationAttempted;
        private int _scaffoldFinalizationAttempts;
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
        private bool _installProjectStructure = true;
        private bool _installStarterScaffold = true;
        private bool _templatesReady;
        private bool _scaffoldScriptsUpToDate;
        private double _refreshStartedAt;
        private string _statusLine = "Ready";
        private Vector2 _scroll;
        private readonly List<string> _templateValidationIssues = new();

        private enum EvoPackageUpdateState
        {
            Unknown = 0,
            Missing = 1,
            InstalledTarget = 2,
            InstalledDifferentRevision = 3
        }

        [MenuItem("EvoTools/Setup")]
        public static void OpenWindow()
        {
            ClearSetupSessionState();
            var window = GetWindow<InfrastructureSetupWizardWindow>("Evo Setup");
            window.minSize = new Vector2(620f, 420f);
            window.RefreshState();
            window.Show();
        }

        private void OnEnable()
        {
            ClearTransientPackageRequests();
            LoadSelectionState();
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
            DrawInstallPlan();
            GUILayout.Space(10f);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Evo Infrastructure Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_statusLine, MessageType.Info);
        }

        private void DrawInstallPlan()
        {
            EditorGUILayout.LabelField("Install Plan", EditorStyles.boldLabel);

            if (!_stateAnalyzed)
            {
                EditorGUILayout.HelpBox(
                    _isRefreshingState
                        ? "Analyzing packages and project state..."
                        : "Run Analyze Installed Packages before Setup.",
                    MessageType.Warning);
            }

            if (_templateValidationIssues.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Template issues:\n" + string.Join("\n", _templateValidationIssues),
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(_isRefreshingState || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
            {
                DrawActionButton(
                    _isRefreshingState ? "Analyzing..." : "Analyze Installed Packages",
                    "Refresh package and scaffold state before setup.",
                    !_isRefreshingState && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested,
                    RefreshState,
                    26f);
            }

            if (_oneClickSetupRequested || _scaffoldSetupRequested)
            {
                DrawActionButton(
                    _scaffoldSetupRequested ? "Cancel Scaffold" : "Cancel Setup",
                    "Stop the current setup session and keep current project files as they are.",
                    true,
                    CancelSetup,
                    26f);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Runtime Packages", EditorStyles.boldLabel);
            _installVContainer = DrawInstallPlanRow("VContainer", _installVContainer, _vContainerInstalled, "DI container for project and scene scopes.", () => RemovePackage(VContainerPackageName, "VContainer"));
            _installUniTask = DrawInstallPlanRow("UniTask", _installUniTask, _uniTaskInstalled, "Async runtime used by loading and services.", () => RemovePackage(UniTaskPackageName, "UniTask"));
            _installNuGetForUnity = DrawInstallPlanRow("NuGetForUnity", _installNuGetForUnity, _nuGetForUnityInstalled, "Installs reactive NuGet packages.", () => RemovePackage(NuGetForUnityPackageName, "NuGetForUnity"));
            _installAddressables = DrawInstallPlanRow("Addressables", _installAddressables, _addressablesInstalled, "Startup scene references are created as addressable assets.", () => RemovePackage(AddressablesPackageName, "Addressables"));
            _installLocalization = DrawInstallPlanRow("Unity Localization", _installLocalization, _localizationInstalled, "Runtime localization package.", () => RemovePackage(LocalizationPackageName, "Unity Localization"));
            _installInputSystem = DrawInstallPlanRow("Input System", _installInputSystem, _inputSystemInstalled, "Default input package for gameplay projects.", () => RemovePackage(InputSystemPackageName, "Input System"));
            _installUgui = DrawInstallPlanRow("Unity UI", _installUgui, _uguiInstalled, "Base UI package for loading and menus.", () => RemovePackage(UguiPackageName, "Unity UI"));
            _installPrimeTween = DrawInstallPlanRow("PrimeTween", _installPrimeTween, _primeTweenInstalled, "Tweening dependency.", () => RemovePackage(PrimeTweenPackageName, "PrimeTween"));
            _installR3Unity = DrawInstallPlanRow("R3.Unity", _installR3Unity, _r3UnityInstalled, "Reactive Unity integration.", () => RemovePackage(R3UnityPackageName, "R3.Unity"));
            _installReactiveNuGets = DrawInstallPlanRow("Reactive NuGets", _installReactiveNuGets, AreReactiveAssembliesReady() || AreReactivePackagesConfigReady(), "R3, ObservableCollections and ObservableCollections.R3.", RemoveReactiveNuGetPackages);
            _installRuntimeModule = DrawRuntimePackageRow();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Optional Modules", EditorStyles.boldLabel);
            _installYandexModule = DrawInstallPlanRow("Evo Infrastructure Yandex", _installYandexModule, _yandexInstalled, "YG2 integration package.", () => RemovePackage(YandexPackageName, "Evo Infrastructure Yandex"));
            _installOdinPackage = DrawInstallPlanRow("Odin Inspector", _installOdinPackage, _odinInstalled, "Imported automatically at the end of Setup. Odin is not required for starter runtime.");

            EditorGUILayout.Space(4f);
            DrawProjectRuntimeActions();

            var hasSelectedWork = HasSelectedSetupWork();
            using (new EditorGUI.DisabledScope(_isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || !_stateAnalyzed || !hasSelectedWork))
            {
                var canInstallSelected = _stateAnalyzed && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested && !_isRefreshingState && hasSelectedWork;
                DrawActionButton(
                    "Setup",
                    canInstallSelected
                        ? "Install selected packages and selected project runtime tasks."
                        : hasSelectedWork
                            ? "Wait until the current operation completes."
                            : "Everything selected is already ready.",
                    canInstallSelected,
                    StartOneClickSetup);
            }

            SaveSelectionState();
        }

        private void DrawProjectRuntimeActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Project Runtime", EditorStyles.boldLabel);

            var packagesReadyForScaffold = ArePackagesReadyForStarterScaffold();
            var scaffoldReady = IsStarterScaffoldReady();
            _installProjectStructure = DrawSetupTaskRow(
                "Project Structure",
                _installProjectStructure,
                _structureReady,
                _structureReady ? "Ready" : "Missing",
                "Base folders under Assets/_Project.",
                _structureReady ? "Ready" : "Create",
                _stateAnalyzed && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested && !_isRefreshingState && !_structureReady,
                CreateProjectStructure);
            _installStarterScaffold = DrawSetupTaskRow(
                "Starter Scaffold",
                _installStarterScaffold,
                scaffoldReady,
                _scaffoldSetupRequested ? "Running" : scaffoldReady ? "Ready" : HasStarterScaffoldFiles() ? "Needs Repair" : "Missing",
                GetStarterScaffoldStatusDetails(packagesReadyForScaffold),
                scaffoldReady ? "Ready" : HasStarterScaffoldFiles() ? "Repair" : "Create",
                _stateAnalyzed && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested && !_isRefreshingState && packagesReadyForScaffold && !scaffoldReady,
                StartStarterRuntimeScaffold);
        }

        private bool DrawSetupTaskRow(
            string label,
            bool selected,
            bool ready,
            string status,
            string details,
            string actionLabel,
            bool actionEnabled,
            Action action)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(ready || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
            {
                selected = EditorGUILayout.Toggle(ready || selected, GUILayout.Width(18f));
            }

            EditorGUILayout.LabelField(label, GUILayout.Width(202f));
            var old = GUI.color;
            GUI.color = ready
                ? new Color(0.25f, 0.7f, 0.25f)
                : new Color(0.85f, 0.65f, 0.2f);
            EditorGUILayout.LabelField(status, GUILayout.Width(92f));
            GUI.color = old;
            EditorGUILayout.LabelField(details);
            using (new EditorGUI.DisabledScope(!actionEnabled))
            {
                if (GUILayout.Button(actionLabel, GUILayout.Width(76f)))
                {
                    action?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
            return ready || selected;
        }

        private string GetStarterScaffoldStatusDetails(bool packagesReadyForScaffold)
        {
            if (!packagesReadyForScaffold)
            {
                return "Requires runtime scaffold packages.";
            }

            var missing = new List<string>(5);
            if (!HasStarterScaffoldFiles()) missing.Add("files");
            if (!_scaffoldScriptsUpToDate) missing.Add("scripts");
            if (!AreStarterRuntimeTypesReady()) missing.Add("compiled types");
            if (!_bootstrapScopesReady) missing.Add("bootstrap scopes");
            if (!_starterAddressablesReady) missing.Add("Addressables");
            if (!_starterBuildScenesReady) missing.Add("Build Settings");

            return missing.Count == 0
                ? "Starter scripts, scenes, configs, Addressables entries and build scenes."
                : "Missing: " + string.Join(", ", missing) + ".";
        }

        private bool DrawInstallPlanRow(string label, bool selected, bool installed, string details, Action removeAction = null)
        {
            EditorGUILayout.BeginHorizontal();
            var value = installed || selected;
            using (new EditorGUI.DisabledScope(installed || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
            {
                value = EditorGUILayout.ToggleLeft(label, value, GUILayout.Width(220f));
            }

            var old = GUI.color;
            GUI.color = installed
                ? new Color(0.25f, 0.7f, 0.25f)
                : value
                    ? new Color(0.85f, 0.65f, 0.2f)
                    : new Color(0.65f, 0.65f, 0.65f);
            EditorGUILayout.LabelField(installed ? "Installed" : value ? "Selected" : "Skipped", GUILayout.Width(72f));
            GUI.color = old;
            using (new EditorGUI.DisabledScope(!installed || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || _removeRequest != null || removeAction == null))
            {
                if (GUILayout.Button("Remove", GUILayout.Width(68f)))
                {
                    removeAction?.Invoke();
                }
            }

            EditorGUILayout.LabelField(details, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();

            if (installed)
            {
                return true;
            }

            return value;
        }

        private bool DrawRuntimePackageRow()
        {
            var updateAvailable = _runtimeUpdateState == EvoPackageUpdateState.InstalledDifferentRevision;
            var installedTarget = _runtimeUpdateState == EvoPackageUpdateState.InstalledTarget;
            var installed = _runtimeInstalled;
            var value = installed || _installRuntimeModule;

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(installed || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
            {
                value = EditorGUILayout.ToggleLeft("Evo Infrastructure Runtime", value, GUILayout.Width(220f));
            }

            var old = GUI.color;
            GUI.color = installedTarget
                ? new Color(0.25f, 0.7f, 0.25f)
                : updateAvailable
                    ? new Color(0.85f, 0.65f, 0.2f)
                    : value
                        ? new Color(0.85f, 0.65f, 0.2f)
                        : new Color(0.65f, 0.65f, 0.65f);
            EditorGUILayout.LabelField(GetRuntimePackageStatusLabel(value), GUILayout.Width(92f));
            GUI.color = old;

            using (new EditorGUI.DisabledScope(!installed || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || _removeRequest != null))
            {
                if (GUILayout.Button("Remove", GUILayout.Width(68f)))
                {
                    RemovePackage(RuntimePackageName, "Evo Infrastructure Runtime");
                }
            }

            using (new EditorGUI.DisabledScope(!updateAvailable || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || _addRequest != null || _addAndRemoveRequest != null))
            {
                if (GUILayout.Button("Update", GUILayout.Width(68f)))
                {
                    UpdateRuntimePackage();
                }
            }

            EditorGUILayout.LabelField(GetRuntimePackageDetails(), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();

            return installed || value;
        }

        private string GetRuntimePackageStatusLabel(bool selected)
        {
            switch (_runtimeUpdateState)
            {
                case EvoPackageUpdateState.InstalledTarget:
                    return "Ready";
                case EvoPackageUpdateState.InstalledDifferentRevision:
                    return "Update";
                case EvoPackageUpdateState.Missing:
                    return selected ? "Selected" : "Skipped";
                default:
                    return _runtimeInstalled ? "Installed" : selected ? "Selected" : "Skipped";
            }
        }

        private string GetRuntimePackageDetails()
        {
            var target = $"target {RuntimeGitTag}";
            if (_runtimeUpdateState == EvoPackageUpdateState.InstalledTarget)
            {
                return $"Runtime framework package, {target}.";
            }

            if (_runtimeUpdateState == EvoPackageUpdateState.InstalledDifferentRevision)
            {
                var current = !string.IsNullOrWhiteSpace(_runtimeManifestDependency)
                    ? _runtimeManifestDependency
                    : !string.IsNullOrWhiteSpace(_runtimeInstalledPackageId)
                        ? _runtimeInstalledPackageId
                        : _runtimeInstalledVersion;
                return $"Runtime framework package, update available: {current} -> {RuntimeGitTag}.";
            }

            return $"Runtime framework package, {target}.";
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
            if (!EnsurePrimeTweenScopedRegistry(out _))
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

        private void UpdateRuntimePackage()
        {
            EnqueueSingleInstall(
                $"{RuntimeGitUrl}#{RuntimeGitTag}",
                $"Updating runtime package to git tag {RuntimeGitTag}...");
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

            if (_installPrimeTween && !_primeTweenInstalled)
            {
                if (!EnsurePrimeTweenScopedRegistry(out var registryChanged))
                {
                    _statusLine = "Failed to add scoped registry for PrimeTween in Packages/manifest.json.";
                    Debug.LogError("[Evo Setup] Failed to add scoped registry for PrimeTween in Packages/manifest.json.");
                    return;
                }

                if (registryChanged)
                {
                    _statusLine = "Added PrimeTween scoped registry. Waiting for Unity Package Manager resolve...";
                    Debug.Log("[Evo Setup] Added PrimeTween scoped registry. Waiting for Unity Package Manager resolve...");
                    RefreshState();
                    QueueRefreshBurst();
                    return;
                }
            }

            _isInstalling = true;
            _statusLine = "Adding packages: " + string.Join(", ", packages.Select(GetPackageDisplayName));
            Debug.Log($"[Evo Setup] Adding selected packages in one Package Manager batch:\n{string.Join("\n", packages)}");
            _addAndRemoveRequest = Client.AddAndRemove(packages.ToArray(), Array.Empty<string>());
        }

        private static string GetPackageDisplayName(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "Unknown";
            }

            if (ContainsIgnoreCase(source, "VContainer")) return "VContainer";
            if (ContainsIgnoreCase(source, "UniTask")) return "UniTask";
            if (ContainsIgnoreCase(source, "NuGetForUnity")) return "NuGetForUnity";
            if (ContainsIgnoreCase(source, "R3.Unity")) return "R3.Unity";
            if (ContainsIgnoreCase(source, RuntimePackageName)) return "Evo Runtime";
            if (ContainsIgnoreCase(source, YandexPackageName)) return "Evo Yandex";
            if (ContainsIgnoreCase(source, PrimeTweenPackageName)) return "PrimeTween";
            if (ContainsIgnoreCase(source, AddressablesPackageName)) return "Addressables";
            if (ContainsIgnoreCase(source, LocalizationPackageName)) return "Localization";
            if (ContainsIgnoreCase(source, InputSystemPackageName)) return "Input System";
            if (ContainsIgnoreCase(source, UguiPackageName)) return "Unity UI";
            return source;
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   !string.IsNullOrWhiteSpace(value) &&
                   source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private List<string> CollectSelectedUpmPackagesToInstall()
        {
            var foundation = CollectFoundationPackagesToInstall();
            if (foundation.Count > 0)
            {
                return foundation;
            }

            if (_installReactiveNuGets && !AreReactiveAssembliesReady())
            {
                return new List<string>();
            }

            return CollectFrameworkPackagesToInstall();
        }

        private List<string> CollectFoundationPackagesToInstall()
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
            return packages;
        }

        private List<string> CollectFrameworkPackagesToInstall()
        {
            var packages = new List<string>();
            if (_installR3Unity && !_r3UnityInstalled) packages.Add(R3UnitySource);
            if (_installRuntimeModule &&
                (!_runtimeInstalled || _runtimeUpdateState == EvoPackageUpdateState.InstalledDifferentRevision) &&
                IsRuntimeInstallReady(packages))
            {
                packages.Add($"{RuntimeGitUrl}#{RuntimeGitTag}");
            }
            if (_installYandexModule && !_yandexInstalled && _runtimeInstalled) packages.Add($"{YandexGitUrl}#{YandexGitTag}");
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

        private void RemovePackage(string packageName, string displayName)
        {
            if (_isInstalling || _removeRequest != null || _addRequest != null || _addAndRemoveRequest != null)
            {
                return;
            }

            _isInstalling = true;
            _statusLine = $"Removing {displayName}...";
            Debug.Log($"[Evo Setup] Removing package: {packageName}");
            _removeRequest = Client.Remove(packageName);
        }

        private void RemoveReactiveNuGetPackages()
        {
            if (_isInstalling)
            {
                return;
            }

            var packagesConfigPath = GetNuGetPackagesConfigPath();
            if (!File.Exists(packagesConfigPath))
            {
                _statusLine = "Reactive NuGet packages.config not found.";
                Debug.LogWarning("[Evo Setup] Reactive NuGet packages.config not found.");
                RefreshState();
                return;
            }

            var document = XDocument.Load(packagesConfigPath);
            var root = document.Root;
            if (root == null)
            {
                return;
            }

            var removed = RemoveNuGetPackage(root, R3NuGetId) |
                          RemoveNuGetPackage(root, ObservableCollectionsNuGetId) |
                          RemoveNuGetPackage(root, ObservableCollectionsR3NuGetId);
            if (!removed)
            {
                _statusLine = "Reactive NuGet packages are not listed in packages.config.";
                Debug.Log("[Evo Setup] Reactive NuGet packages are not listed in packages.config.");
                RefreshState();
                return;
            }

            document.Save(packagesConfigPath);
            AssetDatabase.Refresh();
            _r3InPackagesConfig = false;
            _observableCollectionsInPackagesConfig = false;
            _observableCollectionsR3InPackagesConfig = false;
            _statusLine = "Removed reactive NuGet package entries. Run NuGet restore if physical assemblies remain.";
            Debug.Log("[Evo Setup] Removed reactive NuGet package entries from packages.config.");
            RefreshState();
        }

        private static bool RemoveNuGetPackage(XContainer root, string packageId)
        {
            var node = root.Elements("package")
                .FirstOrDefault(element =>
                {
                    var id = element.Attribute("id")?.Value;
                    return string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase);
                });
            if (node == null)
            {
                return false;
            }

            node.Remove();
            return true;
        }

        private void CreateProjectStructure()
        {
            EnsureProjectStructure();
            AssetDatabase.Refresh();
            _statusLine = "Project structure created.";
            RefreshState();
        }

        private void EnsureProjectStructure()
        {
            for (var i = 0; i < StructureFolders.Length; i++)
            {
                EnsureFolder(StructureFolders[i]);
            }
        }

        private void SetupStarterRuntimeScaffold()
        {
            Debug.Log($"[Evo Setup] Scaffold setup started. Templates root: {TemplatesRootPath}");
            ValidateTemplatesAndScaffoldScriptsState();
            if (!_templatesReady)
            {
                _statusLine = "Starter scaffold templates are invalid. Fix template issues before scaffold setup.";
                Debug.LogError("[Evo Setup] Starter scaffold templates are invalid. Fix template issues before scaffold setup.");
                _scaffoldSetupRequested = false;
                SessionState.SetBool(GetScaffoldStateKey(), false);
                return;
            }

            EnsureProjectStructure();
            EnsureFolder("Assets/_Project/Prefabs/Runtime");
            EnsureScene(EntryScenePath, "EntryPointRoot");
            EnsureScene(LoadingScenePath, "LoadingRoot");
            EnsureScene(TransitionScenePath, "TransitionRoot");
            EnsureScene(MenuScenePath, "MainMenuRoot");
            _statusLine = "Setup: syncing starter scaffold scripts from templates...";
            Debug.Log("[Evo Setup] Syncing starter scaffold scripts from templates...");
            EnsureStarterScripts();
            ValidateTemplatesAndScaffoldScriptsState();
            Debug.Log($"[Evo Setup] Scaffold scripts up to date after sync: {_scaffoldScriptsUpToDate}");

            if (!_scaffoldScriptsUpToDate)
            {
                UpdateScaffoldScriptsFromTemplates();
                QueueFinalizeStarterRuntimeScaffold();
                _scaffoldSetupRequested = true;
                SessionState.SetBool(GetScaffoldStateKey(), true);
                _statusLine = "Starter scaffold scripts updated. Waiting for Unity to compile before configuring scenes and assets...";
                Debug.Log("[Evo Setup] Starter scaffold scripts updated. Waiting for Unity to compile before configuring scenes and assets...");
                return;
            }

            AssetDatabase.Refresh();

            if (!AreStarterRuntimeTypesReady())
            {
                _statusLine = "Starter scripts created. Waiting for Unity to compile before configuring scenes and assets...";
                Debug.Log("[Evo Setup] Starter scripts created. Waiting for Unity to compile before configuring scenes and assets...");
                return;
            }

            FinalizeStarterRuntimeScaffold();
        }

        private void StartStarterRuntimeScaffold()
        {
            _scaffoldSetupRequested = true;
            SessionState.SetBool(GetScaffoldStateKey(), true);
            _bootstrapValidationAttempted = false;
            _scaffoldFinalizationAttempts = 0;
            SetupStarterRuntimeScaffold();
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
                _scaffoldSetupRequested = true;
                SessionState.SetBool(GetScaffoldStateKey(), true);
                _statusLine = "Waiting for starter runtime scripts to compile before configuring scaffold...";
                return;
            }

            FinalizeStarterRuntimeScaffold();
        }

        private void FinalizeStarterRuntimeScaffold()
        {
            _scaffoldFinalizationAttempts++;
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
            return FindTypeByName("Game.Runtime.EntryPoint.RuntimeProjectLifetimeScope") != null &&
                   FindTypeByName("Evo.Infrastructure.Runtime.Config.ProjectConfig") != null;
        }

        private void EnsureDefaultAssets()
        {
            CreateScriptableAsset(
                "Evo.Infrastructure.Runtime.Config.ProjectConfig, Assembly-CSharp",
                ProjectConfigPath);
            CreateScriptableAsset(
                "Evo.Infrastructure.Runtime.UI.UiSystemConfig, Evo.Infrastructure.Runtime",
                UiSystemConfigPath);
            EnsureConfigCatalogAsset();
            CreateScriptableAsset(
                "Evo.Infrastructure.Services.ResourceCatalog.ResourceCatalog, Evo.Infrastructure.Runtime",
                ResourceCatalogPath);
            CreateScriptableAsset(
                "Evo.Infrastructure.Services.Yandex.YandexRuntimeConfig, Assembly-CSharp",
                YandexRuntimeConfigPath);
            RebuildConfigCatalog();
            CreateLifetimeScopePrefab();
        }

        private void EnsureConfigCatalogAsset()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ConfigCatalogPath);
            if (catalog != null)
            {
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(LegacyConfigCatalogPath) != null)
                {
                    Debug.LogWarning(
                        $"[Evo Setup] Both config catalogs exist. Using canonical catalog '{ConfigCatalogPath}'. " +
                        $"Legacy catalog remains at '{LegacyConfigCatalogPath}'.");
                }

                return;
            }

            var legacyCatalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LegacyConfigCatalogPath);
            if (legacyCatalog != null)
            {
                var moveError = AssetDatabase.MoveAsset(LegacyConfigCatalogPath, ConfigCatalogPath);
                if (string.IsNullOrEmpty(moveError))
                {
                    Debug.Log($"[Evo Setup] Moved config catalog to canonical path: {ConfigCatalogPath}");
                    return;
                }

                Debug.LogWarning(
                    $"[Evo Setup] Failed to move legacy config catalog '{LegacyConfigCatalogPath}' to '{ConfigCatalogPath}': {moveError}");
                return;
            }

            CreateScriptableAsset(
                "Evo.Infrastructure.Services.Config.ScriptableConfigCatalog, Evo.Infrastructure.Runtime",
                ConfigCatalogPath);
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

        private static void RebuildConfigCatalog()
        {
            var catalog = LoadConfigCatalogAsset();
            if (catalog == null)
            {
                return;
            }

            var rebuildMethod = catalog.GetType().GetMethod("RebuildFromFolders", BindingFlags.Public | BindingFlags.Instance);
            if (rebuildMethod == null)
            {
                return;
            }

            rebuildMethod.Invoke(catalog, Array.Empty<object>());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private void CreateLifetimeScopePrefab()
        {
            if (File.Exists(LifetimeScopePrefabPath))
            {
                return;
            }

            var type = FindTypeByName("Game.Runtime.EntryPoint.RuntimeProjectLifetimeScope") ??
                       FindTypeByName("Game.Runtime.Bootstrap.RuntimeProjectLifetimeScope");
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
            ConfigureProjectLifetimeScopeReferences(root);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureProjectLifetimeScopeReferences(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var scope = root.GetComponent("RuntimeProjectLifetimeScope");
            if (scope == null)
            {
                return;
            }

            var serialized = new SerializedObject(scope);
            SetObjectReference(serialized.FindProperty("resourceCatalog"), AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ResourceCatalogPath));
            SetObjectReference(serialized.FindProperty("uiSystemConfig"), AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(UiSystemConfigPath));

            var configCatalogs = serialized.FindProperty("configCatalogs");
            var configCatalog = LoadConfigCatalogAsset();
            if (configCatalogs != null && configCatalog != null && configCatalogs.isArray)
            {
                configCatalogs.arraySize = 1;
                configCatalogs.GetArrayElementAtIndex(0).objectReferenceValue = configCatalog;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);
        }

        private static ScriptableObject LoadConfigCatalogAsset()
        {
            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(ConfigCatalogPath) ??
                   AssetDatabase.LoadAssetAtPath<ScriptableObject>(LegacyConfigCatalogPath);
        }

        private static void SetObjectReference(SerializedProperty property, UnityEngine.Object value)
        {
            if (property != null && value != null)
            {
                property.objectReferenceValue = value;
            }
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
                "Game.Runtime.Loading.LoadingSceneLifetimeScope",
                "SceneLifetimeScope");
            var loadingView = EnsureLoadingSceneView(scene);
            ConfigureLoadingSceneScopeReference(scene, loadingView);
            EditorSceneManager.SaveScene(scene);
        }

        private static Component EnsureLoadingSceneView(Scene scene)
        {
            var viewType = FindTypeByName("Game.Runtime.Loading.LoadingScreenView");
            if (viewType == null || !typeof(Component).IsAssignableFrom(viewType))
            {
                return null;
            }

            var existing = FindComponentInScene(scene, viewType);
            if (existing != null)
            {
                return existing;
            }

            var canvasRoot = GetOrCreateCanvasRoot(scene);
            var viewRoot = CreateRectTransform("LoadingScreenView", canvasRoot.transform);
            StretchToParent(viewRoot);
            var canvasGroup = viewRoot.gameObject.AddComponent<CanvasGroup>();
            var image = viewRoot.gameObject.AddComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 1f);

            var content = CreateRectTransform("Content", viewRoot);
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(520f, 140f);
            content.anchoredPosition = Vector2.zero;

            var title = CreateText("Title", content, "Loading", 32, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 0.68f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var slider = CreateSlider("Progress", content);
            var sliderTransform = (RectTransform)slider.transform;
            sliderTransform.anchorMin = new Vector2(0f, 0.34f);
            sliderTransform.anchorMax = new Vector2(1f, 0.54f);
            sliderTransform.offsetMin = Vector2.zero;
            sliderTransform.offsetMax = Vector2.zero;

            var message = CreateText("Message", content, string.Empty, 18, TextAnchor.MiddleCenter);
            message.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            message.rectTransform.anchorMax = new Vector2(0.72f, 0.28f);
            message.rectTransform.offsetMin = Vector2.zero;
            message.rectTransform.offsetMax = Vector2.zero;

            var percent = CreateText("Percent", content, "0%", 18, TextAnchor.MiddleRight);
            percent.rectTransform.anchorMin = new Vector2(0.76f, 0.08f);
            percent.rectTransform.anchorMax = new Vector2(1f, 0.28f);
            percent.rectTransform.offsetMin = Vector2.zero;
            percent.rectTransform.offsetMax = Vector2.zero;

            var view = viewRoot.gameObject.AddComponent(viewType);
            var serialized = new SerializedObject(view);
            SetObjectReference(serialized.FindProperty("slider"), slider);
            SetObjectReference(serialized.FindProperty("progressText"), percent);
            SetObjectReference(serialized.FindProperty("messageText"), message);
            SetObjectReference(serialized.FindProperty("canvasGroup"), canvasGroup);
            SetObjectReference(serialized.FindProperty("contentRoot"), content);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            return view;
        }

        private static void ConfigureLoadingSceneScopeReference(Scene scene, Component loadingView)
        {
            if (loadingView == null)
            {
                return;
            }

            var scope = FindComponentInScene(scene, FindTypeByName("Game.Runtime.Loading.LoadingSceneLifetimeScope"));
            if (scope == null)
            {
                return;
            }

            var serialized = new SerializedObject(scope);
            SetObjectReference(serialized.FindProperty("view"), loadingView);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);
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
                "Game.Runtime.MainMenu.MainMenuSceneLifetimeScope",
                "SceneLifetimeScope");
            EnsureParentReferenceTypeName(scope, ProjectScopeFullTypeName);
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
            GetOrCreateCanvasRoot(scene);
        }

        private static GameObject GetOrCreateCanvasRoot(Scene scene)
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
                return root;
            }

            var canvasRoot = new GameObject("Canvas");
            var createdCanvas = canvasRoot.AddComponent<Canvas>();
            EnsureCanvasSetup(canvasRoot, createdCanvas);
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            return canvasRoot;
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

        private static RectTransform CreateRectTransform(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var rect = CreateRectTransform(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                         Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            return label;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            var root = CreateRectTransform(name, parent);
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.2f, 0.24f, 1f);

            var fillArea = CreateRectTransform("Fill Area", root);
            StretchToParent(fillArea);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);

            var fill = CreateRectTransform("Fill", fillArea);
            StretchToParent(fill);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.22f, 0.72f, 1f, 1f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill;
            slider.targetGraphic = background;
            return slider;
        }

        private static Component FindComponentInScene(Scene scene, Type componentType)
        {
            if (!scene.IsValid() || componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren(componentType, true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
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

            var parentType = FindTypeByName(parentTypeName);
            if (parentType == null)
            {
                return false;
            }

            if (HasParentReferenceType(scope, parentType))
            {
                return false;
            }

            if (!TrySetParentReferenceType(scope, parentType))
            {
                return false;
            }

            EditorUtility.SetDirty(scope);
            return true;
        }

        private static bool HasParentReferenceType(Component scope, Type parentType)
        {
            if (scope == null || parentType == null)
            {
                return false;
            }

            var field = scope.GetType().GetField("parentReference", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(scope);
                if (value != null)
                {
                    var typeProperty = field.FieldType.GetProperty("Type", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (typeProperty?.GetValue(value) is Type type && type == parentType)
                    {
                        return true;
                    }

                    var typeNameField = field.FieldType.GetField("TypeName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (typeNameField?.GetValue(value) is string typeName &&
                        string.Equals(typeName, parentType.FullName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            var serialized = new SerializedObject(scope);
            var serializedTypeName = serialized.FindProperty("parentReference")?.FindPropertyRelative("TypeName");
            return serializedTypeName != null &&
                   string.Equals(serializedTypeName.stringValue, parentType.FullName, StringComparison.Ordinal);
        }

        private static bool TrySetParentReferenceType(Component scope, Type parentType)
        {
            var field = scope.GetType().GetField("parentReference", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                return false;
            }

            var parentReferenceType = field.FieldType;
            var constructor = parentReferenceType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);
            object parentReference;
            if (constructor != null)
            {
                parentReference = constructor.Invoke(new object[] { parentType });
            }
            else
            {
                parentReference = field.GetValue(scope) ?? Activator.CreateInstance(parentReferenceType);
                var typeNameField = parentReferenceType.GetField("TypeName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                typeNameField?.SetValue(parentReference, parentType.FullName);
            }

            field.SetValue(scope, parentReference);
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
            if (scope != null && EnsureParentReferenceTypeName(scope, ProjectScopeFullTypeName))
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
                "Game.Runtime.MainMenu.MainMenuSceneLifetimeScope",
                "SceneLifetimeScope");
            ValidateAndFixSceneScope(
                LoadingScenePath,
                "Context",
                "LoadingRoot",
                fixedItems,
                issues,
                "Game.Runtime.Loading.LoadingSceneLifetimeScope",
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

            var projectScopeType = FindTypeByName(ProjectScopeFullTypeName);
            if (projectScopeType == null || !HasParentReferenceType(scope, projectScopeType))
            {
                issues.Add($"{Path.GetFileNameWithoutExtension(scenePath)} scope parent is not '{ProjectScopeFullTypeName}'.");
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

            if (TryAddComponentByTypeName(target, ProjectScopeFullTypeName))
            {
                return;
            }

            TryAddComponentByTypeName(target, LegacyProjectScopeFullTypeName);
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

            var settings = GetAddressableSettings(true);
            if (settings == null)
            {
                Debug.LogError("[Evo Setup] Addressables settings are unavailable. Install Addressables before creating starter scaffold.");
                return;
            }

            var defaultGroup = GetAddressableDefaultGroup(settings);
            if (defaultGroup == null)
            {
                Debug.LogError("[Evo Setup] Addressables default group is unavailable.");
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
                Debug.LogError("[Evo Setup] Addressables CreateOrMoveEntry API was not found.");
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
                args = new object[] { sceneGuid, defaultGroup, false, true };
            }

            var entry = createOrMoveEntryMethod.Invoke(settings, args);
            if (entry == null)
            {
                Debug.LogError($"[Evo Setup] Failed to create Addressables entry for {scenePath}.");
                return;
            }

            var addressProperty = entry.GetType().GetProperty("address", BindingFlags.Public | BindingFlags.Instance);
            if (addressProperty != null && addressProperty.CanWrite)
            {
                addressProperty.SetValue(entry, string.IsNullOrWhiteSpace(address)
                    ? Path.GetFileNameWithoutExtension(scenePath)
                    : address);
            }

            MarkAddressableSettingsDirty(settings, entry);
            EditorUtility.SetDirty((UnityEngine.Object)settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static object GetAddressableSettings(bool create)
        {
            var settingsType = FindTypeByName("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject");
            if (settingsType == null)
            {
                return null;
            }

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

            var settings = getSettingsMethod?.Invoke(null, new object[] { create });
            if (settings != null)
            {
                return settings;
            }

            var settingsProperty = settingsType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Static);
            return settingsProperty?.GetValue(null);
        }

        private static object GetAddressableDefaultGroup(object settings)
        {
            var defaultGroupProperty = settings.GetType().GetProperty("DefaultGroup", BindingFlags.Public | BindingFlags.Instance);
            var defaultGroup = defaultGroupProperty?.GetValue(settings);
            if (defaultGroup != null)
            {
                return defaultGroup;
            }

            var groupsProperty = settings.GetType().GetProperty("groups", BindingFlags.Public | BindingFlags.Instance);
            var groups = groupsProperty?.GetValue(settings) as System.Collections.IEnumerable;
            if (groups == null)
            {
                return null;
            }

            foreach (var group in groups)
            {
                if (group != null)
                {
                    return group;
                }
            }

            return null;
        }

        private static object FindAddressableEntry(object settings, string guid)
        {
            if (settings == null || string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var findMethod = settings.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "FindAssetEntry", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var p = m.GetParameters();
                    return p.Length == 1 && p[0].ParameterType == typeof(string);
                });

            return findMethod?.Invoke(settings, new object[] { guid });
        }

        private static void MarkAddressableSettingsDirty(object settings, object entry)
        {
            var modificationEventType = settings.GetType().GetNestedType("ModificationEvent", BindingFlags.Public);
            var setDirtyMethod = settings.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "SetDirty", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var p = m.GetParameters();
                    return p.Length >= 3 &&
                           modificationEventType != null &&
                           p[0].ParameterType == modificationEventType &&
                           p[1].ParameterType == typeof(object) &&
                           p[2].ParameterType == typeof(bool);
                });

            if (modificationEventType == null || setDirtyMethod == null)
            {
                return;
            }

            var modificationEvent = Enum.Parse(modificationEventType, "EntryModified");
            var parameters = setDirtyMethod.GetParameters();
            var args = parameters.Length >= 4
                ? new object[] { modificationEvent, entry, true, true }
                : new object[] { modificationEvent, entry, true };
            setDirtyMethod.Invoke(settings, args);
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

            if (_removeRequest != null)
            {
                if (!_removeRequest.IsCompleted)
                {
                    _isInstalling = true;
                    return;
                }

                if (_removeRequest.Status == StatusCode.Success)
                {
                    _statusLine = "Package removed. Waiting for Unity refresh...";
                    Debug.Log("[Evo Setup] Package removed.");
                }
                else
                {
                    if (IsInterruptedPackageRequest(_removeRequest.Error))
                    {
                        _statusLine = "Package remove was interrupted by Unity refresh. Re-analyzing...";
                        Debug.LogWarning("[Evo Setup] Package remove was interrupted by Unity refresh. Re-analyzing setup state.");
                        _removeRequest = null;
                        _isInstalling = false;
                        RefreshState();
                        QueueRefreshBurst();
                        Repaint();
                        return;
                    }

                    var message = GetRequestErrorMessage(_removeRequest.Error);
                    _statusLine = $"Package remove failed: {message}";
                    Debug.LogError($"[Evo Setup] Package remove failed: {message}");
                }

                _removeRequest = null;
                _isInstalling = false;
                RefreshState();
                QueueRefreshBurst();
                Repaint();
                return;
            }

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
                    if (IsInterruptedPackageRequest(_addAndRemoveRequest.Error))
                    {
                        _statusLine = "Package setup was interrupted by Unity refresh. Re-analyzing...";
                        Debug.LogWarning("[Evo Setup] Package setup was interrupted by Unity refresh. Re-analyzing setup state.");
                        _addAndRemoveRequest = null;
                        _isInstalling = false;
                        RefreshState();
                        QueueRefreshBurst();
                        Repaint();
                        return;
                    }

                    var message = GetRequestErrorMessage(_addAndRemoveRequest.Error);
                    _statusLine = $"Package setup failed: {message}";
                    Debug.LogError($"[Evo Setup] Package setup failed: {message}");
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
                    if (IsInterruptedPackageRequest(_addRequest.Error))
                    {
                        _statusLine = "Package install was interrupted by Unity refresh. Re-analyzing...";
                        Debug.LogWarning("[Evo Setup] Package install was interrupted by Unity refresh. Re-analyzing setup state.");
                        _addRequest = null;
                        _isInstalling = false;
                        RefreshState();
                        QueueRefreshBurst();
                        Repaint();
                        return;
                    }

                    var message = GetRequestErrorMessage(_addRequest.Error);
                    _statusLine = $"Install failed: {message}";
                    Debug.LogError($"[Evo Setup] Package install failed: {message}");
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
            ContinueScaffoldSetup();
        }

        private void ClearTransientPackageRequests()
        {
            _addRequest = null;
            _addAndRemoveRequest = null;
            _removeRequest = null;
            _listRequest = null;
            _installQueue.Clear();
            _isInstalling = false;
            _isRefreshingState = false;
        }

        private void ContinueScaffoldSetup()
        {
            if (!_scaffoldSetupRequested)
            {
                return;
            }

            if (_isRefreshingState || EditorApplication.isCompiling || EditorApplication.isUpdating || _scaffoldFinalizeQueued)
            {
                return;
            }

            ValidateTemplatesAndScaffoldScriptsState();
            if (!_templatesReady)
            {
                _scaffoldSetupRequested = false;
                SessionState.SetBool(GetScaffoldStateKey(), false);
                _statusLine = "Scaffold stopped: templates are invalid.";
                Debug.LogError("[Evo Setup] Scaffold templates are invalid.");
                Repaint();
                return;
            }

            if (HasStarterScaffoldFiles() && !_scaffoldScriptsUpToDate)
            {
                _statusLine = "Scaffold: replacing outdated starter scripts from templates...";
                UpdateScaffoldScriptsFromTemplates();
                QueueFinalizeStarterRuntimeScaffold();
                return;
            }

            if (!HasStarterScaffold())
            {
                _statusLine = "Scaffold: creating starter files and scenes...";
                SetupStarterRuntimeScaffold();
                return;
            }

            if (!AreStarterRuntimeTypesReady())
            {
                _statusLine = "Scaffold: waiting for starter runtime scripts to compile...";
                return;
            }

            if (!_bootstrapScopesReady)
            {
                if (_bootstrapValidationAttempted)
                {
                    _scaffoldSetupRequested = false;
                    SessionState.SetBool(GetScaffoldStateKey(), false);
                    _statusLine = "Scaffold stopped: bootstrap scopes are still invalid after auto-fix.";
                    Debug.LogError("[Evo Setup] Bootstrap scopes are still invalid after auto-fix.");
                    Repaint();
                    return;
                }

                _bootstrapValidationAttempted = true;
                _statusLine = "Scaffold: validating bootstrap scopes...";
                ValidateAndFixBootstrapScopes();
                RefreshState();
                return;
            }

            if (!_starterAddressablesReady || !_starterBuildScenesReady)
            {
                if (_scaffoldFinalizationAttempts >= 2)
                {
                    _scaffoldSetupRequested = false;
                    SessionState.SetBool(GetScaffoldStateKey(), false);
                    _statusLine = "Scaffold stopped: Addressables or build settings are still invalid after finalization.";
                    Debug.LogError("[Evo Setup] Addressables or build settings are still invalid after scaffold finalization.");
                    Repaint();
                    return;
                }

                _statusLine = "Scaffold: finalizing Addressables and build settings...";
                FinalizeStarterRuntimeScaffold();
                return;
            }

            _scaffoldSetupRequested = false;
            _bootstrapValidationAttempted = false;
            _scaffoldFinalizationAttempts = 0;
            SessionState.SetBool(GetScaffoldStateKey(), false);
            OpenEntryPointScene();
            _statusLine = "Starter scaffold ready.";
            Repaint();
        }

        private void CancelSetup()
        {
            ClearTransientPackageRequests();
            ClearSetupSessionState();
            _oneClickSetupRequested = false;
            _scaffoldSetupRequested = false;
            _odinImportRequested = false;
            _scaffoldFinalizeQueued = false;
            _reactiveRestoreRequested = false;
            _bootstrapValidationAttempted = false;
            _scaffoldFinalizationAttempts = 0;
            _statusLine = "Setup canceled.";
            EditorUtility.ClearProgressBar();
            Repaint();
        }

        private static void ClearSetupSessionState()
        {
            SessionState.SetBool(GetOneClickStateKey(), false);
            SessionState.SetBool(GetScaffoldStateKey(), false);
            SessionState.SetBool(GetOdinImportStateKey(), false);
        }

        private bool IsInterruptedPackageRequest(Error error)
        {
            return error == null && _oneClickSetupRequested;
        }

        private static string GetRequestErrorMessage(Error error)
        {
            if (error == null)
            {
                return "Package Manager request failed without an error message. Unity may have interrupted the request during domain reload or package resolve.";
            }

            return string.IsNullOrWhiteSpace(error.message)
                ? $"Package Manager request failed with error code {error.errorCode}."
                : error.message;
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

            _stateAnalyzed = false;
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
                RefreshRuntimePackageState(packages);
                _yandexInstalled = HasAnyPackage(packages, YandexPackageName, "com.evo.infrastructure.yandex") ||
                                   ManifestHasAnyDependency(YandexPackageName);
                _odinInstalled = IsOdinInstalled();
                if (_odinInstalled)
                {
                    _odinImportRequested = false;
                    SessionState.SetBool(GetOdinImportStateKey(), false);
                }
                ReadReactivePackagesConfig(
                    out _r3InPackagesConfig,
                    out _observableCollectionsInPackagesConfig,
                    out _observableCollectionsR3InPackagesConfig);
                _r3Ready = _r3InPackagesConfig || _r3UnityInstalled || IsAssemblyLoaded("R3");
                _observableCollectionsReady = _observableCollectionsInPackagesConfig || IsAssemblyLoaded("ObservableCollections");
                _observableCollectionsR3Ready = _observableCollectionsR3InPackagesConfig || IsAssemblyLoaded("ObservableCollections.R3");
                if (AreReactiveAssembliesReady())
                {
                    _reactiveRestoreRequested = false;
                }
            }
            else
            {
                var message = GetRequestErrorMessage(_listRequest.Error);
                _statusLine = $"Package analysis failed: {message}";
                Debug.LogError($"[Evo Setup] Package analysis failed: {message}");
            }

            _structureReady = HasProjectStructure();
            _odinInstalled = IsOdinInstalled();
            ValidateTemplatesAndScaffoldScriptsState();
            _bootstrapScopesReady = AreBootstrapScopesValid();
            _starterAddressablesReady = AreStarterScenesAddressable();
            _starterBuildScenesReady = AreStarterBuildScenesReady();
            _stateAnalyzed = _listRequest.Status == StatusCode.Success;
            _isRefreshingState = false;
            ContinueOneClickSetup();
            ContinueScaffoldSetup();
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

            return text.IndexOf($"TypeName: {ProjectScopeFullTypeName}", StringComparison.Ordinal) >= 0 ||
                   text.IndexOf($"TypeName: {LegacyProjectScopeFullTypeName}", StringComparison.Ordinal) >= 0 ||
                   text.IndexOf("TypeName: RuntimeProjectLifetimeScope", StringComparison.Ordinal) >= 0;
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
            if (!_isInstalling &&
                !_scaffoldSetupRequested &&
                !_scaffoldFinalizeQueued &&
                _addRequest == null &&
                _addAndRemoveRequest == null &&
                _removeRequest == null &&
                _installQueue.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Processing...", EditorStyles.boldLabel);
            var label = _scaffoldSetupRequested || _scaffoldFinalizeQueued
                ? "Configuring starter scaffold..."
                : _addAndRemoveRequest != null
                ? "Adding selected packages..."
                : _removeRequest != null
                    ? "Removing package..."
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
            return HasStarterScaffoldFiles();
        }

        private bool IsStarterScaffoldReady()
        {
            return HasStarterScaffold() &&
                   _scaffoldScriptsUpToDate &&
                   AreStarterRuntimeTypesReady() &&
                   _bootstrapScopesReady &&
                   _starterAddressablesReady &&
                   _starterBuildScenesReady;
        }

        private static bool HasStarterScaffoldFiles()
        {
            return File.Exists(EntryScenePath) &&
                   File.Exists(LoadingScenePath) &&
                   File.Exists(TransitionScenePath) &&
                   File.Exists(MenuScenePath) &&
                   File.Exists(StarterRuntimeProjectLifetimeScopePath) &&
                   File.Exists(StarterRuntimeEntryPointPath) &&
                   File.Exists(StarterLoadingSceneLifetimeScopePath) &&
                   File.Exists(StarterProjectConfigPath) &&
                   File.Exists(StarterLoadingViewSystemPath) &&
                   File.Exists(StarterLoadingViewModelPath) &&
                   File.Exists(StarterLoadingScreenViewPath);
        }

        private static bool AreStarterBuildScenesReady()
        {
            var scenes = EditorBuildSettings.scenes;
            return scenes.Length >= 2 &&
                   scenes[0].enabled &&
                   scenes[1].enabled &&
                   string.Equals(scenes[0].path, EntryScenePath, StringComparison.Ordinal) &&
                   string.Equals(scenes[1].path, TransitionScenePath, StringComparison.Ordinal);
        }

        private static bool AreStarterScenesAddressable()
        {
            return HasAddressableEntry(LoadingScenePath, "LoadingScene") &&
                   HasAddressableEntry(MenuScenePath, "MainMenuScene");
        }

        private static bool HasAddressableEntry(string scenePath, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            var settings = GetAddressableSettings(false);
            var entry = FindAddressableEntry(settings, guid);
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                return true;
            }

            var addressProperty = entry.GetType().GetProperty("address", BindingFlags.Public | BindingFlags.Instance);
            var actualAddress = addressProperty?.GetValue(entry) as string;
            return string.Equals(actualAddress, address, StringComparison.Ordinal);
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
            _scaffoldScriptsUpToDate = true;

            for (var i = 0; i < StarterScriptTemplates.Length; i++)
            {
                var templatePath = GetTemplatePath(StarterScriptTemplates[i].TemplateFileName);
                if (!File.Exists(templatePath))
                {
                    _templatesReady = false;
                    _templateValidationIssues.Add($"Missing template: {templatePath}");
                }

                if (!IsScaffoldScriptUpToDate(StarterScriptTemplates[i]))
                {
                    _scaffoldScriptsUpToDate = false;
                }
            }

            _scaffoldScriptsUpToDate = _templatesReady && _scaffoldScriptsUpToDate;
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
                Debug.Log($"[Evo Setup] Scaffold script missing: {template.TargetPath}");
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
                var upToDate = string.Equals(templateText, targetText, StringComparison.Ordinal);
                if (!upToDate)
                {
                    Debug.Log(
                        $"[Evo Setup] Scaffold script differs from template: {template.TargetPath}. " +
                        $"Template={templatePath} ({GetFileLength(templatePath)} bytes), " +
                        $"Target={GetFileLength(template.TargetPath)} bytes.");
                }

                return upToDate;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Evo Setup] Failed to compare scaffold script '{template.TargetPath}': {ex.Message}");
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
            Debug.Log("[Evo Setup] Force-updating scaffold scripts from templates...");
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
                _odinImportRequested = false;
                SessionState.SetBool(GetOdinImportStateKey(), false);
                _statusLine = "Odin Inspector is already installed.";
                return true;
            }

            var path = interactive ? ResolveOdinPackagePath() : FindEmbeddedOdinPackagePath();
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
                _statusLine = interactive ? "Odin import canceled." : $"Odin package file not found. Put Odin .unitypackage into {EmbeddedOdinPackageFolder}.";
                if (!interactive)
                {
                    Debug.LogError("[Evo Setup] " + _statusLine);
                }

                return false;
            }

            if (string.IsNullOrWhiteSpace(FindEmbeddedOdinPackagePath()))
            {
                EditorPrefs.SetString(OdinPackagePathPrefsKey, path);
            }

            _statusLine = $"Importing Odin package: {Path.GetFileName(path)}";
            Debug.Log("[Evo Setup] " + _statusLine);
            _odinImportRequested = true;
            SessionState.SetBool(GetOdinImportStateKey(), true);
            AssetDatabase.ImportPackage(path, interactive);
            if (!interactive)
            {
                AssetDatabase.Refresh();
            }

            _odinInstalled = IsOdinInstalled();
            if (_odinInstalled)
            {
                _odinImportRequested = false;
                SessionState.SetBool(GetOdinImportStateKey(), false);
            }

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

        private void RefreshRuntimePackageState(IReadOnlyList<UnityEditor.PackageManager.PackageInfo> packages)
        {
            var package = FindPackage(packages, RuntimePackageName, "com.evo.infrastructure.runtime");
            _runtimeManifestDependency = GetManifestDependencyValue(RuntimePackageName);
            _runtimeInstalled = package != null || !string.IsNullOrWhiteSpace(_runtimeManifestDependency);
            _runtimeInstalledVersion = package != null ? package.version ?? string.Empty : string.Empty;
            _runtimeInstalledPackageId = package != null ? package.packageId ?? string.Empty : string.Empty;
            _runtimeUpdateState = ResolveEvoPackageUpdateState(
                _runtimeInstalled,
                _runtimeInstalledVersion,
                _runtimeInstalledPackageId,
                _runtimeManifestDependency,
                RuntimeGitTag);
        }

        private static EvoPackageUpdateState ResolveEvoPackageUpdateState(
            bool installed,
            string version,
            string packageId,
            string manifestDependency,
            string targetTag)
        {
            if (!installed)
            {
                return EvoPackageUpdateState.Missing;
            }

            var targetVersion = targetTag != null && targetTag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? targetTag.Substring(1)
                : targetTag;
            if (EqualsIgnoreCase(version, targetVersion) ||
                ContainsIgnoreCase(packageId, targetTag) ||
                ContainsIgnoreCase(manifestDependency, targetTag))
            {
                return EvoPackageUpdateState.InstalledTarget;
            }

            return EvoPackageUpdateState.InstalledDifferentRevision;
        }

        private static UnityEditor.PackageManager.PackageInfo FindPackage(
            IReadOnlyList<UnityEditor.PackageManager.PackageInfo> packages,
            params string[] candidates)
        {
            if (packages == null || packages.Count == 0 || candidates == null || candidates.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < packages.Count; i++)
            {
                var package = packages[i];
                if (PackageMatches(package, candidates))
                {
                    return package;
                }
            }

            return null;
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
                if (PackageMatches(package, candidates))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PackageMatches(UnityEditor.PackageManager.PackageInfo package, params string[] candidates)
        {
            if (package == null || candidates == null || candidates.Length == 0)
            {
                return false;
            }

            var name = package.name ?? string.Empty;
            var packageId = package.packageId ?? string.Empty;
            var resolvedPath = package.resolvedPath ?? string.Empty;

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

            return false;
        }

        private static string GetManifestDependencyValue(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return string.Empty;
            }

            var path = Path.Combine(GetProjectRootPath(), "Packages", "manifest.json");
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch
            {
                return string.Empty;
            }

            var nameIndex = json.IndexOf($"\"{packageName}\"", StringComparison.OrdinalIgnoreCase);
            if (nameIndex < 0)
            {
                return string.Empty;
            }

            var colonIndex = json.IndexOf(':', nameIndex);
            if (colonIndex < 0)
            {
                return string.Empty;
            }

            var valueStart = json.IndexOf('"', colonIndex + 1);
            if (valueStart < 0)
            {
                return string.Empty;
            }

            var valueEnd = json.IndexOf('"', valueStart + 1);
            if (valueEnd <= valueStart)
            {
                return string.Empty;
            }

            return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
        }

        private static bool EqualsIgnoreCase(string source, string value)
        {
            return string.Equals(source, value, StringComparison.OrdinalIgnoreCase);
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

        private static bool EnsurePrimeTweenScopedRegistry(out bool changed)
        {
            changed = false;
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
                Debug.Log("[Evo Setup] Added PrimeTween scoped registry to Packages/manifest.json.");
                changed = true;
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
            Debug.Log("[Evo Setup] Added PrimeTween scoped registry to Packages/manifest.json.");
            changed = true;
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
                SyncStarterScriptFromTemplate(
                    StarterScriptTemplates[i].TargetPath,
                    StarterScriptTemplates[i].TemplateFileName);
            }
        }

        private static void SyncStarterScriptFromTemplate(string targetPath, string templateFileName)
        {
            var templatePath = GetTemplatePath(templateFileName);
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[Evo Setup] Missing scaffold template: {templatePath}");
                return;
            }

            var templateText = File.ReadAllText(templatePath);
            if (File.Exists(targetPath))
            {
                var targetText = File.ReadAllText(targetPath);
                if (string.Equals(NormalizeText(templateText), NormalizeText(targetText), StringComparison.Ordinal))
                {
                    Debug.Log($"[Evo Setup] Scaffold script already matches template: {targetPath}");
                    return;
                }

                if (LooksLikeLegacyMinimalScope(targetText))
                {
                    Debug.LogWarning($"[Evo Setup] Legacy/minimal scaffold script detected and will be replaced: {targetPath}");
                }

                Debug.Log(
                    $"[Evo Setup] Replacing scaffold script from template: {targetPath}. " +
                    $"Template={templatePath} ({GetTextByteCount(templateText)} bytes), " +
                    $"ExistingTarget={GetTextByteCount(targetText)} bytes.");
            }
            else
            {
                Debug.Log(
                    $"[Evo Setup] Creating scaffold script from template: {targetPath}. " +
                    $"Template={templatePath} ({GetTextByteCount(templateText)} bytes).");
            }

            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(targetPath, templateText);
            Debug.Log($"[Evo Setup] Synced scaffold script from template: {targetPath}. NewTarget={GetFileLength(targetPath)} bytes.");
        }

        private static bool LooksLikeLegacyMinimalScope(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.IndexOf("public class RuntimeProjectLifetimeScope", StringComparison.Ordinal) >= 0 ||
                   text.IndexOf("public class LoadingSceneLifetimeScope", StringComparison.Ordinal) >= 0;
        }

        private static long GetFileLength(string path)
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0L;
        }

        private static int GetTextByteCount(string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : Encoding.UTF8.GetByteCount(text);
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
            SaveSelectionState();
            _oneClickSetupRequested = true;
            _bootstrapValidationAttempted = false;
            _scaffoldFinalizationAttempts = 0;
            SessionState.SetBool(GetOneClickStateKey(), true);
            Debug.Log("[Evo Setup] Setup started.");
            _statusLine = "Setup started.";
            RefreshState();
            ContinueOneClickSetup();
        }

        private void LoadSelectionState()
        {
            _installVContainer = GetSelection(nameof(_installVContainer), _installVContainer);
            _installUniTask = GetSelection(nameof(_installUniTask), _installUniTask);
            _installNuGetForUnity = GetSelection(nameof(_installNuGetForUnity), _installNuGetForUnity);
            _installAddressables = GetSelection(nameof(_installAddressables), _installAddressables);
            _installLocalization = GetSelection(nameof(_installLocalization), _installLocalization);
            _installInputSystem = GetSelection(nameof(_installInputSystem), _installInputSystem);
            _installUgui = GetSelection(nameof(_installUgui), _installUgui);
            _installPrimeTween = GetSelection(nameof(_installPrimeTween), _installPrimeTween);
            _installR3Unity = GetSelection(nameof(_installR3Unity), _installR3Unity);
            _installReactiveNuGets = GetSelection(nameof(_installReactiveNuGets), _installReactiveNuGets);
            _installRuntimeModule = GetSelection(nameof(_installRuntimeModule), _installRuntimeModule);
            _installYandexModule = GetSelection(nameof(_installYandexModule), _installYandexModule);
            _installOdinPackage = GetSelection(nameof(_installOdinPackage), _installOdinPackage);
            _installProjectStructure = GetSelection(nameof(_installProjectStructure), _installProjectStructure);
            _installStarterScaffold = GetSelection(nameof(_installStarterScaffold), _installStarterScaffold);
        }

        private void SaveSelectionState()
        {
            SetSelection(nameof(_installVContainer), _installVContainer);
            SetSelection(nameof(_installUniTask), _installUniTask);
            SetSelection(nameof(_installNuGetForUnity), _installNuGetForUnity);
            SetSelection(nameof(_installAddressables), _installAddressables);
            SetSelection(nameof(_installLocalization), _installLocalization);
            SetSelection(nameof(_installInputSystem), _installInputSystem);
            SetSelection(nameof(_installUgui), _installUgui);
            SetSelection(nameof(_installPrimeTween), _installPrimeTween);
            SetSelection(nameof(_installR3Unity), _installR3Unity);
            SetSelection(nameof(_installReactiveNuGets), _installReactiveNuGets);
            SetSelection(nameof(_installRuntimeModule), _installRuntimeModule);
            SetSelection(nameof(_installYandexModule), _installYandexModule);
            SetSelection(nameof(_installOdinPackage), _installOdinPackage);
            SetSelection(nameof(_installProjectStructure), _installProjectStructure);
            SetSelection(nameof(_installStarterScaffold), _installStarterScaffold);
        }

        private static bool GetSelection(string key, bool defaultValue)
        {
            return SessionState.GetBool(GetSelectionStateKey(key), defaultValue);
        }

        private static void SetSelection(string key, bool value)
        {
            SessionState.SetBool(GetSelectionStateKey(key), value);
        }

        private void ResumeOneClickSetupIfNeeded()
        {
            _oneClickSetupRequested = SessionState.GetBool(GetOneClickStateKey(), false);
            _scaffoldSetupRequested = SessionState.GetBool(GetScaffoldStateKey(), false);
            _odinImportRequested = SessionState.GetBool(GetOdinImportStateKey(), false);
            if (_oneClickSetupRequested)
            {
                _statusLine = "Resuming setup after domain reload...";
                Debug.Log("[Evo Setup] Resumed setup after domain reload.");
            }

            if (_scaffoldSetupRequested)
            {
                _statusLine = "Resuming scaffold setup after domain reload...";
                Debug.Log("[Evo Setup] Resumed scaffold setup after domain reload.");
            }
        }

        private void ContinueOneClickSetup()
        {
            if (!_oneClickSetupRequested)
            {
                return;
            }

            if (_isInstalling || _addRequest != null || _addAndRemoveRequest != null || _removeRequest != null || _isRefreshingState)
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

            if (_scaffoldSetupRequested)
            {
                return;
            }

            if (HasSelectedUpmPackagesToInstall())
            {
                InstallSelectedUpmPackagesBatch();
                return;
            }

            if (_installReactiveNuGets && !AreReactiveAssembliesReady())
            {
                if (!_nuGetForUnityInstalled && !AreReactivePackagesConfigReady())
                {
                    _oneClickSetupRequested = false;
                    SessionState.SetBool(GetOneClickStateKey(), false);
                    EditorUtility.ClearProgressBar();
                    _statusLine = "Setup stopped: Reactive NuGets require NuGetForUnity. Enable NuGetForUnity or disable Reactive NuGets.";
                    Debug.LogError("[Evo Setup] Reactive NuGets require NuGetForUnity. Enable NuGetForUnity or disable Reactive NuGets.");
                    Repaint();
                    return;
                }

                if (AreReactivePackagesConfigReady())
                {
                    _statusLine = "Setup: waiting for NuGet restore/import of reactive assemblies...";
                    if (!_reactiveRestoreRequested)
                    {
                        _reactiveRestoreRequested = true;
                        Debug.Log("[Evo Setup] Waiting for NuGet restore/import of R3 reactive assemblies...");
                        TryInvokeNuGetRestore();
                        QueueRefreshBurst();
                    }

                    return;
                }

                _statusLine = "Setup: configuring reactive NuGet dependencies...";
                Debug.Log("[Evo Setup] Configuring reactive NuGet dependencies...");
                InstallReactiveFromNuGet();
                RefreshState();
                return;
            }

            if (_installR3Unity && !_r3UnityInstalled && !AreReactiveAssembliesReady())
            {
                StopSetupWithError("Setup stopped: R3.Unity requires imported R3 assemblies. Wait for NuGet restore/import or run NuGet restore manually.");
                return;
            }

            if (_installRuntimeModule && !_runtimeInstalled && !IsRuntimeInstallReady(null))
            {
                StopSetupWithError("Setup stopped: Runtime package prerequisites are not ready. Install selected runtime dependencies first.");
                return;
            }

            if (_installYandexModule && !_yandexInstalled && !_runtimeInstalled)
            {
                StopSetupWithError("Setup stopped: Yandex package requires Evo Infrastructure Runtime first.");
                return;
            }

            if (_installOdinPackage && !_odinInstalled)
            {
                if (_odinImportRequested)
                {
                    if (IsOdinInstalled())
                    {
                        _odinInstalled = true;
                        _odinImportRequested = false;
                        SessionState.SetBool(GetOdinImportStateKey(), false);
                        _statusLine = "Setup: Odin import completed.";
                        RefreshState();
                        return;
                    }

                    _statusLine = "Setup: waiting for Odin import to finish...";
                    return;
                }

                if (!TryImportOdinPackage(false))
                {
                    StopSetupWithError($"Setup stopped: Odin package file not found. Put Odin .unitypackage into {EmbeddedOdinPackageFolder}.");
                    return;
                }

                _statusLine = "Setup: importing Odin package...";
                QueueRefreshBurst();
                return;
            }

            if (_installProjectStructure && !_structureReady)
            {
                _statusLine = "Setup: creating project structure...";
                Debug.Log("[Evo Setup] Creating project structure...");
                CreateProjectStructure();
                return;
            }

            if (_installStarterScaffold && !IsStarterScaffoldReady())
            {
                if (!ArePackagesReadyForStarterScaffold())
                {
                    StopSetupWithError("Setup stopped: starter scaffold requires selected runtime packages to be installed and compiled.");
                    return;
                }

                _statusLine = "Setup: creating starter scaffold...";
                Debug.Log("[Evo Setup] Creating starter scaffold...");
                StartStarterRuntimeScaffold();
                return;
            }

            _oneClickSetupRequested = false;
            _bootstrapValidationAttempted = false;
            SessionState.SetBool(GetOneClickStateKey(), false);
            _statusLine = "Setup completed.";
            Debug.Log("[Evo Setup] Setup completed.");
            EditorUtility.ClearProgressBar();
            Repaint();
        }

        private void StopSetupWithError(string message)
        {
            _oneClickSetupRequested = false;
            SessionState.SetBool(GetOneClickStateKey(), false);
            EditorUtility.ClearProgressBar();
            _statusLine = message;
            Debug.LogError("[Evo Setup] " + message);
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
            if (_installOdinPackage) count++;
            if (_installProjectStructure) count++;
            if (_installStarterScaffold) count++;

            return Mathf.Max(1, count);
        }

        private int GetOneClickCompletedStepCount()
        {
            var completed = 0;
            if (IsSelectedUpmPackagesReady()) completed++;
            if (_installReactiveNuGets && AreReactiveAssembliesReady()) completed++;
            if (_installOdinPackage && _odinInstalled) completed++;
            if (_installProjectStructure && _structureReady) completed++;
            if (_installStarterScaffold && IsStarterScaffoldReady()) completed++;
            return completed;
        }

        private bool HasSelectedUpmPackagesToInstall()
        {
            return CollectSelectedUpmPackagesToInstall().Count > 0;
        }

        private bool HasSelectedSetupWork()
        {
            return HasSelectedUpmPackagesToInstall() ||
                   (_installReactiveNuGets && !AreReactiveAssembliesReady()) ||
                   (_installOdinPackage && !_odinInstalled) ||
                   (_installProjectStructure && !_structureReady) ||
                   (_installStarterScaffold && !IsStarterScaffoldReady());
        }

        private bool AreReactivePackagesConfigReady()
        {
            return _r3InPackagesConfig && _observableCollectionsInPackagesConfig && _observableCollectionsR3InPackagesConfig;
        }

        private static bool AreReactiveAssembliesReady()
        {
            return IsAssemblyLoaded("R3") &&
                   IsAssemblyLoaded("ObservableCollections") &&
                   IsAssemblyLoaded("ObservableCollections.R3");
        }

        private bool IsRuntimeInstallReady(ICollection<string> packagesBeingAdded)
        {
            var r3UnityWillBeReady = _r3UnityInstalled ||
                                     (packagesBeingAdded != null && packagesBeingAdded.Contains(R3UnitySource));
            return (!_installVContainer || _vContainerInstalled) &&
                   (!_installUniTask || _uniTaskInstalled) &&
                   (!_installAddressables || _addressablesInstalled) &&
                   (!_installLocalization || _localizationInstalled) &&
                   (!_installInputSystem || _inputSystemInstalled) &&
                   (!_installUgui || _uguiInstalled) &&
                   (!_installPrimeTween || _primeTweenInstalled) &&
                   AreReactiveAssembliesReady() &&
                   (!_installR3Unity || r3UnityWillBeReady);
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
                   (!_installRuntimeModule || _runtimeUpdateState == EvoPackageUpdateState.InstalledTarget) &&
                   (!_installYandexModule || _yandexInstalled);
        }

        private bool ArePackagesReadyForStarterScaffold()
        {
            return _runtimeInstalled &&
                   _vContainerInstalled &&
                   _uniTaskInstalled &&
                   _addressablesInstalled &&
                   _localizationInstalled &&
                   _inputSystemInstalled &&
                   _uguiInstalled &&
                   _primeTweenInstalled &&
                   _r3UnityInstalled &&
                   AreReactiveAssembliesReady();
        }

        private static string GetOneClickStateKey()
        {
            return OneClickStateKeyPrefix + Application.dataPath.GetHashCode();
        }

        private static string GetOdinImportStateKey()
        {
            return OdinImportStateKeyPrefix + Application.dataPath.GetHashCode();
        }

        private static string GetScaffoldStateKey()
        {
            return ScaffoldStateKeyPrefix + Application.dataPath.GetHashCode();
        }

        private static string GetSelectionStateKey(string key)
        {
            return SelectionStateKeyPrefix + Application.dataPath.GetHashCode() + "." + key;
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
