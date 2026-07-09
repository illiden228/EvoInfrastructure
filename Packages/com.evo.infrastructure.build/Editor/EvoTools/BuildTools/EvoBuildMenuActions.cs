using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public static class EvoBuildMenuActions
    {
        public static void ApplyProfile(string globalConfigGuid, string profileGuid, string platformCatalogGuid)
        {
            var globalConfig = LoadAssetByGuid<BuildGlobalConfig>(globalConfigGuid);
            var profile = LoadAssetByGuid<PlatformBuildProfile>(profileGuid);
            var platformCatalog = LoadAssetByGuid<PlatformCatalog>(platformCatalogGuid);

            var report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
            if (report.HasErrors)
            {
                LogMessages("Validate failed", report.Errors, isError: true);
                EditorUtility.DisplayDialog("Evo Build", string.Join("\n", report.Errors), "OK");
                return;
            }

            if (report.RequiresDefineRemovalConfirmation && !ConfirmDefineRemoval(report))
            {
                return;
            }

            EvoBuildApplyResult result;
            using (var progress = new EvoBuildProgressTracker("Evo Build Apply"))
            {
                result = EvoBuildApplier.ApplyPlatform(globalConfig, profile, platformCatalog, switchBuildTarget: true, progress);
            }

            LogResult("Apply", result);
            var message = result.Success
                ? string.Join("\n", result.Messages)
                : string.Join("\n", result.Errors);
            EditorUtility.DisplayDialog("Evo Build", string.IsNullOrWhiteSpace(message) ? "Done." : message, "OK");
        }

        public static void ValidateProfile(string globalConfigGuid, string profileGuid)
        {
            var globalConfig = LoadAssetByGuid<BuildGlobalConfig>(globalConfigGuid);
            var profile = LoadAssetByGuid<PlatformBuildProfile>(profileGuid);
            var report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
            if (report.HasErrors)
            {
                LogMessages("Validation errors", report.Errors, isError: true);
                EditorUtility.DisplayDialog("Evo Build Validation", string.Join("\n", report.Errors), "OK");
                return;
            }

            var message = report.Warnings.Count > 0
                ? string.Join("\n", report.Warnings)
                : "No validation issues found.";
            LogMessages("Validation warnings", report.Warnings, isError: false);
            EditorUtility.DisplayDialog("Evo Build Validation", message, "OK");
        }

        public static void BuildProfile(string globalConfigGuid, string profileGuid, string platformCatalogGuid, bool buildAndRun)
        {
            var globalConfig = LoadAssetByGuid<BuildGlobalConfig>(globalConfigGuid);
            var profile = LoadAssetByGuid<PlatformBuildProfile>(profileGuid);
            var platformCatalog = LoadAssetByGuid<PlatformCatalog>(platformCatalogGuid);
            var report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
            if (report.HasErrors)
            {
                LogMessages("Build validation failed", report.Errors, isError: true);
                EditorUtility.DisplayDialog("Evo Build", string.Join("\n", report.Errors), "OK");
                return;
            }

            if (report.RequiresDefineRemovalConfirmation && !ConfirmDefineRemoval(report))
            {
                return;
            }

            var result = EvoBuildExecutor.Build(globalConfig, profile, platformCatalog, buildAndRun);
            LogResult(buildAndRun ? "Build And Run" : "Build", result);
            var message = result.Success
                ? string.Join("\n", result.Messages)
                : string.Join("\n", result.Errors);
            EditorUtility.DisplayDialog("Evo Build", string.IsNullOrWhiteSpace(message) ? "Done." : message, "OK");
        }

        private static bool ConfirmDefineRemoval(EvoBuildDryRunReport report)
        {
            var message = "The selected profile will remove these scripting define symbols:\n\n" +
                          string.Join("\n", report.WillBeRemovedDefines) +
                          "\n\nContinue?";
            return EditorUtility.DisplayDialog("Confirm Define Removal", message, "Apply", "Cancel");
        }

        private static void LogResult(string action, EvoBuildApplyResult result)
        {
            if (result == null)
            {
                Debug.LogWarning($"[Evo Build] {action}: result is null.");
                return;
            }

            LogMessages($"{action} messages", result.Messages, isError: false);
            LogMessages($"{action} errors", result.Errors, isError: true);
        }

        private static void LogMessages(string title, System.Collections.Generic.IReadOnlyList<string> messages, bool isError)
        {
            if (messages == null || messages.Count == 0)
            {
                if (!isError)
                {
                    Debug.Log($"[Evo Build] {title}: none.");
                }

                return;
            }

            for (var i = 0; i < messages.Count; i++)
            {
                var message = $"[Evo Build] {title}: {messages[i]}";
                if (isError)
                {
                    Debug.LogError(message);
                }
                else
                {
                    Debug.Log(message);
                }
            }
        }

        private static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
