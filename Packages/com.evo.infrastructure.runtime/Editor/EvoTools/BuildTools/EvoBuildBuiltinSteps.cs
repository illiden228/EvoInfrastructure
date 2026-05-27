using System;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public enum EvoVersionBumpMode
    {
        None = 0,
        Patch = 1,
        Minor = 2,
        Major = 3
    }

    [CreateAssetMenu(fileName = "IncrementBundleVersionStep", menuName = "EvoTools/Build/Steps/Increment Bundle Version")]
    public sealed class IncrementBundleVersionStep : EvoBuildStepAsset
    {
        [SerializeField] private bool onlyReleaseBuilds = true;
        [SerializeField] private EvoVersionBumpMode bumpMode = EvoVersionBumpMode.Patch;

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (onlyReleaseBuilds && context?.Profile != null && context.Profile.BuildMode != EvoBuildMode.Release)
            {
                result.AddMessage("Bundle version bump skipped for non-release profile.");
                return true;
            }

            if (bumpMode == EvoVersionBumpMode.None)
            {
                result.AddMessage("Bundle version bump skipped.");
                return true;
            }

            var current = PlayerSettings.bundleVersion;
            var next = BumpVersion(current, bumpMode);
            PlayerSettings.bundleVersion = next;
            if (context?.Profile != null && context.Profile.SyncBundleVersionOverride(next))
            {
                EditorUtility.SetDirty(context.Profile);
                result.AddMessage($"Profile bundle version override updated: {next}");
            }

            result.AddMessage($"Bundle version: {current} -> {next}");
            AssetDatabase.SaveAssets();
            return true;
        }

        private static string BumpVersion(string version, EvoVersionBumpMode mode)
        {
            var parts = (version ?? "0.0.0").Split('.');
            var major = ParsePart(parts, 0);
            var minor = ParsePart(parts, 1);
            var patch = ParsePart(parts, 2);

            switch (mode)
            {
                case EvoVersionBumpMode.Major:
                    major++;
                    minor = 0;
                    patch = 0;
                    break;
                case EvoVersionBumpMode.Minor:
                    minor++;
                    patch = 0;
                    break;
                case EvoVersionBumpMode.Patch:
                    patch++;
                    break;
            }

            return $"{major}.{minor}.{patch}";
        }

        private static int ParsePart(string[] parts, int index)
        {
            if (parts == null || index < 0 || index >= parts.Length)
            {
                return 0;
            }

            return int.TryParse(parts[index], out var value) ? Math.Max(0, value) : 0;
        }
    }
}
