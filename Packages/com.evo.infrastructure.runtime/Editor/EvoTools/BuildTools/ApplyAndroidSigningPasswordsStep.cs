using System;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
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

            if (string.IsNullOrEmpty(ResolveKeystorePassword(context.Profile)))
            {
                report.AddWarning($"{name}: Android keystore password is not configured.");
            }

            if (string.IsNullOrEmpty(ResolveKeyAliasPassword(context.Profile)))
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

            var keystorePassword = ResolveKeystorePassword(context.Profile);
            var keyAliasPassword = ResolveKeyAliasPassword(context.Profile);
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

        private string ResolveKeystorePassword(PlatformBuildProfile profile)
        {
            if (profile?.AndroidSigning != null && profile.AndroidSigning.OverridePasswords)
            {
                return profile.AndroidSigning.ResolveKeystorePassword();
            }

            return ResolveSecret(keystorePasswordEnvironmentVariable, serializedKeystorePassword);
        }

        private string ResolveKeyAliasPassword(PlatformBuildProfile profile)
        {
            if (profile?.AndroidSigning != null && profile.AndroidSigning.OverridePasswords)
            {
                return profile.AndroidSigning.ResolveKeyAliasPassword();
            }

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
