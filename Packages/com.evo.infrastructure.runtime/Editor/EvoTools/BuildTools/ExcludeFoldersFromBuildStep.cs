using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "ExcludeFoldersFromBuildStep", menuName = "EvoTools/Build/Steps/Exclude Folders From Build")]
    public sealed class ExcludeFoldersFromBuildStep : EvoBuildStepAsset, IEvoBuildCleanupStep
    {
        private const string SessionStateKeyPrefix = "EvoTools.Build.ExcludeFoldersFromBuildStep.";
        private const string GlobalTrackedFoldersKey = "EvoTools.Build.ExcludeFoldersFromBuildStep.GlobalTrackedFolders";

        [SerializeField] private List<string> folderPaths = new();
        [SerializeField] private string excludedSuffix = "~";
        [NonSerialized] private HashSet<string> excludedThisRun;

        public override void Validate(EvoBuildContext context, EvoBuildDryRunReport report)
        {
            if (Phase != EvoBuildStepPhase.BeforeBuild)
            {
                report.AddWarning($"{name}: phase should be BeforeBuild.");
            }

            if (folderPaths == null || folderPaths.Count == 0)
            {
                report.AddWarning($"{name}: no folders are configured for exclusion.");
                return;
            }

            for (var i = 0; i < folderPaths.Count; i++)
            {
                var source = NormalizeAssetFolderPath(folderPaths[i]);
                if (string.IsNullOrWhiteSpace(source))
                {
                    report.AddWarning($"{name}: folder path #{i + 1} is empty or outside Assets.");
                    continue;
                }

                var excluded = GetExcludedPath(source);
                var sourceExists = Directory.Exists(ToFullPath(source));
                var excludedExists = Directory.Exists(ToFullPath(excluded));
                if (sourceExists && excludedExists)
                {
                    report.AddError($"{name}: both source and excluded folders exist. Resolve one before build: {source}, {excluded}");
                    continue;
                }

                if (!sourceExists && !excludedExists)
                {
                    report.AddWarning($"{name}: folder not found: {source}");
                }
            }
        }

        public override bool Execute(EvoBuildContext context, EvoBuildApplyResult result)
        {
            if (folderPaths == null || folderPaths.Count == 0)
            {
                result.AddMessage($"{name}: no folders to exclude.");
                return true;
            }

            excludedThisRun = LoadTrackedSources();
            if (excludedThisRun.Count > 0)
            {
                result.AddMessage($"{name}: restoring previously tracked excluded folders before build.");
                RestoreTrackedFolders(result);
                if (excludedThisRun != null && excludedThisRun.Count > 0)
                {
                    return false;
                }
            }

            excludedThisRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var success = true;
            for (var i = 0; i < folderPaths.Count; i++)
            {
                var source = NormalizeAssetFolderPath(folderPaths[i]);
                if (string.IsNullOrWhiteSpace(source))
                {
                    result.AddError($"{name}: folder path #{i + 1} is empty or outside Assets.");
                    success = false;
                    continue;
                }

                if (!ExcludeFolder(source, result))
                {
                    success = false;
                }
            }

            AssetDatabase.Refresh();
            return success;
        }

        public void Cleanup(EvoBuildContext context, EvoBuildApplyResult result)
        {
            excludedThisRun = LoadTrackedSources();
            RestoreTrackedFolders(result);
            AssetDatabase.Refresh();
        }

        private void RestoreTrackedFolders(EvoBuildApplyResult result)
        {
            var restoredOrMissing = new List<string>();
            if (folderPaths != null)
            {
                for (var i = folderPaths.Count - 1; i >= 0; i--)
                {
                    var source = NormalizeAssetFolderPath(folderPaths[i]);
                    if (!string.IsNullOrWhiteSpace(source) &&
                        excludedThisRun != null &&
                        excludedThisRun.Contains(source) &&
                        RestoreFolder(source, result))
                    {
                        UntrackExcludedSource(source);
                        restoredOrMissing.Add(source);
                    }
                }
            }

            if (excludedThisRun != null)
            {
                foreach (var source in excludedThisRun)
                {
                    if (!restoredOrMissing.Contains(source) && RestoreFolder(source, result))
                    {
                        UntrackExcludedSource(source);
                        restoredOrMissing.Add(source);
                    }
                }
            }

            for (var i = 0; i < restoredOrMissing.Count; i++)
            {
                excludedThisRun?.Remove(restoredOrMissing[i]);
            }

            SaveTrackedSources(excludedThisRun);
        }

        private bool ExcludeFolder(string sourcePath, EvoBuildApplyResult result)
        {
            var excludedPath = GetExcludedPath(sourcePath);
            var sourceExists = Directory.Exists(ToFullPath(sourcePath));
            var excludedExists = Directory.Exists(ToFullPath(excludedPath));
            if (sourceExists && excludedExists)
            {
                result.AddError($"{name}: cannot exclude folder because both paths exist: {sourcePath}, {excludedPath}");
                return false;
            }

            if (excludedExists)
            {
                result.AddMessage($"{name}: folder already excluded: {excludedPath}");
                return true;
            }

            if (!sourceExists)
            {
                result.AddError($"{name}: folder not found: {sourcePath}");
                return false;
            }

            if (!MoveFolderWithMeta(sourcePath, excludedPath, result))
            {
                if (Directory.Exists(ToFullPath(excludedPath)) && !Directory.Exists(ToFullPath(sourcePath)))
                {
                    TrackExcludedSource(sourcePath);
                }

                return false;
            }

            TrackExcludedSource(sourcePath);
            result.AddMessage($"{name}: excluded folder {sourcePath} -> {excludedPath}");
            return true;
        }

        private bool RestoreFolder(string sourcePath, EvoBuildApplyResult result)
        {
            var excludedPath = GetExcludedPath(sourcePath);
            if (!Directory.Exists(ToFullPath(excludedPath)))
            {
                return true;
            }

            if (Directory.Exists(ToFullPath(sourcePath)))
            {
                result.AddError($"{name}: cannot restore excluded folder because original path exists: {sourcePath}");
                return false;
            }

            if (!MoveFolderWithMeta(excludedPath, sourcePath, result))
            {
                return false;
            }

            result.AddMessage($"{name}: restored folder {excludedPath} -> {sourcePath}");
            return true;
        }

        private string GetExcludedPath(string sourcePath)
        {
            var suffix = string.IsNullOrWhiteSpace(excludedSuffix) ? "~" : excludedSuffix.Trim();
            return sourcePath.EndsWith(suffix, StringComparison.Ordinal)
                ? sourcePath
                : sourcePath + suffix;
        }

        private void TrackExcludedSource(string sourcePath)
        {
            excludedThisRun ??= LoadTrackedSources();
            excludedThisRun.Add(sourcePath);
            SaveTrackedSources(excludedThisRun);
            RegisterGlobalTrackedFolder(sourcePath, GetExcludedPath(sourcePath));
        }

        private void UntrackExcludedSource(string sourcePath)
        {
            UnregisterGlobalTrackedFolder(sourcePath, GetExcludedPath(sourcePath));
        }

        private HashSet<string> LoadTrackedSources()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var serialized = SessionState.GetString(BuildSessionStateKey(), string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return result;
            }

            var lines = serialized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var source = NormalizeAssetFolderPath(lines[i]);
                if (!string.IsNullOrWhiteSpace(source))
                {
                    result.Add(source);
                }
            }

            return result;
        }

        private void SaveTrackedSources(HashSet<string> sources)
        {
            var key = BuildSessionStateKey();
            if (sources == null || sources.Count == 0)
            {
                SessionState.SetString(key, string.Empty);
                excludedThisRun = null;
                return;
            }

            SessionState.SetString(key, string.Join("\n", sources));
        }

        private string BuildSessionStateKey()
        {
            var path = AssetDatabase.GetAssetPath(this);
            var guid = string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return SessionStateKeyPrefix + (string.IsNullOrWhiteSpace(guid) ? GetInstanceID().ToString() : guid);
        }

        internal static void RestoreAllTrackedFoldersAfterBuild()
        {
            var trackedFolders = LoadGlobalTrackedFolders();
            if (trackedFolders.Count == 0)
            {
                return;
            }

            var remaining = new List<TrackedFolder>();
            var changed = false;
            for (var i = trackedFolders.Count - 1; i >= 0; i--)
            {
                var tracked = trackedFolders[i];
                if (!Directory.Exists(ToFullPath(tracked.ExcludedPath)))
                {
                    changed = true;
                    continue;
                }

                if (Directory.Exists(ToFullPath(tracked.SourcePath)))
                {
                    remaining.Add(tracked);
                    Debug.LogError($"[Evo Build] Cannot restore excluded folder because original path exists: {tracked.SourcePath}");
                    continue;
                }

                if (MoveFolderWithMeta(tracked.ExcludedPath, tracked.SourcePath, out var error))
                {
                    changed = true;
                    Debug.Log($"[Evo Build] Restored excluded folder after build: {tracked.ExcludedPath} -> {tracked.SourcePath}");
                    continue;
                }

                remaining.Add(tracked);
                Debug.LogError($"[Evo Build] Failed to restore excluded folder after build: {error}");
            }

            SaveGlobalTrackedFolders(remaining);
            if (changed)
            {
                AssetDatabase.Refresh();
            }
        }

        private static void RegisterGlobalTrackedFolder(string sourcePath, string excludedPath)
        {
            var trackedFolders = LoadGlobalTrackedFolders();
            for (var i = 0; i < trackedFolders.Count; i++)
            {
                if (string.Equals(trackedFolders[i].SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(trackedFolders[i].ExcludedPath, excludedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            trackedFolders.Add(new TrackedFolder(sourcePath, excludedPath));
            SaveGlobalTrackedFolders(trackedFolders);
        }

        private static void UnregisterGlobalTrackedFolder(string sourcePath, string excludedPath)
        {
            var trackedFolders = LoadGlobalTrackedFolders();
            for (var i = trackedFolders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(trackedFolders[i].SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(trackedFolders[i].ExcludedPath, excludedPath, StringComparison.OrdinalIgnoreCase))
                {
                    trackedFolders.RemoveAt(i);
                }
            }

            SaveGlobalTrackedFolders(trackedFolders);
        }

        private static List<TrackedFolder> LoadGlobalTrackedFolders()
        {
            var result = new List<TrackedFolder>();
            var serialized = SessionState.GetString(GlobalTrackedFoldersKey, string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return result;
            }

            var lines = serialized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split('\t');
                if (parts.Length != 2)
                {
                    continue;
                }

                var source = NormalizeAssetFolderPath(parts[0]);
                var excluded = NormalizeAssetFolderPath(parts[1]);
                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(excluded))
                {
                    result.Add(new TrackedFolder(source, excluded));
                }
            }

            return result;
        }

        private static void SaveGlobalTrackedFolders(List<TrackedFolder> trackedFolders)
        {
            if (trackedFolders == null || trackedFolders.Count == 0)
            {
                SessionState.SetString(GlobalTrackedFoldersKey, string.Empty);
                return;
            }

            var lines = new List<string>(trackedFolders.Count);
            for (var i = 0; i < trackedFolders.Count; i++)
            {
                lines.Add($"{trackedFolders[i].SourcePath}\t{trackedFolders[i].ExcludedPath}");
            }

            SessionState.SetString(GlobalTrackedFoldersKey, string.Join("\n", lines));
        }

        private static string NormalizeAssetFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            path = path.Replace('\\', '/').Trim();
            if (Path.IsPathRooted(path))
            {
                var projectAssetsPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
                if (!path.StartsWith(projectAssetsPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                path = "Assets/" + path.Substring(projectAssetsPath.Length + 1);
            }

            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("/../"))
            {
                return string.Empty;
            }

            return path.TrimEnd('/');
        }

        private bool MoveFolderWithMeta(string sourceAssetPath, string targetAssetPath, EvoBuildApplyResult result)
        {
            if (MoveFolderWithMeta(sourceAssetPath, targetAssetPath, out var error))
            {
                return true;
            }

            result.AddError($"{name}: {error}");
            return false;
        }

        private static bool MoveFolderWithMeta(string sourceAssetPath, string targetAssetPath, out string error)
        {
            error = string.Empty;
            try
            {
                var sourceFullPath = ToFullPath(sourceAssetPath);
                var targetFullPath = ToFullPath(targetAssetPath);
                var sourceMetaPath = sourceFullPath + ".meta";
                var targetMetaPath = targetFullPath + ".meta";

                if (!Directory.Exists(sourceFullPath))
                {
                    error = $"source folder does not exist: {sourceAssetPath}";
                    return false;
                }

                if (Directory.Exists(targetFullPath))
                {
                    error = $"target folder already exists: {targetAssetPath}";
                    return false;
                }

                if (File.Exists(targetMetaPath))
                {
                    error = $"target meta already exists: {targetAssetPath}.meta";
                    return false;
                }

                FileUtil.MoveFileOrDirectory(sourceFullPath, targetFullPath);

                if (File.Exists(sourceMetaPath))
                {
                    FileUtil.MoveFileOrDirectory(sourceMetaPath, targetMetaPath);
                }

                if (!Directory.Exists(targetFullPath))
                {
                    error = $"move did not create target folder: {targetAssetPath}";
                    return false;
                }

                if (Directory.Exists(sourceFullPath))
                {
                    error = $"move left source folder in place: {sourceAssetPath}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"failed to move '{sourceAssetPath}' -> '{targetAssetPath}': {ex.Message}";
                return false;
            }
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return string.IsNullOrWhiteSpace(projectRoot)
                ? assetPath
                : Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private readonly struct TrackedFolder
        {
            public TrackedFolder(string sourcePath, string excludedPath)
            {
                SourcePath = sourcePath;
                ExcludedPath = excludedPath;
            }

            public string SourcePath { get; }
            public string ExcludedPath { get; }
        }
    }

    internal sealed class ExcludeFoldersFromBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            ExcludeFoldersFromBuildStep.RestoreAllTrackedFoldersAfterBuild();
        }
    }
}
