using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "IncrementAndroidVersionCodeStep", menuName = "EvoTools/Build/Steps/Increment Android Version Code")]
    public sealed class IncrementAndroidVersionCodeStep : EvoBuildStepAsset
    {
        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.Android)
            {
                result.AddMessage("Android versionCode bump skipped for non-Android target.");
                return true;
            }

            var current = PlayerSettings.Android.bundleVersionCode;
            PlayerSettings.Android.bundleVersionCode = current + 1;
            result.AddMessage($"Android versionCode: {current} -> {PlayerSettings.Android.bundleVersionCode}");
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
