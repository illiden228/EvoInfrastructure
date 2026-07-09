#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        private const string YandexPackageName = "com.evo.infrastructure.yandex";
        private const string CrazyGamesPackageName = "com.evo.infrastructure.crazygames";
        private const string CorePackageName = "com.evo.infrastructure.core";
        private const string LegacyRuntimePackageName = "com.evo.infrastructure.runtime";
        private const string EvoLatestReleaseApiUrl = "https://api.github.com/repos/illiden228/EvoInfrastructure/releases/latest";
        private const string EvoTagsApiUrl = "https://api.github.com/repos/illiden228/EvoInfrastructure/tags?per_page=1";
        private const string RuntimeGitTag = "v0.5.2";
        private const string YandexGitTag = "v0.5.2";
        private const string CrazyGamesGitTag = "v0.5.2";
        private static readonly EvoPackageDescriptor[] EvoPackages =
        {
            new("com.evo.infrastructure.di", "DI", "Core", "Feature registry and VContainer helpers."),
            new("com.evo.infrastructure.debug", "Debug", "Core", "Logging helpers."),
            new("com.evo.infrastructure.config", "Config", "Core", "Scriptable config catalogs and config service.", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.platform", "Platform", "Core", "Platform info and platform lifecycle abstractions.", "com.evo.infrastructure.config", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.resources", "Resources", "Core", "Addressables resource catalog, loader and provider.", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.scene", "Scene", "Core", "Scene loading and payload services.", "com.evo.infrastructure.resources", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.save", "Save", "Services", "Save contracts, service and local backends.", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.analytics", "Analytics", "Services", "Analytics service and adapter routing.", "com.evo.infrastructure.config", "com.evo.infrastructure.platform", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.ads", "Ads", "Services", "Ads service, cooldown helpers and adapter routing.", "com.evo.infrastructure.analytics", "com.evo.infrastructure.config", "com.evo.infrastructure.platform", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.leaderboards", "Leaderboards", "Services", "Leaderboard service and adapter contract.", "com.evo.infrastructure.config", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.localization", "Localization", "Services", "Unity Localization wrapper.", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.focus", "Focus", "Services", "Input focus service.", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.audio", "Audio", "Services", "Audio playback service.", "com.evo.infrastructure.resources", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.pooling", "Pooling", "Services", "Generic, keyed, async keyed and GameObject pooling helpers."),
            new("com.evo.infrastructure.loading", "Loading", "Runtime", "Startup/loading pipeline.", "com.evo.infrastructure.analytics", "com.evo.infrastructure.config", "com.evo.infrastructure.localization", "com.evo.infrastructure.platform", "com.evo.infrastructure.resources", "com.evo.infrastructure.save", "com.evo.infrastructure.scene", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.ui", "UI", "Runtime", "UI runtime, views and UI service.", "com.evo.infrastructure.resources", "com.evo.infrastructure.scene", "com.evo.infrastructure.di", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.build", "Build", "Editor", "Build profiles and build pipeline editor tools.", "com.evo.infrastructure.config", "com.evo.infrastructure.platform"),
            new("com.evo.infrastructure.editor-tools", "Editor Tools", "Editor", "Config, save, scene, resource and UI editor tools.", "com.evo.infrastructure.ads", "com.evo.infrastructure.analytics", "com.evo.infrastructure.config", "com.evo.infrastructure.leaderboards", "com.evo.infrastructure.platform", "com.evo.infrastructure.resources", "com.evo.infrastructure.save", "com.evo.infrastructure.scene", "com.evo.infrastructure.ui", "com.evo.infrastructure.debug"),
            new(YandexPackageName, "Yandex Core", "Yandex", "Shared PluginYG2 runtime config.", "com.evo.infrastructure.config"),
            new("com.evo.infrastructure.yandex.platform", "Yandex Platform", "Yandex", "Yandex platform info and lifecycle providers.", YandexPackageName, "com.evo.infrastructure.platform", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.yandex.ads", "Yandex Ads", "Yandex", "Yandex ads adapter.", YandexPackageName, "com.evo.infrastructure.ads", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.yandex.analytics", "Yandex Analytics", "Yandex", "Yandex Metrica analytics adapter.", YandexPackageName, "com.evo.infrastructure.analytics", "com.evo.infrastructure.platform", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.yandex.save", "Yandex Save", "Yandex", "Yandex cloud save and player auth adapters.", YandexPackageName, "com.evo.infrastructure.save", "com.evo.infrastructure.platform", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.yandex.leaderboards", "Yandex Leaderboards", "Yandex", "Yandex leaderboard adapter.", YandexPackageName, "com.evo.infrastructure.leaderboards", "com.evo.infrastructure.platform", "com.evo.infrastructure.di"),
            new(CrazyGamesPackageName, "CrazyGames Core", "CrazyGames", "Shared CrazySDK runtime config and SDK helper.", "com.evo.infrastructure.config", "com.evo.infrastructure.debug"),
            new("com.evo.infrastructure.crazygames.platform", "CrazyGames Platform", "CrazyGames", "CrazyGames platform info and lifecycle providers.", CrazyGamesPackageName, "com.evo.infrastructure.platform", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.crazygames.ads", "CrazyGames Ads", "CrazyGames", "CrazyGames ads adapter.", CrazyGamesPackageName, "com.evo.infrastructure.ads", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.crazygames.save", "CrazyGames Save", "CrazyGames", "CrazyGames save and player auth adapters.", CrazyGamesPackageName, "com.evo.infrastructure.save", "com.evo.infrastructure.di"),
            new("com.evo.infrastructure.crazygames.leaderboards", "CrazyGames Leaderboards", "CrazyGames", "CrazyGames leaderboard adapter.", CrazyGamesPackageName, "com.evo.infrastructure.leaderboards", "com.evo.infrastructure.di")
        };
        private static readonly string[] RuntimePackageNames = EvoPackages
            .Where(package => package.Category != "Yandex" && package.Category != "CrazyGames")
            .Select(package => package.Id)
            .ToArray();
        private static readonly string[] DefaultSelectedEvoPackageNames =
        {
            "com.evo.infrastructure.di",
            "com.evo.infrastructure.debug",
            "com.evo.infrastructure.config",
            "com.evo.infrastructure.platform",
            "com.evo.infrastructure.resources",
            "com.evo.infrastructure.scene",
            "com.evo.infrastructure.loading",
            "com.evo.infrastructure.ui",
            "com.evo.infrastructure.save",
            "com.evo.infrastructure.analytics",
            "com.evo.infrastructure.ads",
            "com.evo.infrastructure.leaderboards",
            "com.evo.infrastructure.localization",
            "com.evo.infrastructure.focus",
            "com.evo.infrastructure.audio",
            "com.evo.infrastructure.pooling",
            "com.evo.infrastructure.build",
            "com.evo.infrastructure.editor-tools"
        };
        private const string PluginYgLatestReleaseApiUrl = "https://api.github.com/repos/JustPlay-Max/Unity-PluginYG-2/releases/latest";
        private const string PluginYgFolderPath = "Assets/PluginYourGames";
        private const string PluginYgImportStateKeyPrefix = "Evo.Infrastructure.Core.PluginYgImportRequested.";
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
        private static readonly FeatureRegistrationSnippet[] FeatureRegistrationSnippets =
        {
            new("com.evo.infrastructure.config", "UseConfig", "features.UseConfig(configCatalogs);"),
            new("com.evo.infrastructure.platform", "UsePlatform", "features.UsePlatform();"),
            new("com.evo.infrastructure.focus", "UseFocus", "features.UseFocus();"),
            new("com.evo.infrastructure.resources", "UseResources", "features.UseResources(resourceCatalog);"),
            new("com.evo.infrastructure.scene", "UseScene", "features.UseScene(new SceneLoaderOptions());"),
            new("com.evo.infrastructure.localization", "UseLocalization", "features.UseLocalization();"),
            new("com.evo.infrastructure.audio", "UseAudio", "features.UseAudio();"),
            new("com.evo.infrastructure.ads", "UseAds", "features.UseAds();"),
            new("com.evo.infrastructure.analytics", "UseAnalytics", "features.UseAnalytics();"),
            new("com.evo.infrastructure.leaderboards", "UseLeaderboards", "features.UseLeaderboards();"),
            new("com.evo.infrastructure.save", "UseSave", "features.UseSave(new SaveStorageOptions());"),
            new("com.evo.infrastructure.ui", "UseUi", "features.UseUi(uiSystemConfig);"),
            new("com.evo.infrastructure.loading", "UseLoading", "features.UseLoading(new SceneTransitionOptions());"),
            new("com.evo.infrastructure.yandex.platform", "UseYandexPlatform", "features.UseYandexPlatform();"),
            new("com.evo.infrastructure.yandex.ads", "UseYandexAds", "features.UseYandexAds();"),
            new("com.evo.infrastructure.yandex.analytics", "UseYandexAnalytics", "features.UseYandexAnalytics();"),
            new("com.evo.infrastructure.yandex.save", "UseYandexSave", "features.UseYandexSave();"),
            new("com.evo.infrastructure.yandex.leaderboards", "UseYandexLeaderboards", "features.UseYandexLeaderboards();"),
            new("com.evo.infrastructure.crazygames.platform", "UseCrazyGamesPlatform", "features.UseCrazyGamesPlatform();"),
            new("com.evo.infrastructure.crazygames.ads", "UseCrazyGamesAds", "features.UseCrazyGamesAds();"),
            new("com.evo.infrastructure.crazygames.save", "UseCrazyGamesSave", "features.UseCrazyGamesSave();"),
            new("com.evo.infrastructure.crazygames.leaderboards", "UseCrazyGamesLeaderboards", "features.UseCrazyGamesLeaderboards();")
        };

        private const string VContainerSource = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer";
        private const string UniTaskSource = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask";
        private const string NuGetForUnitySource = "https://github.com/GlitchEnzo/NuGetForUnity.git?path=src/NuGetForUnity";
        private const string AddressablesSource = "com.unity.addressables@2.8.1";
        private const string LocalizationSource = "com.unity.localization@1.5.9";
        private const string InputSystemSource = "com.unity.inputsystem@1.7.0";
        private const string UguiSource = "com.unity.ugui";
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
        [NonSerialized] private readonly HashSet<string> _selectedEvoPackageNames = new(StringComparer.OrdinalIgnoreCase);
        [NonSerialized] private readonly HashSet<string> _installedEvoPackageNames = new(StringComparer.OrdinalIgnoreCase);
        private bool _dependenciesInstalled;
        private bool _runtimeInstalled;
        private bool _legacyRuntimeInstalled;
        private string _legacyRuntimeManifestDependency = string.Empty;
        private bool _pluginYgInstalled;
        private bool _pluginYgDefineReady;
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
        private bool _pluginYgImportRequested;
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
        private bool _installPluginYgPackage;
        private bool _installOdinPackage;
        private bool _installProjectStructure = true;
        private bool _installStarterScaffold = true;
        private bool _templatesReady;
        private bool _scaffoldScriptsUpToDate;
        private double _refreshStartedAt;
        private string _statusLine = "Ready";
        private Vector2 _scroll;
        private string _selectedEvoPackageId;
        private string[] _evoPackageCategories = Array.Empty<string>();
        private readonly Dictionary<string, string> _evoPackageDependencySummary = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _evoPackageExternalDependencySummary = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _cachedDescriptorIssues = new();
        private readonly List<string> _cachedGraphIssues = new();
        private readonly List<string> _cachedLegacyRuntimeCallSites = new();
        private readonly List<string> _cachedOdinAsmdefIssues = new();
        private readonly HashSet<string> _cachedProjectFeatureRegistrationMethods = new(StringComparer.Ordinal);
        private readonly List<string> _outdatedEvoPackageNames = new();
        private string _latestEvoGitTag = string.Empty;
        private string _latestEvoUpdateError = string.Empty;
        private bool _latestEvoVersionCheckRequested;
        private bool _isCheckingLatestEvoVersion;
        private bool _selectionStateDirty;
        private readonly List<string> _templateValidationIssues = new();
        private readonly List<string> _customScaffoldScriptPaths = new();

        private enum EvoPackageUpdateState
        {
            Unknown = 0,
            Missing = 1,
            InstalledTarget = 2,
            InstalledDifferentRevision = 3
        }

        private enum ExternalPackageDependency
        {
            VContainer,
            UniTask,
            NuGetForUnity,
            ReactiveNuGets,
            R3Unity,
            Addressables,
            Localization,
            InputSystem,
            Ugui,
            PrimeTween,
            PluginYg
        }

        private enum DependencyItemState
        {
            Installed,
            Selected,
            Missing
        }

        private sealed class EvoPackageDescriptor
        {
            public EvoPackageDescriptor(string id, string displayName, string category, string description, params string[] dependencies)
            {
                Id = id;
                DisplayName = displayName;
                Category = category;
                Description = description;
                Dependencies = dependencies ?? Array.Empty<string>();
                ExternalDependencies = ResolveExternalDependencies(id);
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Category { get; }
            public string Description { get; }
            public IReadOnlyList<string> Dependencies { get; }
            public IReadOnlyList<ExternalPackageDependency> ExternalDependencies { get; }
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
            EditorApplication.delayCall += RequestLatestEvoVersionCheck;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateInstallQueue;
            EditorApplication.delayCall -= RequestLatestEvoVersionCheck;
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
            DrawTopActions();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("External Dependencies", EditorStyles.boldLabel);
            DrawDependencySection("Core", new[]
            {
                new ExternalDependencyRow(ExternalPackageDependency.VContainer, "VContainer", _installVContainer, _vContainerInstalled, "DI container for project and scene scopes.", () => RemoveExternalDependency(ExternalPackageDependency.VContainer, VContainerPackageName, "VContainer")),
                new ExternalDependencyRow(ExternalPackageDependency.UniTask, "UniTask", _installUniTask, _uniTaskInstalled, "Async runtime used by loading and services.", () => RemoveExternalDependency(ExternalPackageDependency.UniTask, UniTaskPackageName, "UniTask")),
                new ExternalDependencyRow(ExternalPackageDependency.NuGetForUnity, "NuGetForUnity", _installNuGetForUnity, _nuGetForUnityInstalled, "Installs reactive NuGet packages.", () => RemoveExternalDependency(ExternalPackageDependency.NuGetForUnity, NuGetForUnityPackageName, "NuGetForUnity"))
            });
            DrawDependencySection("Runtime", new[]
            {
                new ExternalDependencyRow(ExternalPackageDependency.Addressables, "Addressables", _installAddressables, _addressablesInstalled, "Startup scene references are created as addressable assets.", () => RemoveExternalDependency(ExternalPackageDependency.Addressables, AddressablesPackageName, "Addressables")),
                new ExternalDependencyRow(ExternalPackageDependency.Localization, "Unity Localization", _installLocalization, _localizationInstalled, "Runtime localization package.", () => RemoveExternalDependency(ExternalPackageDependency.Localization, LocalizationPackageName, "Unity Localization")),
                new ExternalDependencyRow(ExternalPackageDependency.InputSystem, "Input System", _installInputSystem, _inputSystemInstalled, "Default input package for gameplay projects.", () => RemoveExternalDependency(ExternalPackageDependency.InputSystem, InputSystemPackageName, "Input System")),
                new ExternalDependencyRow(ExternalPackageDependency.Ugui, "Unity UI", _installUgui, _uguiInstalled, "Base UI package for loading and menus.", () => RemoveExternalDependency(ExternalPackageDependency.Ugui, UguiPackageName, "Unity UI")),
                new ExternalDependencyRow(ExternalPackageDependency.PrimeTween, "PrimeTween", _installPrimeTween, _primeTweenInstalled, "Tweening dependency.", () => RemoveExternalDependency(ExternalPackageDependency.PrimeTween, PrimeTweenPackageName, "PrimeTween")),
                new ExternalDependencyRow(ExternalPackageDependency.R3Unity, "R3.Unity", _installR3Unity, _r3UnityInstalled, "Reactive Unity integration.", () => RemoveExternalDependency(ExternalPackageDependency.R3Unity, R3UnityPackageName, "R3.Unity")),
                new ExternalDependencyRow(ExternalPackageDependency.ReactiveNuGets, "Reactive NuGets", _installReactiveNuGets, AreReactiveReadyOrConfigured(), "R3, ObservableCollections and ObservableCollections.R3.", RemoveReactiveNuGetPackages)
            });
            DrawLegacyRuntimeMigration();
            DrawLatestEvoVersionCheck();
            DrawEvoPackageUpdates();
            DrawEvoPackageGraph();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Optional SDK", EditorStyles.boldLabel);
            DrawDependencySection("Yandex", new[]
            {
                new ExternalDependencyRow(ExternalPackageDependency.PluginYg, "PluginYG2", _installPluginYgPackage, _pluginYgInstalled, GetPluginYgDetails(), null)
            });
            var odinSelected = DrawInstallPlanRow("Odin Inspector", _installOdinPackage, _odinInstalled, "Imported automatically at the end of Setup. Odin is not required for starter runtime.");
            SetSelectionField(ref _installOdinPackage, odinSelected);
            DrawYandexSdkDiagnostics();
            DrawCrazySdkDiagnostics();
            DrawOdinAsmdefDiagnostics();

            EditorGUILayout.Space(4f);
            DrawProjectRuntimeActions();
            DrawRegistrationDiagnostics();

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
        }

        private void DrawTopActions()
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

            DrawScaffoldOwnershipDiagnostics();

            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegacyRuntimeMigration()
        {
            if (!_legacyRuntimeInstalled)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                $"Legacy runtime package is installed: {LegacyRuntimePackageName}" +
                (string.IsNullOrWhiteSpace(_legacyRuntimeManifestDependency) ? string.Empty : $" ({_legacyRuntimeManifestDependency})") +
                ". Use migration to replace it with selected feature packages. Existing project LifetimeScope files are not overwritten.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
            {
                if (GUILayout.Button("Select Migration Packages", GUILayout.Width(170f)))
                {
                    var selectionBefore = BuildSelectionStateSignature();
                    SelectLegacyRuntimeMigrationPackages();
                    MarkSelectionStateDirtyIfChanged(selectionBefore);
                    _statusLine = "Selected packages that replace legacy Evo runtime.";
                }

                var canMigrate = _stateAnalyzed && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested && !_isRefreshingState;
                using (new EditorGUI.DisabledScope(!canMigrate))
                {
                    if (GUILayout.Button("Migrate Runtime Package", GUILayout.Width(180f)))
                    {
                        MigrateLegacyRuntimePackage();
                    }
                }
            }

            EditorGUILayout.LabelField("Installs selected replacement packages and removes only the old aggregate runtime package.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();
            DrawLegacyRuntimeCallSiteDiagnostics();
        }

        private void DrawLegacyRuntimeCallSiteDiagnostics()
        {
            var callSites = _cachedLegacyRuntimeCallSites;
            if (callSites.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Legacy installer API calls were found. Migration changes packages only; update these project call sites manually before removing the old runtime package:\n" +
                string.Join("\n", callSites.Take(6)) +
                "\n\nUse the registration snippet below in your existing RuntimeProjectLifetimeScope instead of YandexRuntimeInstaller/CrazyGamesRuntimeInstaller.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy Migration Registration Snippet", GUILayout.Width(230f)))
            {
                GUIUtility.systemCopyBuffer = BuildFeatureRegistrationSnippet(includeOnlyMissing: false);
                _statusLine = "Copied Evo feature registration snippet to clipboard.";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEvoPackageUpdates()
        {
            if (_outdatedEvoPackageNames.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                $"Evo package updates are available for tag {RuntimeGitTag}:\n" +
                string.Join("\n", _outdatedEvoPackageNames.Select(GetEvoPackageDisplayNameOrId).Take(12)) +
                (_outdatedEvoPackageNames.Count > 12 ? $"\n...and {_outdatedEvoPackageNames.Count - 12} more." : string.Empty),
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || _addAndRemoveRequest != null))
            {
                if (GUILayout.Button("Update Evo Packages", GUILayout.Width(170f)))
                {
                    UpdateOutdatedEvoPackages();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLatestEvoVersionCheck()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                string.IsNullOrWhiteSpace(_latestEvoGitTag)
                    ? $"Current Evo target: {RuntimeGitTag}"
                    : $"Current Evo target: {RuntimeGitTag} | Latest: {_latestEvoGitTag}",
                EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(_isCheckingLatestEvoVersion))
            {
                if (GUILayout.Button(_isCheckingLatestEvoVersion ? "Checking..." : "Check Updates", GUILayout.Width(120f)))
                {
                    CheckLatestEvoVersionAsync(force: true);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(_latestEvoUpdateError))
            {
                EditorGUILayout.HelpBox("Evo update check failed: " + _latestEvoUpdateError, MessageType.Warning);
            }

            if (!HasRemoteEvoUpdate())
            {
                return;
            }

            EditorGUILayout.HelpBox(
                $"A newer EvoInfrastructure release is available: {_latestEvoGitTag}. Update core first; after Unity reloads, the new wizard can update the remaining Evo packages.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || _addAndRemoveRequest != null))
            {
                if (GUILayout.Button("Update Evo Core", GUILayout.Width(150f)))
                {
                    UpdateEvoCoreToLatest();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool HasRemoteEvoUpdate()
        {
            return !string.IsNullOrWhiteSpace(_latestEvoGitTag) &&
                   IsGitTagNewerThan(_latestEvoGitTag, RuntimeGitTag);
        }

        private void UpdateEvoCoreToLatest()
        {
            if (!HasRemoteEvoUpdate() || _isInstalling || _addAndRemoveRequest != null)
            {
                return;
            }

            var source = GetEvoPackageSource(CorePackageName, _latestEvoGitTag);
            _isInstalling = true;
            _statusLine = "Updating Evo core to " + _latestEvoGitTag + "...";
            Debug.Log($"[Evo Setup] Updating Evo core to {_latestEvoGitTag}: {source}");
            _addAndRemoveRequest = Client.AddAndRemove(new[] { source }, Array.Empty<string>());
        }

        private void UpdateOutdatedEvoPackages()
        {
            if (_isInstalling || _addAndRemoveRequest != null || _outdatedEvoPackageNames.Count == 0)
            {
                return;
            }

            var packages = _outdatedEvoPackageNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetEvoPackageInstallOrder)
                .ThenBy(GetEvoPackageDescriptorIndex)
                .Select(GetEvoPackageSource)
                .ToArray();

            if (packages.Length == 0)
            {
                _statusLine = "No Evo packages need update.";
                return;
            }

            _isInstalling = true;
            _statusLine = "Updating Evo packages to " + RuntimeGitTag + "...";
            Debug.Log($"[Evo Setup] Updating Evo packages to {RuntimeGitTag}:\n{string.Join("\n", packages)}");
            _addAndRemoveRequest = Client.AddAndRemove(packages, Array.Empty<string>());
        }

        private void RequestLatestEvoVersionCheck()
        {
            CheckLatestEvoVersionAsync(force: false);
        }

        private async void CheckLatestEvoVersionAsync(bool force)
        {
            if (_isCheckingLatestEvoVersion || (_latestEvoVersionCheckRequested && !force))
            {
                return;
            }

            _latestEvoVersionCheckRequested = true;
            _isCheckingLatestEvoVersion = true;
            _latestEvoUpdateError = string.Empty;
            _statusLine = "Checking latest EvoInfrastructure release...";
            Repaint();

            try
            {
                var latestTag = await Task.Run(TryFetchLatestEvoGitTag);
                _latestEvoGitTag = latestTag;
                _statusLine = string.IsNullOrWhiteSpace(latestTag)
                    ? "Could not find latest EvoInfrastructure tag."
                    : string.Equals(latestTag, RuntimeGitTag, StringComparison.OrdinalIgnoreCase)
                        ? "EvoInfrastructure is up to date."
                        : "EvoInfrastructure update available: " + latestTag;
            }
            catch (Exception ex)
            {
                _latestEvoUpdateError = ex.Message;
                _statusLine = "EvoInfrastructure update check failed.";
                Debug.LogWarning("[Evo Setup] EvoInfrastructure update check failed: " + ex.Message);
            }
            finally
            {
                _isCheckingLatestEvoVersion = false;
                Repaint();
            }
        }

        private static string TryFetchLatestEvoGitTag()
        {
            using var webClient = new System.Net.WebClient();
            webClient.Headers.Add("User-Agent", "Evo-Infrastructure-Setup");

            try
            {
                var releaseJson = webClient.DownloadString(EvoLatestReleaseApiUrl);
                var release = JsonUtility.FromJson<GitHubReleaseInfo>(releaseJson);
                if (!string.IsNullOrWhiteSpace(release?.tag_name))
                {
                    return NormalizeGitTag(release.tag_name);
                }
            }
            catch
            {
                // Some repositories use tags without GitHub releases. Fall back to tags API.
            }

            var tagsJson = webClient.DownloadString(EvoTagsApiUrl);
            var tags = JsonHelper.FromJson<GitHubTagInfo>(tagsJson);
            return tags != null && tags.Length > 0 && !string.IsNullOrWhiteSpace(tags[0].name)
                ? NormalizeGitTag(tags[0].name)
                : string.Empty;
        }

        private static string NormalizeGitTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            tag = tag.Trim();
            return tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag : "v" + tag;
        }

        private static bool IsGitTagNewerThan(string candidateTag, string currentTag)
        {
            if (!TryParseGitTagVersion(candidateTag, out var candidate) ||
                !TryParseGitTagVersion(currentTag, out var current))
            {
                return !string.Equals(candidateTag, currentTag, StringComparison.OrdinalIgnoreCase);
            }

            return candidate.CompareTo(current) > 0;
        }

        private static bool TryParseGitTagVersion(string tag, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            var value = tag.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            var suffixIndex = value.IndexOfAny(new[] { '-', '+' });
            if (suffixIndex >= 0)
            {
                value = value.Substring(0, suffixIndex);
            }

            return Version.TryParse(value, out version);
        }

        private void DrawScaffoldOwnershipDiagnostics()
        {
            if (_customScaffoldScriptPaths.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Starter scaffold scripts differ from Evo templates and are treated as project-owned. Setup will not overwrite them:\n" +
                string.Join("\n", _customScaffoldScriptPaths) +
                "\n\nUse the diagnostics below or copy snippets if you want to update them manually.",
                MessageType.Info);
        }

        private void DrawRegistrationDiagnostics()
        {
            if (!_stateAnalyzed)
            {
                return;
            }

            var missing = CollectMissingFeatureRegistrationSnippets();
            if (missing.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Selected or installed Evo feature packages are not referenced from the existing RuntimeProjectLifetimeScope. The wizard will not rewrite the file; add these calls manually if this project should use them:\n" +
                string.Join("\n", missing.Select(snippet => snippet.Call)),
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy Missing Registration Calls", GUILayout.Width(220f)))
            {
                GUIUtility.systemCopyBuffer = BuildFeatureRegistrationSnippet(includeOnlyMissing: true);
                _statusLine = "Copied missing Evo feature registration calls to clipboard.";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProjectRuntimeActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Project Runtime", EditorStyles.boldLabel);

            var packagesReadyForScaffold = ArePackagesReadyForStarterScaffold();
            var scaffoldReady = IsStarterScaffoldReady();
            var projectStructureSelected = DrawSetupTaskRow(
                "Project Structure",
                _installProjectStructure,
                _structureReady,
                _structureReady ? "Ready" : "Missing",
                "Base folders under Assets/_Project.",
                _structureReady ? "Ready" : "Create",
                _stateAnalyzed && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested && !_isRefreshingState && !_structureReady,
                CreateProjectStructure);
            SetSelectionField(ref _installProjectStructure, projectStructureSelected);

            var starterScaffoldSelected = DrawSetupTaskRow(
                "Starter Scaffold",
                _installStarterScaffold,
                scaffoldReady,
                _scaffoldSetupRequested ? "Running" : scaffoldReady ? "Ready" : HasStarterScaffoldFiles() ? "Needs Repair" : "Missing",
                GetStarterScaffoldStatusDetails(packagesReadyForScaffold),
                scaffoldReady ? "Ready" : HasStarterScaffoldFiles() ? "Repair" : "Create",
                _stateAnalyzed && !_isInstalling && !_oneClickSetupRequested && !_scaffoldSetupRequested && !_isRefreshingState && packagesReadyForScaffold && !scaffoldReady,
                StartStarterRuntimeScaffold);
            SetSelectionField(ref _installStarterScaffold, starterScaffoldSelected);
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
                ? _customScaffoldScriptPaths.Count == 0
                    ? "Starter scripts, scenes, configs, Addressables entries and build scenes."
                    : "Starter scaffold ready. Custom project scripts are preserved."
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

        private void DrawDependencySection(string title, IReadOnlyList<ExternalDependencyRow> rows)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var value = DrawExternalDependencyRow(row.Dependency, row.Label, row.Selected, row.Installed, row.Details, row.RemoveAction);
                if (!row.Installed)
                {
                    SetExternalDependencySelection(row.Dependency, value);
                }
            }
        }

        private bool DrawExternalDependencyRow(
            ExternalPackageDependency dependency,
            string label,
            bool selected,
            bool installed,
            string details,
            Action removeAction = null)
        {
            var value = DrawInstallPlanRow(label, selected, installed, details, removeAction);
            if (!installed && selected && !value)
            {
                DeselectExternalDependencySelection(dependency);
            }

            return value;
        }

        private void DeselectExternalDependencySelection(ExternalPackageDependency dependency)
        {
            if (dependency == ExternalPackageDependency.NuGetForUnity)
            {
                SetSelectionField(ref _installReactiveNuGets, false);
                DeselectEvoPackagesRequiringExternalDependency(ExternalPackageDependency.ReactiveNuGets);
            }

            DeselectEvoPackagesRequiringExternalDependency(dependency);
        }

        private void SetExternalDependencySelection(ExternalPackageDependency dependency, bool value)
        {
            switch (dependency)
            {
                case ExternalPackageDependency.VContainer:
                    SetSelectionField(ref _installVContainer, value);
                    break;
                case ExternalPackageDependency.UniTask:
                    SetSelectionField(ref _installUniTask, value);
                    break;
                case ExternalPackageDependency.NuGetForUnity:
                    SetSelectionField(ref _installNuGetForUnity, value);
                    break;
                case ExternalPackageDependency.ReactiveNuGets:
                    SetSelectionField(ref _installReactiveNuGets, value);
                    break;
                case ExternalPackageDependency.R3Unity:
                    SetSelectionField(ref _installR3Unity, value);
                    break;
                case ExternalPackageDependency.Addressables:
                    SetSelectionField(ref _installAddressables, value);
                    break;
                case ExternalPackageDependency.Localization:
                    SetSelectionField(ref _installLocalization, value);
                    break;
                case ExternalPackageDependency.InputSystem:
                    SetSelectionField(ref _installInputSystem, value);
                    break;
                case ExternalPackageDependency.Ugui:
                    SetSelectionField(ref _installUgui, value);
                    break;
                case ExternalPackageDependency.PrimeTween:
                    SetSelectionField(ref _installPrimeTween, value);
                    break;
                case ExternalPackageDependency.PluginYg:
                    SetSelectionField(ref _installPluginYgPackage, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }

        private void RemoveExternalDependency(ExternalPackageDependency dependency, string packageName, string displayName)
        {
            var selectionBefore = BuildSelectionStateSignature();
            SetExternalDependencySelection(dependency, false);
            DeselectExternalDependencySelection(dependency);
            MarkSelectionStateDirtyIfChanged(selectionBefore);
            RemovePackage(packageName, displayName);
        }

        private void DrawEvoPackageGraph()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Evo Packages", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Selecting a package selects its dependencies. Clearing a dependency clears packages that depend on it.",
                MessageType.Info);

            if (_cachedDescriptorIssues.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Package descriptor issues:\n" + string.Join("\n", _cachedDescriptorIssues),
                    MessageType.Warning);
            }

            if (_cachedGraphIssues.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Selected package graph issues:\n" + string.Join("\n", _cachedGraphIssues),
                    MessageType.Warning);
            }

            for (var c = 0; c < _evoPackageCategories.Length; c++)
            {
                var category = _evoPackageCategories[c];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(category, EditorStyles.miniBoldLabel, GUILayout.Width(180f));
                using (new EditorGUI.DisabledScope(_isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
                {
                    if (GUILayout.Button("Select all", GUILayout.Width(78f)))
                    {
                        var selectionBefore = BuildSelectionStateSignature();
                        SelectEvoPackageCategory(category);
                        MarkSelectionStateDirtyIfChanged(selectionBefore);
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(58f)))
                    {
                        var selectionBefore = BuildSelectionStateSignature();
                        DeselectEvoPackageCategory(category);
                        MarkSelectionStateDirtyIfChanged(selectionBefore);
                    }
                }
                EditorGUILayout.EndHorizontal();
                DrawPackageTableHeader();

                for (var i = 0; i < EvoPackages.Length; i++)
                {
                    var package = EvoPackages[i];
                    if (!string.Equals(package.Category, category, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    DrawEvoPackageRow(package);
                }

                EditorGUILayout.Space(2f);
            }

            DrawSelectedEvoPackageDetails();
        }

        private void DrawEvoPackageRow(EvoPackageDescriptor package)
        {
            var installed = IsEvoPackageInstalled(package.Id);
            var selected = installed || _selectedEvoPackageNames.Contains(package.Id);
            EditorGUILayout.BeginHorizontal();

            var newSelected = selected;
            using (new EditorGUI.DisabledScope(installed || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested))
            {
                newSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(18f));
            }

            if (newSelected != selected)
            {
                if (newSelected)
                {
                    SelectEvoPackageWithDependencies(package.Id);
                }
                else
                {
                    DeselectEvoPackageWithDependents(package.Id);
                }

                MarkSelectionStateDirty();
            }

            var detailsSelected = string.Equals(_selectedEvoPackageId, package.Id, StringComparison.OrdinalIgnoreCase);
            if (GUILayout.Toggle(detailsSelected, package.DisplayName, EditorStyles.miniButton, GUILayout.Width(150f)) && !detailsSelected)
            {
                _selectedEvoPackageId = package.Id;
            }

            var old = GUI.color;
            GUI.color = installed
                ? new Color(0.25f, 0.7f, 0.25f)
                : newSelected
                    ? new Color(0.85f, 0.65f, 0.2f)
                    : new Color(0.65f, 0.65f, 0.65f);
            EditorGUILayout.LabelField(installed ? "Installed" : newSelected ? "Selected" : "Skipped", GUILayout.Width(72f));
            GUI.color = old;

            using (new EditorGUI.DisabledScope(!installed || _isInstalling || _oneClickSetupRequested || _scaffoldSetupRequested || _removeRequest != null))
            {
                if (GUILayout.Button("Remove", GUILayout.Width(68f)))
                {
                    var selectionBefore = BuildSelectionStateSignature();
                    DeselectEvoPackageWithDependents(package.Id);
                    MarkSelectionStateDirtyIfChanged(selectionBefore);
                    RemovePackage(package.Id, package.DisplayName);
                }
            }

            EditorGUILayout.LabelField(GetCachedPackageSummary(_evoPackageDependencySummary, package.Id), EditorStyles.miniLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField(GetCachedPackageSummary(_evoPackageExternalDependencySummary, package.Id), EditorStyles.miniLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField(package.Description, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPackageTableHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            EditorGUILayout.LabelField("Package", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField("State", EditorStyles.miniBoldLabel, GUILayout.Width(72f));
            GUILayout.Space(72f);
            EditorGUILayout.LabelField("Packages", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField("External", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField("Description", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedEvoPackageDetails()
        {
            var package = FindEvoPackage(_selectedEvoPackageId) ?? EvoPackages.FirstOrDefault();
            if (package == null)
            {
                return;
            }

            _selectedEvoPackageId = package.Id;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Selected Package Details", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(package.DisplayName, EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(package.Id, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.LabelField(package.Description, EditorStyles.wordWrappedMiniLabel);

            DrawPackageDependencyItems("Package dependencies", package.Dependencies, IsEvoDependencyReady, GetEvoPackageDisplayName);
            DrawExternalDependencyItems(package);

            var diagnostics = GetEvoPackageSelectionDiagnostics(package, IsEvoPackageInstalled(package.Id) || _selectedEvoPackageNames.Contains(package.Id));
            if (!string.IsNullOrWhiteSpace(diagnostics))
            {
                EditorGUILayout.HelpBox(diagnostics.TrimStart(' ', '|'), MessageType.Warning);
            }
        }

        private void DrawPackageDependencyItems(string title, IReadOnlyList<string> dependencies, Func<string, DependencyItemState> getState, Func<string, string> getDisplayName)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            if (dependencies.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                return;
            }

            for (var i = 0; i < dependencies.Count; i++)
            {
                DrawDependencyItem(getDisplayName(dependencies[i]), getState(dependencies[i]));
            }
        }

        private void DrawExternalDependencyItems(EvoPackageDescriptor package)
        {
            EditorGUILayout.LabelField("External dependencies", EditorStyles.miniBoldLabel);
            if (package.ExternalDependencies.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                return;
            }

            for (var i = 0; i < package.ExternalDependencies.Count; i++)
            {
                var dependency = package.ExternalDependencies[i];
                DrawDependencyItem(GetExternalDependencyDisplayName(dependency), GetExternalDependencyState(dependency));
            }
        }

        private static void DrawDependencyItem(string label, DependencyItemState state)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            var old = GUI.color;
            GUI.color = GetDependencyItemColor(state);
            EditorGUILayout.LabelField(GetDependencyItemStateLabel(state), GUILayout.Width(70f));
            GUI.color = old;
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static List<string> CollectEvoPackageDescriptorIssues()
        {
            var issues = new List<string>();
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                var package = EvoPackages[i];
                for (var d = 0; d < package.Dependencies.Count; d++)
                {
                    if (FindEvoPackage(package.Dependencies[d]) == null)
                    {
                        issues.Add($"{package.DisplayName} references unknown package dependency '{package.Dependencies[d]}'.");
                    }
                }
            }

            return issues;
        }

        private List<string> CollectSelectedEvoPackageGraphIssues()
        {
            var issues = new List<string>();
            var selectedPackages = CollectSelectedEvoPackageClosure();
            foreach (var packageName in _selectedEvoPackageNames)
            {
                if (FindEvoPackage(packageName) == null)
                {
                    issues.Add($"Selected unknown package '{packageName}'.");
                }
            }

            foreach (var packageName in selectedPackages)
            {
                var package = FindEvoPackage(packageName);
                if (package == null)
                {
                    continue;
                }

                for (var i = 0; i < package.Dependencies.Count; i++)
                {
                    if (!selectedPackages.Contains(package.Dependencies[i]) && !IsEvoPackageInstalled(package.Dependencies[i]))
                    {
                        issues.Add($"{package.DisplayName} requires {GetEvoPackageDisplayName(package.Dependencies[i])}.");
                    }
                }
            }

            return issues;
        }

        private string GetEvoPackageSelectionDiagnostics(EvoPackageDescriptor package, bool selected)
        {
            if (!selected || package.ExternalDependencies.Count == 0)
            {
                return string.Empty;
            }

            var missingExternalDependencies = package.ExternalDependencies
                .Where(dependency => !IsExternalDependencySelectedOrInstalled(dependency))
                .Select(GetExternalDependencyDisplayName)
                .ToArray();

            return missingExternalDependencies.Length == 0
                ? string.Empty
                : " | missing external: " + string.Join(", ", missingExternalDependencies);
        }

        private void RefreshCachedEvoPackageDisplay()
        {
            _evoPackageCategories = EvoPackages
                .Select(package => package.Category)
                .Distinct()
                .ToArray();

            _evoPackageDependencySummary.Clear();
            _evoPackageExternalDependencySummary.Clear();
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                var package = EvoPackages[i];
                _evoPackageDependencySummary[package.Id] = BuildPackageDependencySummary(package);
                _evoPackageExternalDependencySummary[package.Id] = BuildExternalDependencySummary(package);
            }

            _cachedDescriptorIssues.Clear();
            _cachedDescriptorIssues.AddRange(CollectEvoPackageDescriptorIssues());
            _cachedGraphIssues.Clear();
            _cachedGraphIssues.AddRange(CollectSelectedEvoPackageGraphIssues());
        }

        private void RefreshCachedHeavyDiagnostics()
        {
            _cachedLegacyRuntimeCallSites.Clear();
            if (_legacyRuntimeInstalled)
            {
                _cachedLegacyRuntimeCallSites.AddRange(FindLegacyRuntimeInstallerCallSites(6));
            }

            _cachedOdinAsmdefIssues.Clear();
            if (IsDefineSymbolEnabled("ODIN_INSPECTOR"))
            {
                _cachedOdinAsmdefIssues.AddRange(CollectOdinAsmdefIssues());
            }

            _cachedProjectFeatureRegistrationMethods.Clear();
            foreach (var methodName in CollectProjectFeatureRegistrationMethodNames())
            {
                _cachedProjectFeatureRegistrationMethods.Add(methodName);
            }
        }

        private static string BuildPackageDependencySummary(EvoPackageDescriptor package)
        {
            return package.Dependencies.Count == 0
                ? "None"
                : string.Join(", ", package.Dependencies.Select(GetEvoPackageDisplayName));
        }

        private static string BuildExternalDependencySummary(EvoPackageDescriptor package)
        {
            return package.ExternalDependencies.Count == 0
                ? "None"
                : string.Join(", ", package.ExternalDependencies.Select(GetExternalDependencyDisplayName));
        }

        private static string GetCachedPackageSummary(Dictionary<string, string> cache, string packageId)
        {
            return cache.TryGetValue(packageId, out var summary) ? summary : "None";
        }

        private bool AreReactiveReadyOrConfigured()
        {
            return _r3Ready &&
                   _observableCollectionsReady &&
                   _observableCollectionsR3Ready;
        }

        private DependencyItemState IsEvoDependencyReady(string packageName)
        {
            if (IsEvoPackageInstalled(packageName))
            {
                return DependencyItemState.Installed;
            }

            return _selectedEvoPackageNames.Contains(packageName)
                ? DependencyItemState.Selected
                : DependencyItemState.Missing;
        }

        private DependencyItemState GetExternalDependencyState(ExternalPackageDependency dependency)
        {
            if (IsExternalDependencyInstalled(dependency))
            {
                return DependencyItemState.Installed;
            }

            return IsExternalDependencySelected(dependency)
                ? DependencyItemState.Selected
                : DependencyItemState.Missing;
        }

        private bool IsExternalDependencyInstalled(ExternalPackageDependency dependency)
        {
            switch (dependency)
            {
                case ExternalPackageDependency.VContainer:
                    return _vContainerInstalled;
                case ExternalPackageDependency.UniTask:
                    return _uniTaskInstalled;
                case ExternalPackageDependency.NuGetForUnity:
                    return _nuGetForUnityInstalled;
                case ExternalPackageDependency.ReactiveNuGets:
                    return AreReactiveReadyOrConfigured();
                case ExternalPackageDependency.R3Unity:
                    return _r3UnityInstalled;
                case ExternalPackageDependency.Addressables:
                    return _addressablesInstalled;
                case ExternalPackageDependency.Localization:
                    return _localizationInstalled;
                case ExternalPackageDependency.InputSystem:
                    return _inputSystemInstalled;
                case ExternalPackageDependency.Ugui:
                    return _uguiInstalled;
                case ExternalPackageDependency.PrimeTween:
                    return _primeTweenInstalled;
                case ExternalPackageDependency.PluginYg:
                    return _pluginYgInstalled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }

        private static string GetDependencyItemStateLabel(DependencyItemState state)
        {
            switch (state)
            {
                case DependencyItemState.Installed:
                    return "Installed";
                case DependencyItemState.Selected:
                    return "Selected";
                case DependencyItemState.Missing:
                    return "Missing";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private static Color GetDependencyItemColor(DependencyItemState state)
        {
            switch (state)
            {
                case DependencyItemState.Installed:
                    return new Color(0.25f, 0.7f, 0.25f);
                case DependencyItemState.Selected:
                    return new Color(0.85f, 0.65f, 0.2f);
                case DependencyItemState.Missing:
                    return new Color(0.85f, 0.25f, 0.25f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private string GetPluginYgDetails()
        {
            if (_pluginYgInstalled)
            {
                return _pluginYgDefineReady
                    ? "Yandex Games PluginYG2 is installed and YandexGamesPlatform_yg define is available."
                    : "PluginYG2 files/types were found. Open PluginYG2 settings if YandexGamesPlatform_yg define is still missing.";
            }

            return "Optional Yandex Games PluginYG2 import from the latest GitHub release.";
        }

        private void DrawCrazySdkDiagnostics()
        {
            if (!IsCrazyGamesSelectedOrInstalled() && !IsDefineSymbolEnabled("CRAZY"))
            {
                return;
            }

            var crazyDefineReady = IsDefineSymbolEnabled("CRAZY");
            var crazySdkReady = FindTypeByName("CrazyGames.CrazySDK") != null;
            if (crazyDefineReady && crazySdkReady)
            {
                EditorGUILayout.HelpBox("CrazySDK is visible and CRAZY define is enabled.", MessageType.Info);
                return;
            }

            var issues = new List<string>();
            if (!crazyDefineReady)
            {
                issues.Add("CRAZY define is missing.");
            }

            if (!crazySdkReady)
            {
                issues.Add("CrazyGames.CrazySDK type was not found. Install CrazySDK and, if your project uses asmdefs, create/select a CrazySDK asmdef manually.");
            }

            var messageType = crazyDefineReady && !crazySdkReady ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox("CrazyGames SDK diagnostics: " + string.Join(" ", issues), messageType);
        }

        private void DrawYandexSdkDiagnostics()
        {
            if (!IsYandexSelectedOrInstalled() && !IsDefineSymbolEnabled("YandexGamesPlatform_yg"))
            {
                return;
            }

            var yandexDefineReady = IsDefineSymbolEnabled("YandexGamesPlatform_yg");
            var pluginReady = IsPluginYgInstalled();
            if (yandexDefineReady && pluginReady)
            {
                EditorGUILayout.HelpBox("PluginYG2 is visible and YandexGamesPlatform_yg define is enabled.", MessageType.Info);
                return;
            }

            var issues = new List<string>();
            if (!pluginReady)
            {
                issues.Add("PluginYG2 types were not found. Import PluginYG2 before enabling Yandex adapter packages.");
            }

            if (!yandexDefineReady)
            {
                issues.Add("YandexGamesPlatform_yg define is missing. Open PluginYG2 settings or enable the Yandex platform define.");
            }

            var messageType = yandexDefineReady && !pluginReady ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox("Yandex SDK diagnostics: " + string.Join(" ", issues), messageType);
        }

        private void DrawOdinAsmdefDiagnostics()
        {
            if (!IsDefineSymbolEnabled("ODIN_INSPECTOR"))
            {
                return;
            }

            var issues = _cachedOdinAsmdefIssues;
            if (issues.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Odin is enabled, but some Evo asmdefs that compile Sirenix code do not reference Sirenix assemblies:\n" +
                string.Join("\n", issues.Take(8)) +
                (issues.Count > 8 ? $"\n...and {issues.Count - 8} more." : string.Empty),
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Run Evo asmdef utility", GUILayout.Width(180f)))
            {
                EvoAsmdefUtility.GenerateOrUpdateAsmdefs();
                _statusLine = "Ran Evo asmdef utility.";
            }
            EditorGUILayout.EndHorizontal();
        }

        private static List<string> CollectOdinAsmdefIssues()
        {
            var issues = new List<string>();
            if (!Directory.Exists("Packages"))
            {
                return issues;
            }

            var asmdefPaths = Directory.GetFiles("Packages", "*.asmdef", SearchOption.AllDirectories);
            for (var i = 0; i < asmdefPaths.Length; i++)
            {
                var asmdefPath = asmdefPaths[i].Replace("\\", "/");
                if (asmdefPath.IndexOf("/com.evo.infrastructure.", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var root = Path.GetDirectoryName(asmdefPath);
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                if (!Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                        .Any(path => SafeReadAllText(path).IndexOf("Sirenix.", StringComparison.Ordinal) >= 0))
                {
                    continue;
                }

                var asmdefText = SafeReadAllText(asmdefPath);
                if (asmdefText.IndexOf("Sirenix.OdinInspector", StringComparison.Ordinal) < 0 &&
                    asmdefText.IndexOf("Sirenix.Utilities", StringComparison.Ordinal) < 0)
                {
                    issues.Add(asmdefPath);
                }
            }

            return issues;
        }

        private bool IsCrazyGamesSelectedOrInstalled()
        {
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                var package = EvoPackages[i];
                if (!string.Equals(package.Category, "CrazyGames", StringComparison.Ordinal))
                {
                    continue;
                }

                if (_selectedEvoPackageNames.Contains(package.Id) || IsEvoPackageInstalled(package.Id))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsYandexSelectedOrInstalled()
        {
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                var package = EvoPackages[i];
                if (!string.Equals(package.Category, "Yandex", StringComparison.Ordinal))
                {
                    continue;
                }

                if (_selectedEvoPackageNames.Contains(package.Id) || IsEvoPackageInstalled(package.Id))
                {
                    return true;
                }
            }

            return false;
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

        private void InstallPluginYgPackage()
        {
            if (TryImportPluginYgPackage())
            {
                _statusLine = "Setup: importing PluginYG2 package...";
                QueueRefreshBurst();
            }
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

        private void MigrateLegacyRuntimePackage()
        {
            if (_isInstalling || _removeRequest != null || _addRequest != null || _addAndRemoveRequest != null)
            {
                return;
            }

            var selectionBefore = BuildSelectionStateSignature();
            SelectLegacyRuntimeMigrationPackages();
            MarkSelectionStateDirtyIfChanged(selectionBefore);
            var foundationPackages = CollectFoundationPackagesToInstall();
            if (foundationPackages.Count > 0 ||
                (ShouldConfigureReactiveNuGets() && !AreReactiveAssembliesReady()) ||
                (ShouldInstallExternalDependency(ExternalPackageDependency.R3Unity) && !_r3UnityInstalled && !AreReactiveAssembliesReady()))
            {
                _statusLine = "Migration packages selected. Install selected runtime dependencies first, then run migration again.";
                Debug.LogWarning("[Evo Setup] Legacy runtime migration requires selected runtime dependencies to be installed first.");
                return;
            }

            var packages = CollectFrameworkPackagesToInstall();
            _isInstalling = true;
            _statusLine = packages.Count > 0
                ? "Migrating legacy runtime to feature packages..."
                : "Removing legacy runtime package...";
            Debug.Log(
                "[Evo Setup] Migrating legacy runtime package. Adding:\n" +
                string.Join("\n", packages) +
                $"\nRemoving:\n{LegacyRuntimePackageName}");
            _addAndRemoveRequest = Client.AddAndRemove(packages.ToArray(), new[] { LegacyRuntimePackageName });
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
            var evoPackage = EvoPackages.FirstOrDefault(package => ContainsIgnoreCase(source, package.Id));
            if (evoPackage != null) return evoPackage.DisplayName;
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

        private List<FeatureRegistrationSnippet> CollectMissingFeatureRegistrationSnippets()
        {
            var missing = new List<FeatureRegistrationSnippet>();
            for (var i = 0; i < FeatureRegistrationSnippets.Length; i++)
            {
                var snippet = FeatureRegistrationSnippets[i];
                if (!IsEvoPackageSelectedOrInstalled(snippet.PackageName))
                {
                    continue;
                }

                if (!_cachedProjectFeatureRegistrationMethods.Contains(snippet.MethodName))
                {
                    missing.Add(snippet);
                }
            }

            return missing;
        }

        private static HashSet<string> CollectProjectFeatureRegistrationMethodNames()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);

            var assetsRoot = Path.Combine(GetProjectRootPath(), "Assets");
            if (!Directory.Exists(assetsRoot))
            {
                return result;
            }

            try
            {
                var files = Directory.GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories);
                for (var i = 0; i < files.Length; i++)
                {
                    string text;
                    try
                    {
                        text = File.ReadAllText(files[i]);
                    }
                    catch
                    {
                        continue;
                    }

                    for (var snippetIndex = 0; snippetIndex < FeatureRegistrationSnippets.Length; snippetIndex++)
                    {
                        var methodName = FeatureRegistrationSnippets[snippetIndex].MethodName;
                        if (!result.Contains(methodName) &&
                            text.IndexOf(methodName, StringComparison.Ordinal) >= 0)
                        {
                            result.Add(methodName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Evo Setup] Failed to scan project scripts for registration diagnostics: {ex.Message}");
            }

            return result;
        }

        private string BuildFeatureRegistrationSnippet(bool includeOnlyMissing)
        {
            var snippets = includeOnlyMissing
                ? CollectMissingFeatureRegistrationSnippets()
                : FeatureRegistrationSnippets
                    .Where(snippet => IsEvoPackageSelectedOrInstalled(snippet.PackageName))
                    .ToList();

            return snippets.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, snippets.Select(snippet => snippet.Call));
        }

        private bool IsEvoPackageSelectedOrInstalled(string packageName)
        {
            return _selectedEvoPackageNames.Contains(packageName) || _installedEvoPackageNames.Contains(packageName);
        }

        private static List<string> FindLegacyRuntimeInstallerCallSites(int maxResults)
        {
            var results = new List<string>();
            var assetsRoot = Path.Combine(GetProjectRootPath(), "Assets");
            if (!Directory.Exists(assetsRoot))
            {
                return results;
            }

            try
            {
                var files = Directory.GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories);
                for (var i = 0; i < files.Length && results.Count < maxResults; i++)
                {
                    var file = files[i];
                    string text;
                    try
                    {
                        text = File.ReadAllText(file);
                    }
                    catch
                    {
                        continue;
                    }

                    if (text.IndexOf("YandexRuntimeInstaller", StringComparison.Ordinal) < 0 &&
                        text.IndexOf("CrazyGamesRuntimeInstaller", StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    results.Add(ToProjectRelativePath(file));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Evo Setup] Failed to scan legacy runtime installer call sites: {ex.Message}");
            }

            return results;
        }

        private static string ToProjectRelativePath(string fullPath)
        {
            var projectRoot = GetProjectRootPath();
            if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/");
            }

            return fullPath.Replace("\\", "/");
        }

        private static string GetEvoPackageSource(string packageName)
        {
            return GetEvoPackageSource(packageName, GetEvoPackageTargetTag(packageName));
        }

        private static string GetEvoPackageSource(string packageName, string tag)
        {
            return $"https://github.com/illiden228/EvoInfrastructure.git?path=Packages/{packageName}#{tag}";
        }

        private static string GetEvoPackageTargetTag(string packageName)
        {
            if (packageName.StartsWith(CrazyGamesPackageName, StringComparison.OrdinalIgnoreCase))
            {
                return CrazyGamesGitTag;
            }

            if (packageName.StartsWith(YandexPackageName, StringComparison.OrdinalIgnoreCase))
            {
                return YandexGitTag;
            }

            return RuntimeGitTag;
        }

        private static string GetEvoPackageDisplayName(string packageName)
        {
            var package = FindEvoPackage(packageName);
            return package != null ? package.DisplayName : packageName;
        }

        private static string GetEvoPackageDisplayNameOrId(string packageName)
        {
            var displayName = GetEvoPackageDisplayName(packageName);
            return string.Equals(displayName, packageName, StringComparison.OrdinalIgnoreCase)
                ? packageName
                : $"{displayName} ({packageName})";
        }

        private static IReadOnlyList<ExternalPackageDependency> ResolveExternalDependencies(string packageName)
        {
            switch (packageName)
            {
                case "com.evo.infrastructure.di":
                    return new[] { ExternalPackageDependency.VContainer };
                case "com.evo.infrastructure.config":
                case "com.evo.infrastructure.platform":
                case "com.evo.infrastructure.leaderboards":
                    return new[] { ExternalPackageDependency.VContainer };
                case "com.evo.infrastructure.resources":
                case "com.evo.infrastructure.scene":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask, ExternalPackageDependency.Addressables };
                case "com.evo.infrastructure.save":
                case "com.evo.infrastructure.analytics":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask };
                case "com.evo.infrastructure.ads":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask, ExternalPackageDependency.ReactiveNuGets };
                case "com.evo.infrastructure.localization":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask, ExternalPackageDependency.Localization };
                case "com.evo.infrastructure.focus":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.ReactiveNuGets, ExternalPackageDependency.InputSystem };
                case "com.evo.infrastructure.audio":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask, ExternalPackageDependency.Addressables };
                case "com.evo.infrastructure.pooling":
                    return new[] { ExternalPackageDependency.UniTask };
                case "com.evo.infrastructure.loading":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask, ExternalPackageDependency.Addressables, ExternalPackageDependency.Localization };
                case "com.evo.infrastructure.ui":
                    return new[] { ExternalPackageDependency.VContainer, ExternalPackageDependency.UniTask, ExternalPackageDependency.ReactiveNuGets, ExternalPackageDependency.R3Unity, ExternalPackageDependency.Addressables, ExternalPackageDependency.InputSystem, ExternalPackageDependency.Ugui, ExternalPackageDependency.PrimeTween };
                case "com.evo.infrastructure.editor-tools":
                    return new[] { ExternalPackageDependency.Addressables, ExternalPackageDependency.Localization };
                case "com.evo.infrastructure.yandex":
                case "com.evo.infrastructure.yandex.platform":
                case "com.evo.infrastructure.yandex.ads":
                case "com.evo.infrastructure.yandex.analytics":
                case "com.evo.infrastructure.yandex.save":
                case "com.evo.infrastructure.yandex.leaderboards":
                    return new[] { ExternalPackageDependency.PluginYg };
                default:
                    return Array.Empty<ExternalPackageDependency>();
            }
        }

        private static string GetExternalDependencyDisplayName(ExternalPackageDependency dependency)
        {
            switch (dependency)
            {
                case ExternalPackageDependency.VContainer:
                    return "VContainer";
                case ExternalPackageDependency.UniTask:
                    return "UniTask";
                case ExternalPackageDependency.NuGetForUnity:
                    return "NuGetForUnity";
                case ExternalPackageDependency.ReactiveNuGets:
                    return "Reactive NuGets";
                case ExternalPackageDependency.R3Unity:
                    return "R3.Unity";
                case ExternalPackageDependency.Addressables:
                    return "Addressables";
                case ExternalPackageDependency.Localization:
                    return "Unity Localization";
                case ExternalPackageDependency.InputSystem:
                    return "Input System";
                case ExternalPackageDependency.Ugui:
                    return "Unity UI";
                case ExternalPackageDependency.PrimeTween:
                    return "PrimeTween";
                case ExternalPackageDependency.PluginYg:
                    return "PluginYG2";
                default:
                    return dependency.ToString();
            }
        }

        private static EvoPackageDescriptor FindEvoPackage(string packageName)
        {
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                if (string.Equals(EvoPackages[i].Id, packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return EvoPackages[i];
                }
            }

            return null;
        }

        private bool IsEvoPackageInstalled(string packageName)
        {
            return _installedEvoPackageNames.Contains(packageName);
        }

        private HashSet<string> CollectSelectedEvoPackageClosure()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var packageName in _selectedEvoPackageNames)
            {
                AddEvoPackageWithDependencies(packageName, result);
            }

            return result;
        }

        private static void AddEvoPackageWithDependencies(string packageName, HashSet<string> result)
        {
            var package = FindEvoPackage(packageName);
            if (package == null || !result.Add(package.Id))
            {
                return;
            }

            for (var i = 0; i < package.Dependencies.Count; i++)
            {
                AddEvoPackageWithDependencies(package.Dependencies[i], result);
            }
        }

        private HashSet<ExternalPackageDependency> CollectRequiredExternalDependenciesForSelectedEvoPackages()
        {
            var dependencies = new HashSet<ExternalPackageDependency>();
            var selectedPackages = CollectSelectedEvoPackageClosure();
            foreach (var packageName in selectedPackages)
            {
                var package = FindEvoPackage(packageName);
                if (package == null)
                {
                    continue;
                }

                for (var i = 0; i < package.ExternalDependencies.Count; i++)
                {
                    dependencies.Add(package.ExternalDependencies[i]);
                }
            }

            return dependencies;
        }

        private bool IsExternalDependencyRequiredBySelectedEvoPackages(ExternalPackageDependency dependency)
        {
            return CollectRequiredExternalDependenciesForSelectedEvoPackages().Contains(dependency);
        }

        private void SelectEvoPackageWithDependencies(string packageName)
        {
            SelectEvoPackageWithDependencies(packageName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private void SelectLegacyRuntimeMigrationPackages()
        {
            for (var i = 0; i < DefaultSelectedEvoPackageNames.Length; i++)
            {
                SelectEvoPackageWithDependencies(DefaultSelectedEvoPackageNames[i]);
            }

            if (IsEvoPackageInstalled(YandexPackageName))
            {
                SelectEvoPackageCategory("Yandex");
            }

            if (IsEvoPackageInstalled(CrazyGamesPackageName))
            {
                SelectEvoPackageCategory("CrazyGames");
            }
        }

        private void SelectEvoPackageWithDependencies(string packageName, HashSet<string> visited)
        {
            var package = FindEvoPackage(packageName);
            if (package == null || !visited.Add(package.Id))
            {
                return;
            }

            _selectedEvoPackageNames.Add(package.Id);
            SelectExternalDependencies(package.ExternalDependencies);
            if (string.Equals(package.Category, "Yandex", StringComparison.Ordinal))
            {
                _installPluginYgPackage = true;
            }

            for (var i = 0; i < package.Dependencies.Count; i++)
            {
                SelectEvoPackageWithDependencies(package.Dependencies[i], visited);
            }
        }

        private void DeselectEvoPackageWithDependents(string packageName)
        {
            DeselectEvoPackageWithDependents(packageName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private void DeselectEvoPackageWithDependents(string packageName, HashSet<string> visited)
        {
            if (!visited.Add(packageName))
            {
                return;
            }

            var canonicalPackage = FindEvoPackage(packageName);
            _selectedEvoPackageNames.Remove(canonicalPackage?.Id ?? packageName);
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                var package = EvoPackages[i];
                if (package.Dependencies.Any(dependency => string.Equals(dependency, packageName, StringComparison.OrdinalIgnoreCase)))
                {
                    DeselectEvoPackageWithDependents(package.Id, visited);
                }
            }
        }

        private void DeselectEvoPackagesRequiringExternalDependency(ExternalPackageDependency dependency)
        {
            var affectedPackages = EvoPackages
                .Where(package => package.ExternalDependencies.Contains(dependency))
                .OrderByDescending(package => GetEvoPackageInstallOrder(package.Id))
                .Select(package => package.Id)
                .ToArray();

            for (var i = 0; i < affectedPackages.Length; i++)
            {
                DeselectEvoPackageWithDependents(affectedPackages[i]);
            }
        }

        private void SelectExternalDependencies(IReadOnlyList<ExternalPackageDependency> dependencies)
        {
            for (var i = 0; i < dependencies.Count; i++)
            {
                SelectExternalDependency(dependencies[i]);
            }
        }

        private void NormalizeExternalDependencySelection()
        {
            var selectedPackages = _selectedEvoPackageNames.ToArray();
            for (var i = 0; i < selectedPackages.Length; i++)
            {
                var package = FindEvoPackage(selectedPackages[i]);
                if (package == null)
                {
                    continue;
                }

                SelectExternalDependencies(package.ExternalDependencies);
            }
        }

        private void SelectExternalDependency(ExternalPackageDependency dependency)
        {
            switch (dependency)
            {
                case ExternalPackageDependency.VContainer:
                    _installVContainer = true;
                    break;
                case ExternalPackageDependency.UniTask:
                    _installUniTask = true;
                    break;
                case ExternalPackageDependency.NuGetForUnity:
                    _installNuGetForUnity = true;
                    break;
                case ExternalPackageDependency.ReactiveNuGets:
                    _installNuGetForUnity = true;
                    _installReactiveNuGets = true;
                    break;
                case ExternalPackageDependency.R3Unity:
                    _installR3Unity = true;
                    break;
                case ExternalPackageDependency.Addressables:
                    _installAddressables = true;
                    break;
                case ExternalPackageDependency.Localization:
                    _installLocalization = true;
                    break;
                case ExternalPackageDependency.InputSystem:
                    _installInputSystem = true;
                    break;
                case ExternalPackageDependency.Ugui:
                    _installUgui = true;
                    break;
                case ExternalPackageDependency.PrimeTween:
                    _installPrimeTween = true;
                    break;
                case ExternalPackageDependency.PluginYg:
                    _installPluginYgPackage = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }

        private bool IsExternalDependencySelectedOrInstalled(ExternalPackageDependency dependency)
        {
            switch (dependency)
            {
                case ExternalPackageDependency.VContainer:
                    return _installVContainer || _vContainerInstalled;
                case ExternalPackageDependency.UniTask:
                    return _installUniTask || _uniTaskInstalled;
                case ExternalPackageDependency.NuGetForUnity:
                    return _installNuGetForUnity || _nuGetForUnityInstalled;
                case ExternalPackageDependency.ReactiveNuGets:
                    return _installReactiveNuGets || AreReactiveReadyOrConfigured();
                case ExternalPackageDependency.R3Unity:
                    return _installR3Unity || _r3UnityInstalled;
                case ExternalPackageDependency.Addressables:
                    return _installAddressables || _addressablesInstalled;
                case ExternalPackageDependency.Localization:
                    return _installLocalization || _localizationInstalled;
                case ExternalPackageDependency.InputSystem:
                    return _installInputSystem || _inputSystemInstalled;
                case ExternalPackageDependency.Ugui:
                    return _installUgui || _uguiInstalled;
                case ExternalPackageDependency.PrimeTween:
                    return _installPrimeTween || _primeTweenInstalled;
                case ExternalPackageDependency.PluginYg:
                    return _installPluginYgPackage || _pluginYgInstalled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }

        private bool ShouldInstallExternalDependency(ExternalPackageDependency dependency)
        {
            return IsExternalDependencySelected(dependency) ||
                   IsExternalDependencyRequiredBySelectedEvoPackages(dependency) ||
                   (dependency == ExternalPackageDependency.NuGetForUnity && IsExternalDependencyRequiredBySelectedEvoPackages(ExternalPackageDependency.ReactiveNuGets));
        }

        private bool ShouldConfigureReactiveNuGets()
        {
            return _installReactiveNuGets ||
                   IsExternalDependencyRequiredBySelectedEvoPackages(ExternalPackageDependency.ReactiveNuGets);
        }

        private bool IsExternalDependencySelected(ExternalPackageDependency dependency)
        {
            switch (dependency)
            {
                case ExternalPackageDependency.VContainer:
                    return _installVContainer;
                case ExternalPackageDependency.UniTask:
                    return _installUniTask;
                case ExternalPackageDependency.NuGetForUnity:
                    return _installNuGetForUnity;
                case ExternalPackageDependency.ReactiveNuGets:
                    return _installReactiveNuGets;
                case ExternalPackageDependency.R3Unity:
                    return _installR3Unity;
                case ExternalPackageDependency.Addressables:
                    return _installAddressables;
                case ExternalPackageDependency.Localization:
                    return _installLocalization;
                case ExternalPackageDependency.InputSystem:
                    return _installInputSystem;
                case ExternalPackageDependency.Ugui:
                    return _installUgui;
                case ExternalPackageDependency.PrimeTween:
                    return _installPrimeTween;
                case ExternalPackageDependency.PluginYg:
                    return _installPluginYgPackage;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }

        private void SelectEvoPackageCategory(string category)
        {
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                if (string.Equals(EvoPackages[i].Category, category, StringComparison.Ordinal))
                {
                    SelectEvoPackageWithDependencies(EvoPackages[i].Id);
                }
            }
        }

        private void DeselectEvoPackageCategory(string category)
        {
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                if (string.Equals(EvoPackages[i].Category, category, StringComparison.Ordinal))
                {
                    DeselectEvoPackageWithDependents(EvoPackages[i].Id);
                }
            }
        }

        private static int GetEvoPackageInstallOrder(string packageName)
        {
            return GetEvoPackageInstallOrder(packageName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static int GetEvoPackageDescriptorIndex(string packageName)
        {
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                if (string.Equals(EvoPackages[i].Id, packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private static int GetEvoPackageInstallOrder(string packageName, HashSet<string> visited)
        {
            if (!visited.Add(packageName))
            {
                return 0;
            }

            var package = FindEvoPackage(packageName);
            if (package == null || package.Dependencies.Count == 0)
            {
                return 0;
            }

            var max = 0;
            for (var i = 0; i < package.Dependencies.Count; i++)
            {
                max = Math.Max(max, GetEvoPackageInstallOrder(package.Dependencies[i], visited) + 1);
            }

            return max;
        }

        private List<string> CollectSelectedUpmPackagesToInstall()
        {
            var foundation = CollectFoundationPackagesToInstall();
            if (foundation.Count > 0)
            {
                return foundation;
            }

            if (ShouldConfigureReactiveNuGets() && !AreReactiveAssembliesReady())
            {
                return new List<string>();
            }

            return CollectFrameworkPackagesToInstall();
        }

        private List<string> CollectFoundationPackagesToInstall()
        {
            var packages = new List<string>();
            if (ShouldInstallExternalDependency(ExternalPackageDependency.VContainer) && !_vContainerInstalled) packages.Add(VContainerSource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.UniTask) && !_uniTaskInstalled) packages.Add(UniTaskSource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.NuGetForUnity) && !_nuGetForUnityInstalled) packages.Add(NuGetForUnitySource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.Addressables) && !_addressablesInstalled) packages.Add(AddressablesSource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.Localization) && !_localizationInstalled) packages.Add(LocalizationSource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.InputSystem) && !_inputSystemInstalled) packages.Add(InputSystemSource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.Ugui) && !_uguiInstalled) packages.Add(UguiSource);
            if (ShouldInstallExternalDependency(ExternalPackageDependency.PrimeTween) && !_primeTweenInstalled) packages.Add(PrimeTweenSource);
            return packages;
        }

        private List<string> CollectFrameworkPackagesToInstall()
        {
            var packages = new List<string>();
            if (ShouldInstallExternalDependency(ExternalPackageDependency.R3Unity) && !_r3UnityInstalled) packages.Add(R3UnitySource);
            if (IsSelectedEvoPackageInstallReady(packages))
            {
                var selectedPackages = CollectSelectedEvoPackageClosure()
                    .Where(packageName => !IsEvoPackageInstalled(packageName))
                    .OrderBy(GetEvoPackageInstallOrder)
                    .ThenBy(GetEvoPackageDescriptorIndex);
                foreach (var packageName in selectedPackages)
                {
                    packages.Add(GetEvoPackageSource(packageName));
                }
            }
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
                "Evo.Infrastructure.Runtime.UI.UiSystemConfig, Evo.Infrastructure.UI",
                UiSystemConfigPath);
            EnsureConfigCatalogAsset();
            CreateScriptableAsset(
                "Evo.Infrastructure.Services.ResourceCatalog.ResourceCatalog, Evo.Infrastructure.Resources",
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
                "Evo.Infrastructure.Services.Config.ScriptableConfigCatalog, Evo.Infrastructure.Config",
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
            _pluginYgImportRequested = false;
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
            SessionState.SetBool(GetPluginYgImportStateKey(), false);
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
                _pluginYgInstalled = IsPluginYgInstalled();
                _pluginYgDefineReady = IsPluginYgDefineReady();
                if (_pluginYgInstalled)
                {
                    _pluginYgImportRequested = false;
                    SessionState.SetBool(GetPluginYgImportStateKey(), false);
                }
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
            _pluginYgInstalled = IsPluginYgInstalled();
            _pluginYgDefineReady = IsPluginYgDefineReady();
            _odinInstalled = IsOdinInstalled();
            ValidateTemplatesAndScaffoldScriptsState();
            _bootstrapScopesReady = AreBootstrapScopesValid();
            _starterAddressablesReady = AreStarterScenesAddressable();
            _starterBuildScenesReady = AreStarterBuildScenesReady();
            _stateAnalyzed = _listRequest.Status == StatusCode.Success;
            _isRefreshingState = false;
            RefreshCachedEvoPackageDisplay();
            RefreshCachedHeavyDiagnostics();
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
            _customScaffoldScriptPaths.Clear();
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
                else if (IsExistingScaffoldScriptCustomized(StarterScriptTemplates[i]))
                {
                    _customScaffoldScriptPaths.Add(StarterScriptTemplates[i].TargetPath);
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
                        $"[Evo Setup] Scaffold script differs from template and will be preserved as project-owned: {template.TargetPath}. " +
                        $"Template={templatePath} ({GetFileLength(templatePath)} bytes), " +
                        $"Target={GetFileLength(template.TargetPath)} bytes.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Evo Setup] Failed to compare scaffold script '{template.TargetPath}': {ex.Message}");
                return false;
            }
        }

        private static bool IsExistingScaffoldScriptCustomized(StarterScriptTemplate template)
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
                return !string.Equals(
                    NormalizeText(File.ReadAllText(templatePath)),
                    NormalizeText(File.ReadAllText(template.TargetPath)),
                    StringComparison.Ordinal);
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
            Debug.Log("[Evo Setup] Creating missing scaffold scripts from templates...");
            var updated = new List<string>();
            var errors = new List<string>();
            var skipped = new List<string>();

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

                    skipped.Add(Path.GetFileName(template.TargetPath));
                    Debug.LogWarning(
                        $"[Evo Setup] Scaffold script differs from template and was left untouched: {template.TargetPath}. " +
                        "Update it manually if you want the new starter code.");
                    continue;
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
                ? skipped.Count == 0
                    ? "Scaffold scripts are already up to date."
                    : "Scaffold scripts differ and were left untouched: " + string.Join(", ", skipped)
                : "Created scaffold scripts: " + string.Join(", ", updated);
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

        private static bool IsPluginYgInstalled()
        {
            return FindTypeByName("YG.YG2") != null ||
                   FindTypeByName("YG.SavesYG") != null ||
                   AssetDatabase.IsValidFolder(PluginYgFolderPath) ||
                   Directory.Exists(Path.Combine(GetProjectRootPath(), PluginYgFolderPath));
        }

        private static bool IsPluginYgDefineReady()
        {
            return IsDefineSymbolEnabled("YandexGamesPlatform_yg");
        }

        private bool TryImportPluginYgPackage()
        {
            if (IsPluginYgInstalled())
            {
                _pluginYgInstalled = true;
                _pluginYgDefineReady = IsPluginYgDefineReady();
                _pluginYgImportRequested = false;
                SessionState.SetBool(GetPluginYgImportStateKey(), false);
                _statusLine = "PluginYG2 is already installed.";
                return true;
            }

            if (!TryDownloadLatestPluginYgPackage(out var packagePath, out var error))
            {
                _statusLine = "PluginYG2 import failed: " + error;
                Debug.LogError("[Evo Setup] " + _statusLine);
                return false;
            }

            _statusLine = $"Importing PluginYG2 package: {Path.GetFileName(packagePath)}";
            Debug.Log("[Evo Setup] " + _statusLine);
            _pluginYgImportRequested = true;
            SessionState.SetBool(GetPluginYgImportStateKey(), true);
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();

            _pluginYgInstalled = IsPluginYgInstalled();
            _pluginYgDefineReady = IsPluginYgDefineReady();
            if (_pluginYgInstalled)
            {
                _pluginYgImportRequested = false;
                SessionState.SetBool(GetPluginYgImportStateKey(), false);
            }

            return true;
        }

        private static bool TryDownloadLatestPluginYgPackage(out string packagePath, out string error)
        {
            packagePath = string.Empty;
            error = string.Empty;

            try
            {
                using var webClient = new System.Net.WebClient();
                webClient.Headers.Add("User-Agent", "Evo-Infrastructure-Setup");
                var releaseJson = webClient.DownloadString(PluginYgLatestReleaseApiUrl);
                var release = JsonUtility.FromJson<GitHubReleaseInfo>(releaseJson);
                var asset = release?.assets?
                    .FirstOrDefault(item =>
                        item != null &&
                        !string.IsNullOrWhiteSpace(item.browser_download_url) &&
                        item.name != null &&
                        item.name.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase));

                if (asset == null)
                {
                    error = "latest GitHub release does not contain a .unitypackage asset.";
                    return false;
                }

                var cacheDirectory = Path.Combine(GetProjectRootPath(), "Library", "EvoSetup");
                Directory.CreateDirectory(cacheDirectory);
                packagePath = Path.Combine(cacheDirectory, asset.name);
                webClient.DownloadFile(asset.browser_download_url, packagePath);

                if (!File.Exists(packagePath) || new FileInfo(packagePath).Length <= 0)
                {
                    error = "downloaded unitypackage is empty.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
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
            _installedEvoPackageNames.Clear();
            _outdatedEvoPackageNames.Clear();
            var legacyRuntimePackage = FindPackage(packages, LegacyRuntimePackageName);
            _legacyRuntimeManifestDependency = GetManifestDependencyValue(LegacyRuntimePackageName);
            _legacyRuntimeInstalled = legacyRuntimePackage != null || !string.IsNullOrWhiteSpace(_legacyRuntimeManifestDependency);

            TrackEvoPackageUpdateState(packages, CorePackageName);
            for (var i = 0; i < EvoPackages.Length; i++)
            {
                var packageName = EvoPackages[i].Id;
                var package = FindPackage(packages, packageName);
                var manifestDependency = GetManifestDependencyValue(packageName);
                if (package != null || !string.IsNullOrWhiteSpace(manifestDependency))
                {
                    _installedEvoPackageNames.Add(packageName);
                    TrackEvoPackageUpdateState(packages, packageName);
                }
            }

            var installedCount = 0;
            var installedTargetCount = 0;
            string firstDifferentRevision = null;

            for (var i = 0; i < RuntimePackageNames.Length; i++)
            {
                var packageName = RuntimePackageNames[i];
                var package = FindPackage(packages, packageName);
                var manifestDependency = GetManifestDependencyValue(packageName);
                var installed = package != null || !string.IsNullOrWhiteSpace(manifestDependency);
                if (!installed)
                {
                    continue;
                }

                installedCount++;
                var updateState = ResolveEvoPackageUpdateState(
                    true,
                    package != null ? package.version ?? string.Empty : string.Empty,
                    package != null ? package.packageId ?? string.Empty : string.Empty,
                    manifestDependency,
                    RuntimeGitTag);

                if (updateState == EvoPackageUpdateState.InstalledTarget)
                {
                    installedTargetCount++;
                }
                else if (string.IsNullOrWhiteSpace(firstDifferentRevision))
                {
                    firstDifferentRevision = !string.IsNullOrWhiteSpace(manifestDependency)
                        ? manifestDependency
                        : package != null ? package.packageId ?? package.version ?? packageName : packageName;
                }
            }

            _runtimeInstalled = installedCount == RuntimePackageNames.Length;
            _runtimeManifestDependency = $"{installedCount}/{RuntimePackageNames.Length} packages";
            _runtimeInstalledVersion = _runtimeInstalled ? RuntimeGitTag.TrimStart('v', 'V') : string.Empty;
            _runtimeInstalledPackageId = _runtimeInstalled ? "Evo Infrastructure feature packages" : string.Empty;

            if (installedCount == 0)
            {
                _runtimeUpdateState = EvoPackageUpdateState.Missing;
            }
            else if (installedTargetCount == RuntimePackageNames.Length)
            {
                _runtimeUpdateState = EvoPackageUpdateState.InstalledTarget;
            }
            else
            {
                _runtimeManifestDependency = !string.IsNullOrWhiteSpace(firstDifferentRevision)
                    ? $"{installedCount}/{RuntimePackageNames.Length} packages, {firstDifferentRevision}"
                    : _runtimeManifestDependency;
                _runtimeUpdateState = EvoPackageUpdateState.InstalledDifferentRevision;
            }
        }

        private void TrackEvoPackageUpdateState(IReadOnlyList<UnityEditor.PackageManager.PackageInfo> packages, string packageName)
        {
            var package = FindPackage(packages, packageName);
            var manifestDependency = GetManifestDependencyValue(packageName);
            var installed = package != null || !string.IsNullOrWhiteSpace(manifestDependency);
            if (!installed)
            {
                return;
            }

            var updateState = ResolveEvoPackageUpdateState(
                true,
                package != null ? package.version ?? string.Empty : string.Empty,
                package != null ? package.packageId ?? string.Empty : string.Empty,
                manifestDependency,
                GetEvoPackageTargetTag(packageName));
            if (updateState == EvoPackageUpdateState.InstalledDifferentRevision)
            {
                _outdatedEvoPackageNames.Add(packageName);
            }
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

        private static bool IsPackageDependencyPresent(string packageName)
        {
            return !string.IsNullOrWhiteSpace(GetManifestDependencyValue(packageName));
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

                Debug.LogWarning(
                    $"[Evo Setup] Scaffold script exists and differs from template, leaving it untouched: {targetPath}. " +
                    $"Template={templatePath} ({GetTextByteCount(templateText)} bytes), " +
                    $"ExistingTarget={GetTextByteCount(targetText)} bytes. Update manually if needed.");
                return;
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
            LoadEvoPackageSelectionState();
            _installPluginYgPackage = GetSelection(nameof(_installPluginYgPackage), _installPluginYgPackage);
            _installOdinPackage = GetSelection(nameof(_installOdinPackage), _installOdinPackage);
            _installProjectStructure = GetSelection(nameof(_installProjectStructure), _installProjectStructure);
            _installStarterScaffold = GetSelection(nameof(_installStarterScaffold), _installStarterScaffold);
            NormalizeExternalDependencySelection();
            RefreshCachedEvoPackageDisplay();
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
            SaveEvoPackageSelectionState();
            SetSelection(nameof(_installPluginYgPackage), _installPluginYgPackage);
            SetSelection(nameof(_installOdinPackage), _installOdinPackage);
            SetSelection(nameof(_installProjectStructure), _installProjectStructure);
            SetSelection(nameof(_installStarterScaffold), _installStarterScaffold);
        }

        private void SetSelectionField(ref bool field, bool value)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            MarkSelectionStateDirty();
        }

        private void MarkSelectionStateDirty()
        {
            _selectionStateDirty = true;
            RefreshCachedEvoPackageDisplay();
            SaveSelectionStateIfDirty();
        }

        private void MarkSelectionStateDirtyIfChanged(string previousSignature)
        {
            if (string.Equals(previousSignature, BuildSelectionStateSignature(), StringComparison.Ordinal))
            {
                return;
            }

            MarkSelectionStateDirty();
        }

        private string BuildSelectionStateSignature()
        {
            return string.Join("|", _selectedEvoPackageNames.OrderBy(GetEvoPackageInstallOrder).ThenBy(GetEvoPackageDescriptorIndex)) +
                   $"|{_installVContainer}|{_installUniTask}|{_installNuGetForUnity}|{_installAddressables}|{_installLocalization}|{_installInputSystem}|{_installUgui}|{_installPrimeTween}|{_installR3Unity}|{_installReactiveNuGets}|{_installPluginYgPackage}|{_installOdinPackage}|{_installProjectStructure}|{_installStarterScaffold}";
        }

        private void SaveSelectionStateIfDirty()
        {
            if (!_selectionStateDirty)
            {
                return;
            }

            SaveSelectionState();
            _selectionStateDirty = false;
        }

        private void LoadEvoPackageSelectionState()
        {
            _selectedEvoPackageNames.Clear();
            var value = SessionState.GetString(GetSelectionStateKey("EvoPackages"), string.Empty);
            var source = string.IsNullOrWhiteSpace(value)
                ? DefaultSelectedEvoPackageNames
                : value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < source.Length; i++)
            {
                SelectEvoPackageWithDependencies(source[i]);
            }
        }

        private void SaveEvoPackageSelectionState()
        {
            SessionState.SetString(
                GetSelectionStateKey("EvoPackages"),
                string.Join("|", _selectedEvoPackageNames
                    .OrderBy(GetEvoPackageInstallOrder)
                    .ThenBy(GetEvoPackageDescriptorIndex)));
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
            _pluginYgImportRequested = SessionState.GetBool(GetPluginYgImportStateKey(), false);
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

            if (HasSelectedEvoPackagesToInstall() && !IsSelectedEvoPackageInstallReady(null))
            {
                StopSetupWithError("Setup stopped: Evo feature package prerequisites are not ready. Install selected runtime dependencies first.");
                return;
            }

            if (_installPluginYgPackage && !_pluginYgInstalled)
            {
                if (_pluginYgImportRequested)
                {
                    if (IsPluginYgInstalled())
                    {
                        _pluginYgInstalled = true;
                        _pluginYgDefineReady = IsPluginYgDefineReady();
                        _pluginYgImportRequested = false;
                        SessionState.SetBool(GetPluginYgImportStateKey(), false);
                        _statusLine = "Setup: PluginYG2 import completed.";
                        RefreshState();
                        return;
                    }

                    _statusLine = "Setup: waiting for PluginYG2 import to finish...";
                    return;
                }

                _statusLine = "Setup: downloading and importing PluginYG2...";
                Debug.Log("[Evo Setup] Downloading and importing PluginYG2 from the latest GitHub release...");
                if (!TryImportPluginYgPackage())
                {
                    StopSetupWithError(_statusLine);
                    return;
                }

                QueueRefreshBurst();
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
            if (_installPluginYgPackage) count++;
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
            if (_installPluginYgPackage && _pluginYgInstalled) completed++;
            if (_installOdinPackage && _odinInstalled) completed++;
            if (_installProjectStructure && _structureReady) completed++;
            if (_installStarterScaffold && IsStarterScaffoldReady()) completed++;
            return completed;
        }

        private bool HasSelectedUpmPackagesToInstall()
        {
            return CollectSelectedUpmPackagesToInstall().Count > 0;
        }

        private bool HasSelectedEvoPackagesToInstall()
        {
            return _selectedEvoPackageNames.Any(packageName => !IsEvoPackageInstalled(packageName));
        }

        private bool HasSelectedSetupWork()
        {
            return HasSelectedUpmPackagesToInstall() ||
                   (_installReactiveNuGets && !AreReactiveAssembliesReady()) ||
                   (_installPluginYgPackage && !_pluginYgInstalled) ||
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

        private static bool IsDefineSymbolEnabled(string define)
        {
            if (string.IsNullOrWhiteSpace(define))
            {
                return false;
            }

            var groups = new[]
            {
                BuildTargetGroup.Standalone,
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.WebGL
            };

            for (var i = 0; i < groups.Length; i++)
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(groups[i]);
                if (string.IsNullOrWhiteSpace(symbols))
                {
                    continue;
                }

                var parts = symbols.Split(';');
                for (var j = 0; j < parts.Length; j++)
                {
                    if (string.Equals(parts[j].Trim(), define, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsSelectedEvoPackageInstallReady(ICollection<string> packagesBeingAdded)
        {
            var requiredDependencies = CollectRequiredExternalDependenciesForSelectedEvoPackages();
            var r3UnityWillBeReady = _r3UnityInstalled ||
                                     (packagesBeingAdded != null && packagesBeingAdded.Contains(R3UnitySource));
            return (!requiredDependencies.Contains(ExternalPackageDependency.VContainer) || _vContainerInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.UniTask) || _uniTaskInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.Addressables) || _addressablesInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.Localization) || _localizationInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.InputSystem) || _inputSystemInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.Ugui) || _uguiInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.PrimeTween) || _primeTweenInstalled) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.ReactiveNuGets) || AreReactiveAssembliesReady()) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.R3Unity) || r3UnityWillBeReady) &&
                   (!requiredDependencies.Contains(ExternalPackageDependency.PluginYg) || _pluginYgInstalled || _installPluginYgPackage);
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
                   AreSelectedEvoPackagesInstalled() &&
                   (!_installPluginYgPackage || _pluginYgInstalled);
        }

        private bool ArePackagesReadyForStarterScaffold()
        {
            return AreStarterEvoPackagesInstalled() &&
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

        private bool AreSelectedEvoPackagesInstalled()
        {
            return _selectedEvoPackageNames.All(IsEvoPackageInstalled);
        }

        private bool AreStarterEvoPackagesInstalled()
        {
            return DefaultSelectedEvoPackageNames.All(IsEvoPackageInstalled);
        }

        private static string GetOneClickStateKey()
        {
            return OneClickStateKeyPrefix + Application.dataPath.GetHashCode();
        }

        private static string GetOdinImportStateKey()
        {
            return OdinImportStateKeyPrefix + Application.dataPath.GetHashCode();
        }

        private static string GetPluginYgImportStateKey()
        {
            return PluginYgImportStateKeyPrefix + Application.dataPath.GetHashCode();
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

        private readonly struct FeatureRegistrationSnippet
        {
            public readonly string PackageName;
            public readonly string MethodName;
            public readonly string Call;

            public FeatureRegistrationSnippet(string packageName, string methodName, string call)
            {
                PackageName = packageName;
                MethodName = methodName;
                Call = call;
            }
        }

        private readonly struct ExternalDependencyRow
        {
            public readonly ExternalPackageDependency Dependency;
            public readonly string Label;
            public readonly bool Selected;
            public readonly bool Installed;
            public readonly string Details;
            public readonly Action RemoveAction;

            public ExternalDependencyRow(
                ExternalPackageDependency dependency,
                string label,
                bool selected,
                bool installed,
                string details,
                Action removeAction)
            {
                Dependency = dependency;
                Label = label;
                Selected = selected;
                Installed = installed;
                Details = details;
                RemoveAction = removeAction;
            }
        }

        [Serializable]
        private sealed class GitHubReleaseInfo
        {
            public string tag_name;
            public GitHubReleaseAssetInfo[] assets;
        }

        [Serializable]
        private sealed class GitHubTagInfo
        {
            public string name;
        }

        [Serializable]
        private sealed class GitHubReleaseAssetInfo
        {
            public string name;
            public string browser_download_url;
        }

        private static class JsonHelper
        {
            public static T[] FromJson<T>(string json)
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Array.Empty<T>();
                }

                var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>("{\"items\":" + json + "}");
                return wrapper?.items ?? Array.Empty<T>();
            }

            [Serializable]
            private sealed class JsonArrayWrapper<T>
            {
                public T[] items;
            }
        }
    }
}
#endif
