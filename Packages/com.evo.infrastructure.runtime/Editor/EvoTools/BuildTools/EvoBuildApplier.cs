using System.Collections.Generic;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;
using UnityEditor.Build;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildApplyResult
    {
        private readonly List<string> _messages = new();
        private readonly List<string> _errors = new();

        public IReadOnlyList<string> Messages => _messages;
        public IReadOnlyList<string> Errors => _errors;
        public bool Success => _errors.Count == 0;

        public void AddMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _messages.Add(message);
            }
        }

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _errors.Add(message);
            }
        }
    }

    public static class EvoBuildApplier
    {
        public static EvoBuildApplyResult ApplyPlatform(
            BuildGlobalConfig globalConfig,
            PlatformBuildProfile profile,
            PlatformCatalog platformCatalog,
            bool switchBuildTarget)
        {
            var result = new EvoBuildApplyResult();
            var report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
            if (report.HasErrors)
            {
                for (var i = 0; i < report.Errors.Count; i++)
                {
                    result.AddError(report.Errors[i]);
                }

                return result;
            }

            if (profile == null)
            {
                result.AddError("Build profile is missing.");
                return result;
            }

            var context = new EvoBuildContext(globalConfig, profile, report, string.Empty, buildAndRun: false);
            if (!EvoBuildStepRunner.Execute(context, EvoBuildStepPhase.BeforeApply, result))
            {
                return result;
            }

            if (switchBuildTarget && !SwitchBuildTarget(profile, result))
            {
                return result;
            }

            ApplyDefines(report, result);
            ApplyPlayerSettings(profile, result);
            ApplyPlatformCatalog(profile, platformCatalog, result);
            if (!EvoBuildStepRunner.Execute(context, EvoBuildStepPhase.AfterApply, result))
            {
                return result;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static bool SwitchBuildTarget(PlatformBuildProfile profile, EvoBuildApplyResult result)
        {
            if (EditorUserBuildSettings.activeBuildTarget == profile.BuildTarget)
            {
                result.AddMessage($"Build target already active: {profile.BuildTarget}.");
                return true;
            }

            var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(profile.BuildTargetGroup, profile.BuildTarget);
            if (!switched)
            {
                result.AddError($"Failed to switch build target to {profile.BuildTargetGroup}/{profile.BuildTarget}.");
                return false;
            }

            result.AddMessage($"Switched build target to {profile.BuildTargetGroup}/{profile.BuildTarget}.");
            return true;
        }

        private static void ApplyDefines(EvoBuildDryRunReport report, EvoBuildApplyResult result)
        {
            var defines = string.Join(";", report.TargetDefines);
            PlayerSettings.SetScriptingDefineSymbols(ToNamedBuildTarget(report.BuildTargetGroup), defines);
            result.AddMessage($"Applied {report.TargetDefines.Count} scripting define symbols.");
        }

        private static void ApplyPlayerSettings(PlatformBuildProfile profile, EvoBuildApplyResult result)
        {
            var overrides = profile.PlayerSettings;
            if (overrides == null)
            {
                return;
            }

            if (overrides.OverrideProductName)
            {
                PlayerSettings.productName = overrides.ProductName ?? string.Empty;
                result.AddMessage("Applied PlayerSettings.productName.");
            }

            if (overrides.OverrideBundleVersion)
            {
                PlayerSettings.bundleVersion = overrides.BundleVersion ?? string.Empty;
                result.AddMessage("Applied PlayerSettings.bundleVersion.");
            }

            if (overrides.OverrideApplicationIdentifier)
            {
                PlayerSettings.SetApplicationIdentifier(profile.BuildTargetGroup, overrides.ApplicationIdentifier ?? string.Empty);
                result.AddMessage("Applied PlayerSettings.applicationIdentifier.");
            }

            if (overrides.OverrideOrientation)
            {
                PlayerSettings.defaultInterfaceOrientation = overrides.DefaultOrientation;
                PlayerSettings.allowedAutorotateToPortrait = overrides.AutorotateToPortrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = overrides.AutorotateToPortraitUpsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = overrides.AutorotateToLandscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeRight = overrides.AutorotateToLandscapeRight;
                result.AddMessage("Applied PlayerSettings orientation settings.");
            }
        }

        private static void ApplyPlatformCatalog(
            PlatformBuildProfile profile,
            PlatformCatalog platformCatalog,
            EvoBuildApplyResult result)
        {
            if (platformCatalog == null)
            {
                result.AddMessage("PlatformCatalog was not assigned; currentPlatformId was not changed.");
                return;
            }

            platformCatalog.SetCurrentPlatformId(profile.PlatformId);
            result.AddMessage($"Applied PlatformCatalog.currentPlatformId = '{profile.PlatformId}'.");
        }

        private static NamedBuildTarget ToNamedBuildTarget(BuildTargetGroup group)
        {
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }
    }
}
