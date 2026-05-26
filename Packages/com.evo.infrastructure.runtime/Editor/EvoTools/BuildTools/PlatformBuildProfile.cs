using System.Collections.Generic;
using Evo.Infrastructure.Services.Config;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "PlatformBuildProfile", menuName = "EvoTools/Build/Platform Build Profile")]
    public sealed class PlatformBuildProfile : ScriptableObject
    {
        [SerializeField] private string profileId;
        [CatalogDropdown(CatalogDropdownKind.PlatformId)]
        [SerializeField] private string platformId;
        [SerializeField] private string displayName;
        [SerializeField] private bool showInGeneratedMenu = true;
        [SerializeField] private EvoBuildMode buildMode = EvoBuildMode.Release;
        [SerializeField] private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        [SerializeField, HideInInspector] private BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;
        [SerializeField] private List<string> defines = new();
        [SerializeField, HideInInspector] private List<string> platformDefines = new();
        [SerializeField, HideInInspector] private List<string> profileDefines = new();
        [SerializeField] private PlayerSettingsOverrides playerSettings = new();
        [SerializeField] private string outputPathTemplate;
        [SerializeField] private BuildOptions buildOptions;
        [SerializeField] private List<EvoBuildStepAsset> steps = new();

        public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? name : profileId;
        public string PlatformId => platformId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ProfileId : displayName;
        public bool ShowInGeneratedMenu => showInGeneratedMenu;
        public EvoBuildMode BuildMode => buildMode;
        public BuildTarget BuildTarget => buildTarget;
        public BuildTargetGroup BuildTargetGroup
        {
            get
            {
                var group = ResolveBuildTargetGroup(buildTarget);
                return group == BuildTargetGroup.Unknown ? buildTargetGroup : group;
            }
        }
        public IReadOnlyList<string> Defines => defines;
        public PlayerSettingsOverrides PlayerSettings => playerSettings;
        public string OutputPathTemplate => outputPathTemplate;
        public BuildOptions BuildOptions => buildOptions;
        public IReadOnlyList<EvoBuildStepAsset> Steps => steps;

        private void OnValidate()
        {
            buildTargetGroup = BuildTargetGroup;
            MigrateLegacyDefines();
        }

        internal void SetDefaults(
            string valueProfileId,
            string valuePlatformId,
            string valueDisplayName,
            BuildTarget valueBuildTarget)
        {
            profileId = valueProfileId;
            platformId = valuePlatformId;
            displayName = valueDisplayName;
            buildMode = EvoBuildMode.Release;
            buildTarget = valueBuildTarget;
            buildTargetGroup = BuildTargetGroup;
            showInGeneratedMenu = true;
            MigrateLegacyDefines();
        }

        internal bool AddStepIfMissing(EvoBuildStepAsset step)
        {
            if (step == null || steps.Contains(step))
            {
                return false;
            }

            steps.Add(step);
            return true;
        }

        private void MigrateLegacyDefines()
        {
            AddLegacyDefines(platformDefines);
            AddLegacyDefines(profileDefines);
        }

        private void AddLegacyDefines(IReadOnlyList<string> legacyDefines)
        {
            if (legacyDefines == null || legacyDefines.Count == 0)
            {
                return;
            }

            defines ??= new List<string>();
            for (var i = 0; i < legacyDefines.Count; i++)
            {
                var define = legacyDefines[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(define) && !defines.Contains(define))
                {
                    defines.Add(define);
                }
            }
        }

        private static BuildTargetGroup ResolveBuildTargetGroup(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.Android => BuildTargetGroup.Android,
                BuildTarget.iOS => BuildTargetGroup.iOS,
                BuildTarget.WebGL => BuildTargetGroup.WebGL,
                BuildTarget.StandaloneLinux64 => BuildTargetGroup.Standalone,
                BuildTarget.StandaloneOSX => BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows => BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64 => BuildTargetGroup.Standalone,
                _ => BuildTargetGroup.Unknown
            };
        }
    }
}
