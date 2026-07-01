using System;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "IncrementIosBuildNumberStep", menuName = "EvoTools/Build/Steps/Increment iOS Build Number")]
    public sealed class IncrementIosBuildNumberStep : EvoBuildStepAsset, IEvoBuildCleanupStep
    {
        [Tooltip("When enabled, the previous iOS buildNumber is restored if the build is cancelled or BuildPipeline fails.")]
        [SerializeField] private bool restoreIfBuildDoesNotSucceed = true;
        [NonSerialized] private bool changedThisRun;
        [NonSerialized] private string previousBuildNumber;

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.iOS)
            {
                result.AddMessage("iOS build number bump skipped for non-iOS target.");
                return true;
            }

            if (context.HasCiBuildNumber)
            {
                result.AddMessage($"iOS buildNumber bump skipped because CI tag already set buildNumber {context.CiRequest.ParsedTag.BuildNumber}.");
                return true;
            }

            var current = PlayerSettings.iOS.buildNumber;
            var numeric = int.TryParse(current, out var value) ? Math.Max(0, value) : 0;
            var next = (numeric + 1).ToString();
            previousBuildNumber = current;
            changedThisRun = true;
            PlayerSettings.iOS.buildNumber = next;
            result.AddMessage($"iOS buildNumber: {current} -> {next}");
            AssetDatabase.SaveAssets();
            return true;
        }

        public void Cleanup(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (!restoreIfBuildDoesNotSucceed || !changedThisRun || result?.BuildSucceeded == true)
            {
                changedThisRun = false;
                previousBuildNumber = null;
                return;
            }

            var current = PlayerSettings.iOS.buildNumber;
            PlayerSettings.iOS.buildNumber = previousBuildNumber ?? string.Empty;
            result?.AddMessage($"iOS buildNumber restored after unsuccessful build: {current} -> {PlayerSettings.iOS.buildNumber}");
            AssetDatabase.SaveAssets();
            changedThisRun = false;
            previousBuildNumber = null;
        }
    }
}
