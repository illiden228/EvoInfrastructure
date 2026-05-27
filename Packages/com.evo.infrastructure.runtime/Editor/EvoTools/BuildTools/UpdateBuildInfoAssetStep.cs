using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "UpdateBuildInfoAssetStep", menuName = "EvoTools/Build/Steps/Update Build Info Asset")]
    public sealed class UpdateBuildInfoAssetStep : EvoBuildStepAsset
    {
        [SerializeField] private EvoBuildInfoAsset buildInfoAsset;

        public override void Validate(EvoBuildContext context, EvoBuildDryRunReport report)
        {
            if (buildInfoAsset == null)
            {
                report.AddWarning($"{name}: BuildInfo asset is not assigned.");
            }
        }

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (buildInfoAsset == null)
            {
                result.AddMessage("BuildInfo update skipped: asset is not assigned.");
                return true;
            }

            buildInfoAsset.Capture(context?.Profile, context?.OutputPath);
            AssetDatabase.SaveAssets();
            result.AddMessage($"Updated build info asset: {AssetDatabase.GetAssetPath(buildInfoAsset)}");
            return true;
        }
    }
}
