using System;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [Serializable]
    public sealed class AndroidSigningSettings
    {
        [Tooltip("When enabled, ApplyAndroidSigningPasswordsStep reads passwords from this build profile. When disabled, the step uses its own legacy settings.")]
        [SerializeField] private bool overridePasswords;
        [SerializeField] private string keystorePasswordEnvironmentVariable = "EVO_ANDROID_KEYSTORE_PASS";
        [SerializeField] private string keyAliasPasswordEnvironmentVariable = "EVO_ANDROID_KEYALIAS_PASS";
        [SerializeField] private bool allowSerializedFallback;
        [SerializeField] private string serializedKeystorePassword;
        [SerializeField] private string serializedKeyAliasPassword;

        public bool OverridePasswords => overridePasswords;
        public string KeystorePasswordEnvironmentVariable => keystorePasswordEnvironmentVariable;
        public string KeyAliasPasswordEnvironmentVariable => keyAliasPasswordEnvironmentVariable;
        public bool AllowSerializedFallback => allowSerializedFallback;
        public string SerializedKeystorePassword => serializedKeystorePassword;
        public string SerializedKeyAliasPassword => serializedKeyAliasPassword;

        public string ResolveKeystorePassword()
        {
            return ResolveSecret(keystorePasswordEnvironmentVariable, serializedKeystorePassword);
        }

        public string ResolveKeyAliasPassword()
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
