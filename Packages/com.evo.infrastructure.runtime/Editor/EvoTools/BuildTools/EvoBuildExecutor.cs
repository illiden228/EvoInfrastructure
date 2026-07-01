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
            return Build(globalConfig, profile, platformCatalog, new EvoBuildExecutorOptions
            {
                BuildAndRun = buildAndRun,
                Interactive = true,
                RevealOutput = true
            });
        }

        public static EvoBuildApplyResult Build(
            BuildGlobalConfig globalConfig,
            PlatformBuildProfile profile,
            PlatformCatalog platformCatalog,
            EvoBuildExecutorOptions options)
        {
            options ??= new EvoBuildExecutorOptions();
            var progress = new EvoBuildProgressTracker("Evo Build");
            var result = new EvoBuildApplyResult();
            var outputPath = string.Empty;
            EvoBuildContext cleanupContext = null;
            try
            {
                result = EvoBuildApplier.ApplyPlatform(globalConfig, profile, platformCatalog, switchBuildTarget: true, progress);
                if (!result.Success)
                {
                    return result;
                }

                ApplyCiVersion(options.CiRequest, profile, result);

                EvoBuildDryRunReport report;
                using (progress.Step("Create Dry Run Report", 0.24f, result))
                {
                    report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
                }

                var prepareContext = new EvoBuildContext(globalConfig, profile, report, string.Empty, options.BuildAndRun, options.CiRequest);
                cleanupContext = prepareContext;
                if (!EvoBuildStepRunner.Execute(prepareContext, EvoBuildStepPhase.PrepareBuild, result, progress))
                {
                    return result;
                }

                using (progress.Step("Save Assets Before Build", 0.32f, result))
                {
                    AssetDatabase.SaveAssets();
                }

                outputPath = ResolveOutputPath(globalConfig, profile, options.OutputPathOverride);
                result.SetOutputPath(outputPath);
                if (options.Interactive && !ConfirmBuildWithOptionalVersionBump(globalConfig, profile, options.BuildAndRun, ref outputPath, result))
                {
                    result.SetOutputPath(outputPath);
                    result.AddMessage("Build cancelled.");
                    result.MarkBuildCancelled();
                    return result;
                }
                else if (!options.Interactive)
                {
                    result.AddMessage("Build confirmation skipped for non-interactive build.");
                }

                result.SetOutputPath(outputPath);
                result.AddMessage($"Build output path: {outputPath}");
                var signingSnapshot = AndroidSigningPasswordSnapshot.Capture(profile);
                EvoBuildContext context = null;
                var beforeBuildStarted = false;
                try
                {
                    context = new EvoBuildContext(globalConfig, profile, report, outputPath, options.BuildAndRun, options.CiRequest);
                    beforeBuildStarted = true;
                    if (!EvoBuildStepRunner.Execute(context, EvoBuildStepPhase.BeforeBuild, result, progress))
                    {
                        return result;
                    }

                    outputPath = ResolveOutputPath(globalConfig, profile, options.OutputPathOverride);
                    result.SetOutputPath(outputPath);
                    context = new EvoBuildContext(globalConfig, profile, report, outputPath, options.BuildAndRun, options.CiRequest);
                    cleanupContext = context;
                    using (progress.Step("Ensure Output Directory", 0.4f, result))
                    {
                        EnsureOutputDirectory(outputPath);
                    }

                    var buildOptions = profile.BuildOptions;
                    if (options.BuildAndRun)
                    {
                        buildOptions |= BuildOptions.AutoRunPlayer;
                    }

                    BuildReport buildReport;
                    using (progress.Step("BuildPipeline.BuildPlayer", 0.5f, result))
                    {
                        buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                        {
                            scenes = GetEnabledScenes(),
                            locationPathName = outputPath,
                            target = profile.BuildTarget,
                            targetGroup = profile.BuildTargetGroup,
                            options = buildOptions
                        });
                    }

                    if (buildReport.summary.result != BuildResult.Succeeded)
                    {
                        result.AddError($"Build failed: {buildReport.summary.result}. Errors: {buildReport.summary.totalErrors}.");
                        return result;
                    }

                    result.AddMessage($"Build succeeded: {outputPath}");
                    result.AddMessage($"Build size: {buildReport.summary.totalSize} bytes.");
                    result.MarkBuildSucceeded();
                    EvoBuildStepRunner.Cleanup(context, result, progress);
                    beforeBuildStarted = false;
                    EvoBuildStepRunner.Execute(context, EvoBuildStepPhase.AfterBuild, result, progress);
                    if (options.RevealOutput)
                    {
                        RevealBuildOutput(outputPath, result);
                    }

                    return result;
                }
                finally
                {
                    if (beforeBuildStarted)
                    {
                        EvoBuildStepRunner.Cleanup(context, result, progress);
                        cleanupContext = null;
                    }

                    signingSnapshot.Restore(result);
                }
            }
            finally
            {
                if (!result.BuildSucceeded && cleanupContext != null)
                {
                    EvoBuildStepRunner.Cleanup(cleanupContext, result, progress);
                }

                WriteBuildReport(outputPath, profile, options.BuildAndRun, result, progress);
                progress.Dispose();
            }
        }

        public static string BuildConfirmationMessage(PlatformBuildProfile profile, string outputPath, bool buildAndRun)
        {
            var androidVersionCode = profile.BuildTarget == BuildTarget.Android
                ? $"\nAndroid versionCode: {PlayerSettings.Android.bundleVersionCode}"
                : string.Empty;
            var androidPackageFormat = profile.BuildTarget == BuildTarget.Android
                ? $"\nAndroid package: {(EditorUserBuildSettings.buildAppBundle ? "AAB" : "APK")}"
                : string.Empty;
            var iosBuildNumber = profile.BuildTarget == BuildTarget.iOS
                ? $"\niOS buildNumber: {PlayerSettings.iOS.buildNumber}"
                : string.Empty;
            var debugOptions = BuildHasDebugOptions(profile)
                ? "\nDebuggable options: YES"
                : "\nDebuggable options: no";

            return $"{(buildAndRun ? "Build and run" : "Build")} profile '{profile.DisplayName}'?" +
                   $"\n\nVersion: {PlayerSettings.bundleVersion}" +
                   androidVersionCode +
                   androidPackageFormat +
                   iosBuildNumber +
                   $"\nBuild mode: {profile.BuildMode}" +
                   $"\nBuild options: {profile.BuildOptions}" +
                   debugOptions +
                   $"\nPlatform: {profile.PlatformId}" +
                   $"\nTarget: {profile.BuildTargetGroup}/{profile.BuildTarget}" +
                   $"\n\nOutput:\n{outputPath}";
        }

        private static bool ConfirmBuildWithOptionalVersionBump(
            BuildGlobalConfig globalConfig,
            PlatformBuildProfile profile,
            bool buildAndRun,
            ref string outputPath,
            EvoBuildApplyResult result)
        {
            while (true)
            {
                var selected = EditorUtility.DisplayDialogComplex(
                    "Evo Build",
                    BuildConfirmationMessage(profile, outputPath, buildAndRun),
                    buildAndRun ? "Build And Run" : "Build",
                    "Bump + Build",
                    "Cancel");

                if (selected == 0)
                {
                    return true;
                }

                if (selected == 2)
                {
                    return false;
                }

                BumpPatchAndAndroidVersionCode(profile, result);
                outputPath = ResolveOutputPath(globalConfig, profile);
            }
        }

        private static void BumpPatchAndAndroidVersionCode(PlatformBuildProfile profile, EvoBuildApplyResult result)
        {
            var currentVersion = PlayerSettings.bundleVersion;
            var nextVersion = IncrementBundleVersionStep.ChangeVersion(currentVersion, EvoVersionBumpMode.Patch, 1);
            PlayerSettings.bundleVersion = nextVersion;
            if (profile != null && profile.SyncBundleVersionOverride(nextVersion))
            {
                EditorUtility.SetDirty(profile);
            }

            result.AddMessage($"Bundle version: {currentVersion} -> {nextVersion}");
            if (profile != null && profile.BuildTarget == BuildTarget.Android)
            {
                var currentCode = PlayerSettings.Android.bundleVersionCode;
                PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, currentCode + 1);
                result.AddMessage($"Android versionCode: {currentCode} -> {PlayerSettings.Android.bundleVersionCode}");
            }

            AssetDatabase.SaveAssets();
        }

        private static bool BuildHasDebugOptions(PlatformBuildProfile profile)
        {
            if (profile == null)
            {
                return false;
            }

            var options = profile.BuildOptions;
            return (options & BuildOptions.Development) != 0 ||
                   (options & BuildOptions.AllowDebugging) != 0 ||
                   (options & BuildOptions.ConnectWithProfiler) != 0 ||
                   (options & BuildOptions.EnableDeepProfilingSupport) != 0;
        }

        public static string ResolveOutputPath(BuildGlobalConfig globalConfig, PlatformBuildProfile profile)
        {
            return ResolveOutputPath(globalConfig, profile, outputPathOverride: null);
        }

        public static string ResolveOutputPath(BuildGlobalConfig globalConfig, PlatformBuildProfile profile, string outputPathOverride)
        {
            if (!string.IsNullOrWhiteSpace(outputPathOverride))
            {
                var overriddenPath = outputPathOverride.Replace('\\', '/');
                if (profile != null && profile.BuildTarget == BuildTarget.Android)
                {
                    overriddenPath = EnsureAndroidPackageExtension(overriddenPath, ResolveAndroidBuildAppBundle(profile));
                }

                return overriddenPath;
            }

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
                .Replace("{version}", SanitizePathPart(PlayerSettings.bundleVersion))
                .Replace("{androidVersionCode}", SanitizePathPart(PlayerSettings.Android.bundleVersionCode.ToString()));

            if (profile.BuildTarget == BuildTarget.Android)
            {
                result = EnsureAndroidPackageExtension(result, ResolveAndroidBuildAppBundle(profile));
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

        private static void ApplyCiVersion(EvoBuildCiRequest request, PlatformBuildProfile profile, EvoBuildApplyResult result)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ParsedTag.Version))
            {
                return;
            }

            PlayerSettings.bundleVersion = request.ParsedTag.Version;
            result.AddMessage($"Applied CI bundleVersion: {request.ParsedTag.Version}.");
            if (profile == null)
            {
                return;
            }

            if (profile.BuildTarget == BuildTarget.Android)
            {
                PlayerSettings.Android.bundleVersionCode = request.ParsedTag.BuildNumber;
                result.AddMessage($"Applied CI Android versionCode: {request.ParsedTag.BuildNumber}.");
            }
            else if (profile.BuildTarget == BuildTarget.iOS)
            {
                PlayerSettings.iOS.buildNumber = request.ParsedTag.BuildNumber.ToString();
                result.AddMessage($"Applied CI iOS buildNumber: {request.ParsedTag.BuildNumber}.");
            }
            else
            {
                result.AddMessage($"CI buildNumber {request.ParsedTag.BuildNumber} parsed but no platform-specific build number was applied for {profile.BuildTarget}.");
            }
        }

        private static bool ResolveAndroidBuildAppBundle(PlatformBuildProfile profile)
        {
            var androidBuild = profile?.AndroidBuild;
            if (androidBuild != null && androidBuild.OverrideBuildAppBundle)
            {
                return androidBuild.BuildAppBundle;
            }

            return EditorUserBuildSettings.buildAppBundle;
        }

        private static string EnsureAndroidPackageExtension(string path, bool buildAppBundle)
        {
            var expectedExtension = buildAppBundle ? ".aab" : ".apk";
            if (string.IsNullOrWhiteSpace(path))
            {
                return $"Build{expectedExtension}";
            }

            var extension = Path.GetExtension(path);
            if (string.Equals(extension, expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            if (string.Equals(extension, ".apk", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".aab", StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(0, path.Length - extension.Length) + expectedExtension;
            }

            return path + expectedExtension;
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

        private static void RevealBuildOutput(string outputPath, EvoBuildApplyResult result)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            var normalizedPath = outputPath.Replace('\\', '/');
            var revealPath = File.Exists(normalizedPath) || Directory.Exists(normalizedPath)
                ? normalizedPath
                : Path.GetDirectoryName(normalizedPath);

            if (string.IsNullOrWhiteSpace(revealPath) || !Directory.Exists(revealPath) && !File.Exists(revealPath))
            {
                result.AddMessage($"Build output reveal skipped: path was not found ({outputPath}).");
                return;
            }

            EditorUtility.RevealInFinder(revealPath);
            result.AddMessage($"Revealed build output: {revealPath.Replace('\\', '/')}");
        }

        private static void WriteBuildReport(
            string outputPath,
            PlatformBuildProfile profile,
            bool buildAndRun,
            EvoBuildApplyResult result,
            EvoBuildProgressTracker progress)
        {
            if (string.IsNullOrWhiteSpace(outputPath) || progress == null)
            {
                return;
            }

            try
            {
                var normalizedPath = outputPath.Replace('\\', '/');
                var directory = Path.HasExtension(normalizedPath)
                    ? Path.GetDirectoryName(normalizedPath)
                    : normalizedPath;
                if (string.IsNullOrWhiteSpace(directory))
                {
                    return;
                }

                Directory.CreateDirectory(directory);
                var fileName = $"evo-build-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
                var reportPath = Path.Combine(directory, fileName).Replace('\\', '/');
                File.WriteAllText(reportPath, progress.CreateReport(profile, outputPath, buildAndRun, result));
                result?.AddMessage($"Build report saved: {reportPath}");
            }
            catch (Exception ex)
            {
                result?.AddError($"Failed to write build report: {ex.Message}");
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

        private readonly struct AndroidSigningPasswordSnapshot
        {
            private readonly bool _enabled;
            private readonly string _keystorePass;
            private readonly string _keyaliasPass;

            private AndroidSigningPasswordSnapshot(bool enabled, string keystorePass, string keyaliasPass)
            {
                _enabled = enabled;
                _keystorePass = keystorePass;
                _keyaliasPass = keyaliasPass;
            }

            public static AndroidSigningPasswordSnapshot Capture(PlatformBuildProfile profile)
            {
                if (profile == null || profile.BuildTarget != BuildTarget.Android)
                {
                    return new AndroidSigningPasswordSnapshot(false, string.Empty, string.Empty);
                }

                return new AndroidSigningPasswordSnapshot(
                    true,
                    PlayerSettings.Android.keystorePass,
                    PlayerSettings.Android.keyaliasPass);
            }

            public void Restore(EvoBuildApplyResult result)
            {
                if (!_enabled)
                {
                    return;
                }

                PlayerSettings.Android.keystorePass = _keystorePass;
                PlayerSettings.Android.keyaliasPass = _keyaliasPass;
                result.AddMessage("Restored Android signing passwords after build.");
            }
        }
    }
}
