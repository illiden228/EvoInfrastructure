using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Evo.Infrastructure.Editor.EvoTools.Build;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Build.Editor.Tests
{
    public sealed class ExcludeFoldersFromBuildStepTests
    {
        private const string TestRoot = "Assets/__EvoExcludeFoldersTest";
        private const string SourcePath = TestRoot + "/_Downloads";
        private const string ExcludedPath = SourcePath + "~";

        [SetUp]
        public void SetUp()
        {
            DeleteAssetArtifacts(TestRoot);
            Directory.CreateDirectory(ToFullPath(TestRoot));
        }

        [TearDown]
        public void TearDown()
        {
            DeleteAssetArtifacts(TestRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Execute_SourceExists_MovesFolderAndMetaThenCleanupRestores()
        {
            CreateFolderWithMeta(SourcePath, "source.txt", "source");
            var step = CreateStep(SourcePath);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.False);
            Assert.That(File.Exists(ToFullPath(SourcePath) + ".meta"), Is.False);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.True);
            Assert.That(File.Exists(ToFullPath(ExcludedPath) + ".meta"), Is.True);

            step.Cleanup(null, result);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.True);
            Assert.That(File.Exists(ToFullPath(SourcePath) + ".meta"), Is.True);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.False);
            Assert.That(File.Exists(ToFullPath(ExcludedPath) + ".meta"), Is.False);
        }

        [Test]
        public void Execute_MissingFolderWithSkip_DoesNotFail()
        {
            var step = CreateStep(SourcePath, ExcludeFoldersMissingFolderBehavior.Skip);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Messages.Any(message => message.Contains("folder already absent")), Is.True);
        }

        [Test]
        public void Execute_AlreadyExcludedWithSkip_TracksAndCleanupRestores()
        {
            CreateFolderWithMeta(ExcludedPath, "excluded.txt", "excluded");
            var step = CreateStep(SourcePath, ExcludeFoldersMissingFolderBehavior.Skip);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));
            step.Cleanup(null, result);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.True);
            Assert.That(File.Exists(ToFullPath(SourcePath) + ".meta"), Is.True);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.False);
            Assert.That(File.Exists(ToFullPath(ExcludedPath) + ".meta"), Is.False);
        }

        [Test]
        public void Execute_ConflictWithDefaultBehavior_Fails()
        {
            CreateFolderWithMeta(SourcePath, "source.txt", "source");
            CreateFolderWithMeta(ExcludedPath, "excluded.txt", "excluded");
            var step = CreateStep(SourcePath);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("source and excluded artifacts both exist")), Is.True);
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.True);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.True);
        }

        [Test]
        public void Execute_ConflictWithDeleteExcluded_DeletesDuplicateAndExcludesSource()
        {
            CreateFolderWithMeta(SourcePath, "source.txt", "source");
            CreateFolderWithMeta(ExcludedPath, "stale.txt", "stale");
            var step = CreateStep(
                SourcePath,
                ExcludeFoldersMissingFolderBehavior.Skip,
                ExcludeFoldersConflictBehavior.DeleteExcluded);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));

            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.False);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.True);
            Assert.That(File.Exists(Path.Combine(ToFullPath(ExcludedPath), "source.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(ToFullPath(ExcludedPath), "stale.txt")), Is.False);
            Assert.That(File.Exists(ToFullPath(ExcludedPath) + ".meta"), Is.True);

            step.Cleanup(null, result);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.True);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.False);
        }

        [Test]
        public void Execute_TrackedConflictWithDeleteExcluded_DeletesDuplicateBeforeBuild()
        {
            CreateFolderWithMeta(ExcludedPath, "stale.txt", "stale");
            var step = CreateStep(
                SourcePath,
                ExcludeFoldersMissingFolderBehavior.Skip,
                ExcludeFoldersConflictBehavior.DeleteExcluded);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));
            CreateFolderWithMeta(SourcePath, "source.txt", "source");

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));

            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.False);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.True);
            Assert.That(File.Exists(Path.Combine(ToFullPath(ExcludedPath), "source.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(ToFullPath(ExcludedPath), "stale.txt")), Is.False);

            step.Cleanup(null, result);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.True);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.False);
        }

        [Test]
        public void Cleanup_CalledTwice_DoesNotFail()
        {
            CreateFolderWithMeta(SourcePath, "source.txt", "source");
            var step = CreateStep(SourcePath);
            var result = new EvoBuildApplyResult();

            Assert.That(step.Execute(null, result), Is.True, string.Join("\n", result.Errors));
            step.Cleanup(null, result);
            step.Cleanup(null, result);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(Directory.Exists(ToFullPath(SourcePath)), Is.True);
            Assert.That(Directory.Exists(ToFullPath(ExcludedPath)), Is.False);
        }

        private static ExcludeFoldersFromBuildStep CreateStep(
            string folderPath,
            ExcludeFoldersMissingFolderBehavior missingFolderBehavior = ExcludeFoldersMissingFolderBehavior.Fail,
            ExcludeFoldersConflictBehavior conflictBehavior = ExcludeFoldersConflictBehavior.Fail)
        {
            var step = ScriptableObject.CreateInstance<ExcludeFoldersFromBuildStep>();
            step.name = "ExcludeFoldersFromBuildStepTests";
            SetField(step, "folderPaths", new List<string> { folderPath });
            SetField(step, "missingFolderBehavior", missingFolderBehavior);
            SetField(step, "conflictBehavior", conflictBehavior);
            return step;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field not found: {fieldName}");
            field.SetValue(target, value);
        }

        private static void CreateFolderWithMeta(string assetPath, string fileName, string fileContent)
        {
            var fullPath = ToFullPath(assetPath);
            Directory.CreateDirectory(fullPath);
            File.WriteAllText(Path.Combine(fullPath, fileName), fileContent);
            File.WriteAllText(fullPath + ".meta", "fileFormatVersion: 2\n");
        }

        private static void DeleteAssetArtifacts(string assetPath)
        {
            var fullPath = ToFullPath(assetPath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            if (File.Exists(fullPath + ".meta"))
            {
                File.Delete(fullPath + ".meta");
            }
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.Combine(projectRoot ?? string.Empty, assetPath).Replace('\\', '/');
        }
    }
}
