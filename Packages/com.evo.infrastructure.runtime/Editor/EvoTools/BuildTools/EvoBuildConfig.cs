using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public enum EvoBuildMode
    {
        Release = 0,
        Debug = 1
    }

    public enum EvoDefineCleanupPolicy
    {
        WarnBeforeRemove = 0,
        WarnAndPreserve = 1,
        FailIfUnknown = 2
    }

    [CreateAssetMenu(fileName = "BuildGlobalConfig", menuName = "EvoTools/Build/Build Global Config")]
    public sealed class BuildGlobalConfig : ScriptableObject
    {
        [SerializeField] private EvoDefineCleanupPolicy defineCleanupPolicy = EvoDefineCleanupPolicy.WarnBeforeRemove;
        [SerializeField] private List<string> commonDefines = new();
        [SerializeField] private List<string> debugDefines = new() { "FULL_LOG" };
        [SerializeField] private List<PlatformBuildProfile> profiles = new();
        [SerializeField] private string outputPathPattern = "Builds/{profileId}/{productName}_{version}";

        public EvoDefineCleanupPolicy DefineCleanupPolicy => defineCleanupPolicy;
        public IReadOnlyList<string> CommonDefines => commonDefines;
        public IReadOnlyList<string> DebugDefines => debugDefines;
        public IReadOnlyList<PlatformBuildProfile> Profiles => profiles;
        public string OutputPathPattern => outputPathPattern;

        internal bool AddProfileIfMissing(PlatformBuildProfile profile)
        {
            if (profile == null || profiles.Contains(profile))
            {
                return false;
            }

            profiles.Add(profile);
            return true;
        }
    }

    [CreateAssetMenu(fileName = "PlatformBuildProfile", menuName = "EvoTools/Build/Platform Build Profile")]
    public sealed class PlatformBuildProfile : ScriptableObject
    {
        [SerializeField] private string profileId;
        [SerializeField] private string platformId;
        [SerializeField] private string displayName;
        [SerializeField] private bool showInGeneratedMenu = true;
        [SerializeField] private EvoBuildMode buildMode = EvoBuildMode.Release;
        [SerializeField] private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        [SerializeField] private BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;
        [SerializeField] private List<string> platformDefines = new();
        [SerializeField] private List<string> profileDefines = new();
        [SerializeField] private PlayerSettingsOverrides playerSettings = new();
        [SerializeField] private string outputPathTemplate;
        [SerializeField] private BuildOptions buildOptions;

        public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? name : profileId;
        public string PlatformId => platformId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ProfileId : displayName;
        public bool ShowInGeneratedMenu => showInGeneratedMenu;
        public EvoBuildMode BuildMode => buildMode;
        public BuildTarget BuildTarget => buildTarget;
        public BuildTargetGroup BuildTargetGroup => buildTargetGroup;
        public IReadOnlyList<string> PlatformDefines => platformDefines;
        public IReadOnlyList<string> ProfileDefines => profileDefines;
        public PlayerSettingsOverrides PlayerSettings => playerSettings;
        public string OutputPathTemplate => outputPathTemplate;
        public BuildOptions BuildOptions => buildOptions;

        internal void SetDefaults(
            string valueProfileId,
            string valuePlatformId,
            string valueDisplayName,
            BuildTarget valueBuildTarget,
            BuildTargetGroup valueBuildTargetGroup)
        {
            profileId = valueProfileId;
            platformId = valuePlatformId;
            displayName = valueDisplayName;
            buildMode = EvoBuildMode.Release;
            buildTarget = valueBuildTarget;
            buildTargetGroup = valueBuildTargetGroup;
            showInGeneratedMenu = true;
        }
    }

    [Serializable]
    public sealed class PlayerSettingsOverrides
    {
        [SerializeField] private bool overrideProductName;
        [SerializeField] private string productName;
        [SerializeField] private bool overrideBundleVersion;
        [SerializeField] private string bundleVersion;
        [SerializeField] private bool overrideApplicationIdentifier;
        [SerializeField] private string applicationIdentifier;

        public bool OverrideProductName => overrideProductName;
        public string ProductName => productName;
        public bool OverrideBundleVersion => overrideBundleVersion;
        public string BundleVersion => bundleVersion;
        public bool OverrideApplicationIdentifier => overrideApplicationIdentifier;
        public string ApplicationIdentifier => applicationIdentifier;
    }
}
