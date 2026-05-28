using System;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "IncrementIosBuildNumberStep", menuName = "EvoTools/Build/Steps/Increment iOS Build Number")]
    public sealed class IncrementIosBuildNumberStep : EvoBuildStepAsset
    {
        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.iOS)
            {
                result.AddMessage("iOS build number bump skipped for non-iOS target.");
                return true;
            }

            var current = PlayerSettings.iOS.buildNumber;
            var numeric = int.TryParse(current, out var value) ? Math.Max(0, value) : 0;
            var next = (numeric + 1).ToString();
            PlayerSettings.iOS.buildNumber = next;
            result.AddMessage($"iOS buildNumber: {current} -> {next}");
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
