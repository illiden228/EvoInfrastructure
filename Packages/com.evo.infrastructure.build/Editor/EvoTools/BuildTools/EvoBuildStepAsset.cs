using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public enum EvoBuildStepPhase
    {
        Validate = 0,
        BeforeApply = 10,
        AfterApply = 20,
        PrepareBuild = 30,
        BeforeBuild = 40,
        AfterBuild = 50
    }

    public sealed class EvoBuildContext
    {
        public EvoBuildContext(
            BuildGlobalConfig globalConfig,
            PlatformBuildProfile profile,
            EvoBuildDryRunReport report,
            string outputPath,
            bool buildAndRun,
            EvoBuildCiRequest ciRequest = null)
        {
            GlobalConfig = globalConfig;
            Profile = profile;
            Report = report;
            OutputPath = outputPath ?? string.Empty;
            BuildAndRun = buildAndRun;
            CiRequest = ciRequest;
        }

        public BuildGlobalConfig GlobalConfig { get; }
        public PlatformBuildProfile Profile { get; }
        public EvoBuildDryRunReport Report { get; }
        public string OutputPath { get; }
        public bool BuildAndRun { get; }
        public EvoBuildCiRequest CiRequest { get; }
        public bool HasCiVersion => !string.IsNullOrWhiteSpace(CiRequest?.ParsedTag.Version);
        public bool HasCiBuildNumber => CiRequest?.ParsedTag.BuildNumber > 0;
    }

    public abstract class EvoBuildStepAsset : ScriptableObject
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private EvoBuildStepPhase phase = EvoBuildStepPhase.BeforeBuild;
        [SerializeField] private int order;

        public bool Enabled => enabled;
        public EvoBuildStepPhase Phase => phase;
        public int Order => order;

        public virtual void Validate(EvoBuildContext context, EvoBuildDryRunReport report)
        {
        }

        public virtual bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            return true;
        }
    }

    public interface IEvoBuildCleanupStep
    {
        void Cleanup(EvoBuildContext context, EvoBuildApplyResult result);
    }
}
