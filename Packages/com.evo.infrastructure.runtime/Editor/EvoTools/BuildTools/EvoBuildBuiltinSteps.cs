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
            result.AddMessage($"Bundle version: {current} -> {next}");
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

    [CreateAssetMenu(fileName = "IncrementAndroidVersionCodeStep", menuName = "EvoTools/Build/Steps/Increment Android Version Code")]
    public sealed class IncrementAndroidVersionCodeStep : EvoBuildStepAsset
    {
        [SerializeField] private bool onlyReleaseBuilds = true;

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (onlyReleaseBuilds && context?.Profile != null && context.Profile.BuildMode != EvoBuildMode.Release)
            {
                result.AddMessage("Android versionCode bump skipped for non-release profile.");
                return true;
            }

            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.Android)
            {
                result.AddMessage("Android versionCode bump skipped for non-Android target.");
                return true;
            }

            var current = PlayerSettings.Android.bundleVersionCode;
            PlayerSettings.Android.bundleVersionCode = current + 1;
            result.AddMessage($"Android versionCode: {current} -> {PlayerSettings.Android.bundleVersionCode}");
            return true;
        }
    }

    [CreateAssetMenu(fileName = "IncrementIosBuildNumberStep", menuName = "EvoTools/Build/Steps/Increment iOS Build Number")]
    public sealed class IncrementIosBuildNumberStep : EvoBuildStepAsset
    {
        [SerializeField] private bool onlyReleaseBuilds = true;

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (onlyReleaseBuilds && context?.Profile != null && context.Profile.BuildMode != EvoBuildMode.Release)
            {
                result.AddMessage("iOS build number bump skipped for non-release profile.");
                return true;
            }

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
            return true;
        }
    }

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

    [CreateAssetMenu(fileName = "ApplyAndroidSigningPasswordsStep", menuName = "EvoTools/Build/Steps/Apply Android Signing Passwords")]
    public sealed class ApplyAndroidSigningPasswordsStep : EvoBuildStepAsset
    {
        [SerializeField] private string keystorePasswordEnvironmentVariable = "EVO_ANDROID_KEYSTORE_PASS";
        [SerializeField] private string keyAliasPasswordEnvironmentVariable = "EVO_ANDROID_KEYALIAS_PASS";
        [SerializeField] private bool allowSerializedFallback;
        [SerializeField] private string serializedKeystorePassword;
        [SerializeField] private string serializedKeyAliasPassword;

        public override void Validate(EvoBuildContext context, EvoBuildDryRunReport report)
        {
            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.Android)
            {
                return;
            }

            if (string.IsNullOrEmpty(ResolveKeystorePassword()))
            {
                report.AddWarning($"{name}: Android keystore password is not configured.");
            }

            if (string.IsNullOrEmpty(ResolveKeyAliasPassword()))
            {
                report.AddWarning($"{name}: Android key alias password is not configured.");
            }
        }

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (context?.Profile == null || context.Profile.BuildTarget != BuildTarget.Android)
            {
                result.AddMessage("Android signing passwords skipped for non-Android target.");
                return true;
            }

            var keystorePassword = ResolveKeystorePassword();
            var keyAliasPassword = ResolveKeyAliasPassword();
            if (string.IsNullOrEmpty(keystorePassword))
            {
                result.AddError("Android keystore password is missing.");
                return false;
            }

            if (string.IsNullOrEmpty(keyAliasPassword))
            {
                result.AddError("Android key alias password is missing.");
                return false;
            }

            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasPass = keyAliasPassword;
            result.AddMessage("Applied Android signing passwords.");
            return true;
        }

        private string ResolveKeystorePassword()
        {
            return ResolveSecret(keystorePasswordEnvironmentVariable, serializedKeystorePassword);
        }

        private string ResolveKeyAliasPassword()
        {
            return ResolveSecret(keyAliasPasswordEnvironmentVariable, serializedKeyAliasPassword);
        }

        private string ResolveSecret(string environmentVariable, string serializedValue)
        {
            if (!string.IsNullOrWhiteSpace(environmentVariable))
            {
                var value = Environment.GetEnvironmentVariable(environmentVariable);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return allowSerializedFallback ? serializedValue : string.Empty;
        }
    }
}
