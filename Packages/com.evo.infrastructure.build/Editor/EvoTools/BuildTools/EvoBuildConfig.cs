using System.Collections.Generic;
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

        internal int RemoveMissingProfileReferences()
        {
            return profiles.RemoveAll(profile => profile == null);
        }
    }
}
