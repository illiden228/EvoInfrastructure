using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "ExcludeFoldersFromBuildStep", menuName = "EvoTools/Build/Steps/Exclude Folders From Build")]
    public sealed class ExcludeFoldersFromBuildStep : EvoBuildStepAsset, IEvoBuildCleanupStep
    {
        [SerializeField] private List<string> folderPaths = new();
        [SerializeField] private string excludedSuffix = "~";

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
                if (!Directory.Exists(ToFullPath(source)) && !Directory.Exists(ToFullPath(excluded)))
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
            if (folderPaths == null || folderPaths.Count == 0)
            {
                return;
            }

            for (var i = folderPaths.Count - 1; i >= 0; i--)
            {
                var source = NormalizeAssetFolderPath(folderPaths[i]);
                if (!string.IsNullOrWhiteSpace(source))
                {
                    RestoreFolder(source, result);
                }
            }

            AssetDatabase.Refresh();
        }

        private bool ExcludeFolder(string sourcePath, EvoBuildApplyResult result)
        {
            var excludedPath = GetExcludedPath(sourcePath);
            if (Directory.Exists(ToFullPath(excludedPath)))
            {
                result.AddMessage($"{name}: folder already excluded: {excludedPath}");
                return true;
            }

            if (!Directory.Exists(ToFullPath(sourcePath)))
            {
                result.AddError($"{name}: folder not found: {sourcePath}");
                return false;
            }

            if (Directory.Exists(ToFullPath(excludedPath)))
            {
                result.AddError($"{name}: excluded path already exists on disk: {excludedPath}");
                return false;
            }

            if (!MoveFolderWithMeta(sourcePath, excludedPath, result))
            {
                return false;
            }

            result.AddMessage($"{name}: excluded folder {sourcePath} -> {excludedPath}");
            return true;
        }

        private void RestoreFolder(string sourcePath, EvoBuildApplyResult result)
        {
            var excludedPath = GetExcludedPath(sourcePath);
            if (!Directory.Exists(ToFullPath(excludedPath)))
            {
                return;
            }

            if (Directory.Exists(ToFullPath(sourcePath)))
            {
                result.AddError($"{name}: cannot restore excluded folder because original path exists: {sourcePath}");
                return;
            }

            if (!MoveFolderWithMeta(excludedPath, sourcePath, result))
            {
                return;
            }

            result.AddMessage($"{name}: restored folder {excludedPath} -> {sourcePath}");
        }

        private string GetExcludedPath(string sourcePath)
        {
            var suffix = string.IsNullOrWhiteSpace(excludedSuffix) ? "~" : excludedSuffix.Trim();
            return sourcePath.EndsWith(suffix, StringComparison.Ordinal)
                ? sourcePath
                : sourcePath + suffix;
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
            try
            {
                var sourceFullPath = ToFullPath(sourceAssetPath);
                var targetFullPath = ToFullPath(targetAssetPath);
                FileUtil.MoveFileOrDirectory(sourceFullPath, targetFullPath);

                var sourceMetaPath = sourceFullPath + ".meta";
                if (File.Exists(sourceMetaPath))
                {
                    FileUtil.MoveFileOrDirectory(sourceMetaPath, targetFullPath + ".meta");
                }

                return true;
            }
            catch (Exception ex)
            {
                result.AddError($"{name}: failed to move '{sourceAssetPath}' -> '{targetAssetPath}': {ex.Message}");
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
    }
}
