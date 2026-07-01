using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "EvoBuildCiConfig", menuName = "EvoTools/Build/CI Config")]
    public sealed class EvoBuildCiConfig : ScriptableObject
    {
        [SerializeField] private BuildGlobalConfig globalConfig;
        [SerializeField] private PlatformCatalog platformCatalog;
        [SerializeField] private List<EvoBuildCiProfileRule> profileRules = new();

        public BuildGlobalConfig GlobalConfig => globalConfig;
        public PlatformCatalog PlatformCatalog => platformCatalog;
        public IReadOnlyList<EvoBuildCiProfileRule> ProfileRules => profileRules;

        public PlatformBuildProfile ResolveProfile(EvoBuildCiTag tag)
        {
            if (profileRules == null)
            {
                return null;
            }

            for (var i = 0; i < profileRules.Count; i++)
            {
                var rule = profileRules[i];
                if (rule != null && rule.Matches(tag))
                {
                    return rule.Profile;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class EvoBuildCiProfileRule
    {
        [SerializeField] private string platform;
        [SerializeField] private string buildType;
        [SerializeField] private string artifactType;
        [SerializeField] private string debugInfo;
        [SerializeField] private PlatformBuildProfile profile;

        public string Platform => platform;
        public string BuildType => buildType;
        public string ArtifactType => artifactType;
        public string DebugInfo => debugInfo;
        public PlatformBuildProfile Profile => profile;

        public bool Matches(EvoBuildCiTag tag)
        {
            return MatchesToken(platform, tag.Platform) &&
                   MatchesToken(buildType, tag.BuildType) &&
                   MatchesToken(artifactType, tag.ArtifactType) &&
                   MatchesDebugInfo(tag);
        }

        private bool MatchesDebugInfo(EvoBuildCiTag tag)
        {
            if (string.IsNullOrWhiteSpace(debugInfo))
            {
                return true;
            }

            return MatchesToken(debugInfo, tag.DebugInfo);
        }

        private static bool MatchesToken(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected))
            {
                return true;
            }

            return string.Equals(expected.Trim(), actual ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
