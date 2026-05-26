using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildDryRunReport
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();
        private readonly List<string> _currentDefines = new();
        private readonly List<string> _targetDefines = new();
        private readonly List<string> _willBeAddedDefines = new();
        private readonly List<string> _willBeRemovedDefines = new();
        private readonly List<PlayerSettingsChange> _playerSettingsChanges = new();

        public string ProfileId { get; internal set; }
        public string PlatformId { get; internal set; }
        public BuildTarget BuildTarget { get; internal set; }
        public BuildTargetGroup BuildTargetGroup { get; internal set; }
        public EvoDefineCleanupPolicy DefineCleanupPolicy { get; internal set; }
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public IReadOnlyList<string> CurrentDefines => _currentDefines;
        public IReadOnlyList<string> TargetDefines => _targetDefines;
        public IReadOnlyList<string> WillBeAddedDefines => _willBeAddedDefines;
        public IReadOnlyList<string> WillBeRemovedDefines => _willBeRemovedDefines;
        public IReadOnlyList<PlayerSettingsChange> PlayerSettingsChanges => _playerSettingsChanges;
        public bool HasErrors => _errors.Count > 0;
        public bool RequiresDefineRemovalConfirmation => _willBeRemovedDefines.Count > 0 && DefineCleanupPolicy == EvoDefineCleanupPolicy.WarnBeforeRemove;

        public void AddError(string message) => AddUnique(_errors, message);
        public void AddWarning(string message) => AddUnique(_warnings, message);
        internal void AddCurrentDefine(string define) => AddUnique(_currentDefines, define);
        internal void AddTargetDefine(string define) => AddUnique(_targetDefines, define);
        internal void AddAddedDefine(string define) => AddUnique(_willBeAddedDefines, define);
        internal void AddRemovedDefine(string define) => AddUnique(_willBeRemovedDefines, define);
        internal void AddPlayerSettingsChange(PlayerSettingsChange change) => _playerSettingsChanges.Add(change);

        private static void AddUnique(List<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            value = value.Trim();
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }
    }

    public readonly struct PlayerSettingsChange
    {
        public PlayerSettingsChange(string fieldName, string currentValue, string targetValue, string source)
        {
            FieldName = fieldName ?? string.Empty;
            CurrentValue = currentValue ?? string.Empty;
            TargetValue = targetValue ?? string.Empty;
            Source = source ?? string.Empty;
        }

        public string FieldName { get; }
        public string CurrentValue { get; }
        public string TargetValue { get; }
        public string Source { get; }
    }

    public static class EvoBuildPlanner
    {
        public static EvoBuildDryRunReport CreateDryRun(BuildGlobalConfig globalConfig, PlatformBuildProfile profile)
        {
            var report = new EvoBuildDryRunReport();
            if (profile == null)
            {
                report.AddError("Build profile is missing.");
                return report;
            }

            report.ProfileId = profile.ProfileId;
            report.PlatformId = profile.PlatformId;
            report.BuildTarget = profile.BuildTarget;
            report.BuildTargetGroup = profile.BuildTargetGroup;
            report.DefineCleanupPolicy = globalConfig != null
                ? globalConfig.DefineCleanupPolicy
                : EvoDefineCleanupPolicy.WarnBeforeRemove;

            ValidateProfile(report, profile);

            var currentDefines = SplitDefines(PlayerSettings.GetScriptingDefineSymbols(ToNamedBuildTarget(profile.BuildTargetGroup)));
            var targetDefines = BuildTargetDefines(globalConfig, profile);
            var sortedCurrentDefines = new List<string>(currentDefines);
            sortedCurrentDefines.Sort(StringComparer.Ordinal);

            foreach (var define in sortedCurrentDefines)
            {
                report.AddCurrentDefine(define);
                if (!targetDefines.Contains(define))
                {
                    if (report.DefineCleanupPolicy == EvoDefineCleanupPolicy.WarnAndPreserve)
                    {
                        targetDefines.Add(define);
                    }
                    else
                    {
                        report.AddRemovedDefine(define);
                    }
                }
            }

            var sortedTargetDefines = new List<string>(targetDefines);
            sortedTargetDefines.Sort(StringComparer.Ordinal);
            foreach (var define in sortedTargetDefines)
            {
                report.AddTargetDefine(define);
                if (!currentDefines.Contains(define))
                {
                    report.AddAddedDefine(define);
                }
            }

            if (report.DefineCleanupPolicy == EvoDefineCleanupPolicy.FailIfUnknown && report.WillBeRemovedDefines.Count > 0)
            {
                report.AddError("Current defines contain values not listed in selected build profile.");
            }

            AppendPlayerSettingsChanges(report, profile);
            var context = new EvoBuildContext(globalConfig, profile, report, string.Empty, buildAndRun: false);
            EvoBuildStepRunner.Validate(context, report);
            return report;
        }

        private static HashSet<string> BuildTargetDefines(BuildGlobalConfig globalConfig, PlatformBuildProfile profile)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            AddDefines(result, globalConfig?.CommonDefines);
            AddDefines(result, profile.Defines);

            if (profile.BuildMode == EvoBuildMode.Debug)
            {
                AddDefines(result, globalConfig?.DebugDefines);
            }

            return result;
        }

        private static void AppendPlayerSettingsChanges(EvoBuildDryRunReport report, PlatformBuildProfile profile)
        {
            var overrides = profile.PlayerSettings;
            if (overrides == null)
            {
                return;
            }

            if (overrides.OverrideProductName)
            {
                AddChangeIfDifferent(report, "PlayerSettings.productName", PlayerSettings.productName, overrides.ProductName, "Profile");
            }

            if (overrides.OverrideBundleVersion)
            {
                AddChangeIfDifferent(report, "PlayerSettings.bundleVersion", PlayerSettings.bundleVersion, overrides.BundleVersion, "Profile");
            }

            if (overrides.OverrideApplicationIdentifier)
            {
                var current = PlayerSettings.GetApplicationIdentifier(profile.BuildTargetGroup);
                AddChangeIfDifferent(report, "PlayerSettings.applicationIdentifier", current, overrides.ApplicationIdentifier, "Profile");
            }

            if (overrides.OverrideOrientation)
            {
                AddChangeIfDifferent(report, "PlayerSettings.defaultInterfaceOrientation", PlayerSettings.defaultInterfaceOrientation.ToString(), overrides.DefaultOrientation.ToString(), "Profile");
                AddChangeIfDifferent(report, "PlayerSettings.allowedAutorotateToPortrait", PlayerSettings.allowedAutorotateToPortrait.ToString(), overrides.AutorotateToPortrait.ToString(), "Profile");
                AddChangeIfDifferent(report, "PlayerSettings.allowedAutorotateToPortraitUpsideDown", PlayerSettings.allowedAutorotateToPortraitUpsideDown.ToString(), overrides.AutorotateToPortraitUpsideDown.ToString(), "Profile");
                AddChangeIfDifferent(report, "PlayerSettings.allowedAutorotateToLandscapeLeft", PlayerSettings.allowedAutorotateToLandscapeLeft.ToString(), overrides.AutorotateToLandscapeLeft.ToString(), "Profile");
                AddChangeIfDifferent(report, "PlayerSettings.allowedAutorotateToLandscapeRight", PlayerSettings.allowedAutorotateToLandscapeRight.ToString(), overrides.AutorotateToLandscapeRight.ToString(), "Profile");
            }
        }

        private static void ValidateProfile(EvoBuildDryRunReport report, PlatformBuildProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                report.AddError("Build profile id is missing.");
            }

            if (string.IsNullOrWhiteSpace(profile.PlatformId))
            {
                report.AddWarning("Platform id is empty. Runtime PlatformCatalog.currentPlatformId will be empty after apply.");
            }

            if (profile.BuildTargetGroup == BuildTargetGroup.Unknown)
            {
                report.AddError($"Build target group could not be resolved for {profile.BuildTarget}.");
            }
        }

        private static void AddChangeIfDifferent(EvoBuildDryRunReport report, string fieldName, string currentValue, string targetValue, string source)
        {
            currentValue ??= string.Empty;
            targetValue ??= string.Empty;
            if (!string.Equals(currentValue, targetValue, StringComparison.Ordinal))
            {
                report.AddPlayerSettingsChange(new PlayerSettingsChange(fieldName, currentValue, targetValue, source));
            }
        }

        private static void AddDefines(HashSet<string> target, IReadOnlyList<string> defines)
        {
            if (defines == null)
            {
                return;
            }

            for (var i = 0; i < defines.Count; i++)
            {
                var define = defines[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(define))
                {
                    target.Add(define);
                }
            }
        }

        private static HashSet<string> SplitDefines(string defineSymbols)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(defineSymbols))
            {
                return result;
            }

            var parts = defineSymbols.Split(';');
            for (var i = 0; i < parts.Length; i++)
            {
                var define = parts[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(define))
                {
                    result.Add(define);
                }
            }

            return result;
        }

        private static NamedBuildTarget ToNamedBuildTarget(BuildTargetGroup group)
        {
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }
    }
}
