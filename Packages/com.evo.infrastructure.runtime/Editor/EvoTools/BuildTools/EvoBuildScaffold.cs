using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildScaffoldResult
    {
        private readonly List<string> _messages = new();

        public BuildGlobalConfig GlobalConfig { get; internal set; }
        public IReadOnlyList<string> Messages => _messages;

        internal void AddMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _messages.Add(message);
            }
        }
    }

    public static class EvoBuildScaffold
    {
        public const string RootFolder = "Assets/_Project/Configs/Build";
        private const string ProfilesFolder = RootFolder + "/Profiles";
        private const string ReportsFolder = RootFolder + "/Reports";
        private const string GlobalConfigPath = RootFolder + "/BuildGlobalConfig.asset";

        public static EvoBuildScaffoldResult EnsureDefaultAssets()
        {
            var result = new EvoBuildScaffoldResult();
            EnsureFolder(RootFolder);
            EnsureFolder(ProfilesFolder);
            EnsureFolder(ReportsFolder);
            result.AddMessage("Build assets are created under Assets/_Project/Configs/Build. If the project .gitignore ignores directories named Build, add an exception for this folder.");

            var globalConfig = AssetDatabase.LoadAssetAtPath<BuildGlobalConfig>(GlobalConfigPath);
            if (globalConfig == null)
            {
                globalConfig = ScriptableObject.CreateInstance<BuildGlobalConfig>();
                AssetDatabase.CreateAsset(globalConfig, GlobalConfigPath);
                result.AddMessage($"Created {GlobalConfigPath}");
            }
            else
            {
                result.AddMessage($"Found {GlobalConfigPath}");
            }

            result.GlobalConfig = globalConfig;
            var profiles = new[]
            {
                EnsureProfile("Android.asset", "android", "android", "Android", BuildTarget.Android, BuildTargetGroup.Android, result),
                EnsureProfile("Web.asset", "web", "web", "Web", BuildTarget.WebGL, BuildTargetGroup.WebGL, result),
                EnsureProfile("Standalone.asset", "standalone", "standalone", "Standalone", BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, result)
            };

            AddProfilesToGlobalConfig(globalConfig, profiles, result);
            EditorUtility.SetDirty(globalConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static PlatformBuildProfile EnsureProfile(
            string fileName,
            string profileId,
            string platformId,
            string displayName,
            BuildTarget buildTarget,
            BuildTargetGroup buildTargetGroup,
            EvoBuildScaffoldResult result)
        {
            var path = $"{ProfilesFolder}/{fileName}";
            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlatformBuildProfile>();
                profile.name = fileName.Replace(".asset", string.Empty);
                profile.SetDefaults(profileId, platformId, displayName, buildTarget, buildTargetGroup);
                AssetDatabase.CreateAsset(profile, path);
                EditorUtility.SetDirty(profile);
                result.AddMessage($"Created {path}");
            }
            else
            {
                result.AddMessage($"Found {path}");
            }

            return profile;
        }

        private static void AddProfilesToGlobalConfig(
            BuildGlobalConfig globalConfig,
            IReadOnlyList<PlatformBuildProfile> profiles,
            EvoBuildScaffoldResult result)
        {
            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (globalConfig.AddProfileIfMissing(profile))
                {
                    result.AddMessage($"Added profile '{profile.name}' to BuildGlobalConfig.");
                }
            }
        }

        private static void EnsureFolder(string folder)
        {
            var normalized = folder.Replace('\\', '/').Trim('/');
            var parts = normalized.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
