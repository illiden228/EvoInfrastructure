using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "IncrementAndroidVersionCodeStep", menuName = "EvoTools/Build/Steps/Increment Android Version Code")]
    public sealed class IncrementAndroidVersionCodeStep : EvoBuildStepAsset, IEvoBuildCleanupStep
    {
        [Tooltip("When enabled, the previous Android versionCode is restored if the build is cancelled or BuildPipeline fails.")]
        [SerializeField] private bool restoreIfBuildDoesNotSucceed = true;
        [System.NonSerialized] private bool changedThisRun;
        [System.NonSerialized] private int previousVersionCode;

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.Android)
            {
                result.AddMessage("Android versionCode bump skipped for non-Android target.");
                return true;
            }

            if (context.HasCiBuildNumber)
            {
                result.AddMessage($"Android versionCode bump skipped because CI tag already set buildNumber {context.CiRequest.ParsedTag.BuildNumber}.");
                return true;
            }

            var current = PlayerSettings.Android.bundleVersionCode;
            previousVersionCode = current;
            changedThisRun = true;
            PlayerSettings.Android.bundleVersionCode = current + 1;
            result.AddMessage($"Android versionCode: {current} -> {PlayerSettings.Android.bundleVersionCode}");
            AssetDatabase.SaveAssets();
            return true;
        }

        public void Cleanup(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (!restoreIfBuildDoesNotSucceed || !changedThisRun || result?.BuildSucceeded == true)
            {
                changedThisRun = false;
                return;
            }

            var current = PlayerSettings.Android.bundleVersionCode;
            PlayerSettings.Android.bundleVersionCode = previousVersionCode;
            result?.AddMessage($"Android versionCode restored after unsuccessful build: {current} -> {previousVersionCode}");
            AssetDatabase.SaveAssets();
            changedThisRun = false;
        }
    }
}
