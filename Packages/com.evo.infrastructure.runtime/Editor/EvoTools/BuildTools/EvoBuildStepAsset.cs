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
            bool buildAndRun)
        {
            GlobalConfig = globalConfig;
            Profile = profile;
            Report = report;
            OutputPath = outputPath ?? string.Empty;
            BuildAndRun = buildAndRun;
        }

        public BuildGlobalConfig GlobalConfig { get; }
        public PlatformBuildProfile Profile { get; }
        public EvoBuildDryRunReport Report { get; }
        public string OutputPath { get; }
        public bool BuildAndRun { get; }
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
