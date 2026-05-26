using System;
using System.Collections.Generic;
using System.IO;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public static class EvoBuildExecutor
    {
        public static EvoBuildApplyResult Build(
            BuildGlobalConfig globalConfig,
            PlatformBuildProfile profile,
            PlatformCatalog platformCatalog,
            bool buildAndRun)
        {
            var result = EvoBuildApplier.ApplyPlatform(globalConfig, profile, platformCatalog, switchBuildTarget: true);
            if (!result.Success)
            {
                return result;
            }

            var report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
            var prepareContext = new EvoBuildContext(globalConfig, profile, report, string.Empty, buildAndRun);
            if (!EvoBuildStepRunner.Execute(prepareContext, EvoBuildStepPhase.PrepareBuild, result))
            {
                return result;
            }

            AssetDatabase.SaveAssets();
            var outputPath = ResolveOutputPath(globalConfig, profile);
            if (!EditorUtility.DisplayDialog(
                    "Evo Build",
                    BuildConfirmationMessage(profile, outputPath, buildAndRun),
                    buildAndRun ? "Build And Run" : "Build",
                    "Cancel"))
            {
                result.AddMessage("Build cancelled.");
                return result;
            }

            var context = new EvoBuildContext(globalConfig, profile, report, outputPath, buildAndRun);
            if (!EvoBuildStepRunner.Execute(context, EvoBuildStepPhase.BeforeBuild, result))
            {
                return result;
            }

            outputPath = ResolveOutputPath(globalConfig, profile);
            context = new EvoBuildContext(globalConfig, profile, report, outputPath, buildAndRun);
            EnsureOutputDirectory(outputPath);
            var options = profile.BuildOptions;
            if (buildAndRun)
            {
                options |= BuildOptions.AutoRunPlayer;
            }

            var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = profile.BuildTarget,
                targetGroup = profile.BuildTargetGroup,
                options = options
            });

            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                result.AddError($"Build failed: {buildReport.summary.result}. Errors: {buildReport.summary.totalErrors}.");
                return result;
            }

            result.AddMessage($"Build succeeded: {outputPath}");
            result.AddMessage($"Build size: {buildReport.summary.totalSize} bytes.");
            EvoBuildStepRunner.Execute(context, EvoBuildStepPhase.AfterBuild, result);
            return result;
        }

        public static string BuildConfirmationMessage(PlatformBuildProfile profile, string outputPath, bool buildAndRun)
        {
            var androidVersionCode = profile.BuildTarget == BuildTarget.Android
                ? $"\nAndroid versionCode: {PlayerSettings.Android.bundleVersionCode}"
                : string.Empty;
            var iosBuildNumber = profile.BuildTarget == BuildTarget.iOS
                ? $"\niOS buildNumber: {PlayerSettings.iOS.buildNumber}"
                : string.Empty;

            return $"{(buildAndRun ? "Build and run" : "Build")} profile '{profile.DisplayName}'?" +
                   $"\n\nVersion: {PlayerSettings.bundleVersion}" +
                   androidVersionCode +
                   iosBuildNumber +
                   $"\nPlatform: {profile.PlatformId}" +
                   $"\nTarget: {profile.BuildTargetGroup}/{profile.BuildTarget}" +
                   $"\n\nOutput:\n{outputPath}";
        }

        public static string ResolveOutputPath(BuildGlobalConfig globalConfig, PlatformBuildProfile profile)
        {
            var template = !string.IsNullOrWhiteSpace(profile.OutputPathTemplate)
                ? profile.OutputPathTemplate
                : globalConfig?.OutputPathPattern;
            if (string.IsNullOrWhiteSpace(template))
            {
                template = "Builds/{profileId}/{productName}_{version}";
            }

            var result = template
                .Replace("{profileId}", SanitizePathPart(profile.ProfileId))
                .Replace("{platformId}", SanitizePathPart(profile.PlatformId))
                .Replace("{productName}", SanitizePathPart(PlayerSettings.productName))
                .Replace("{version}", SanitizePathPart(PlayerSettings.bundleVersion));

            if (profile.BuildTarget == BuildTarget.Android && !Path.HasExtension(result))
            {
                result += EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
            }

            if (profile.BuildTarget == BuildTarget.StandaloneWindows || profile.BuildTarget == BuildTarget.StandaloneWindows64)
            {
                if (!Path.HasExtension(result))
                {
                    result = Path.Combine(result, $"{SanitizePathPart(PlayerSettings.productName)}.exe");
                }
            }

            return result.Replace('\\', '/');
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            var editorScenes = EditorBuildSettings.scenes;
            for (var i = 0; i < editorScenes.Length; i++)
            {
                var scene = editorScenes[i];
                if (scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }

        private static void EnsureOutputDirectory(string outputPath)
        {
            var directory = Path.HasExtension(outputPath)
                ? Path.GetDirectoryName(outputPath)
                : outputPath;
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
