#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Evo.Infrastructure.Core.Editor
{
    public static class EvoAsmdefUtility
    {
        private const string PackagesRoot = "Packages";
        private const string PackagePrefix = "com.evo.infrastructure.";

        private static readonly Dictionary<string, string> PackageAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["com.evo.infrastructure.ads"] = "Evo.Infrastructure.Ads",
            ["com.evo.infrastructure.analytics"] = "Evo.Infrastructure.Analytics",
            ["com.evo.infrastructure.analytics.adjust"] = "Evo.Infrastructure.Analytics.Adjust",
            ["com.evo.infrastructure.analytics.appmetrica"] = "Evo.Infrastructure.Analytics.AppMetrica",
            ["com.evo.infrastructure.analytics.firebase"] = "Evo.Infrastructure.Analytics.Firebase",
            ["com.evo.infrastructure.audio"] = "Evo.Infrastructure.Audio",
            ["com.evo.infrastructure.ads.applovin"] = "Evo.Infrastructure.Ads.AppLovin",
            ["com.evo.infrastructure.build"] = "Evo.Infrastructure.Build.Editor",
            ["com.evo.infrastructure.config"] = "Evo.Infrastructure.Config",
            ["com.evo.infrastructure.core"] = "Evo.Infrastructure.Core.Editor",
            ["com.evo.infrastructure.crazygames"] = "Evo.Infrastructure.CrazyGames",
            ["com.evo.infrastructure.crazygames.ads"] = "Evo.Infrastructure.CrazyGames.Ads",
            ["com.evo.infrastructure.crazygames.leaderboards"] = "Evo.Infrastructure.CrazyGames.Leaderboards",
            ["com.evo.infrastructure.crazygames.platform"] = "Evo.Infrastructure.CrazyGames.Platform",
            ["com.evo.infrastructure.crazygames.save"] = "Evo.Infrastructure.CrazyGames.Save",
            ["com.evo.infrastructure.debug"] = "Evo.Infrastructure.Debug",
            ["com.evo.infrastructure.di"] = "Evo.Infrastructure.DI",
            ["com.evo.infrastructure.editor-tools"] = "Evo.Infrastructure.EditorTools.Editor",
            ["com.evo.infrastructure.focus"] = "Evo.Infrastructure.Focus",
            ["com.evo.infrastructure.leaderboards"] = "Evo.Infrastructure.Leaderboards",
            ["com.evo.infrastructure.loading"] = "Evo.Infrastructure.Loading",
            ["com.evo.infrastructure.localization"] = "Evo.Infrastructure.Localization",
            ["com.evo.infrastructure.platform"] = "Evo.Infrastructure.Platform",
            ["com.evo.infrastructure.pooling"] = "Evo.Infrastructure.Pooling",
            ["com.evo.infrastructure.resources"] = "Evo.Infrastructure.Resources",
            ["com.evo.infrastructure.save"] = "Evo.Infrastructure.Save",
            ["com.evo.infrastructure.scene"] = "Evo.Infrastructure.Scene",
            ["com.evo.infrastructure.ui"] = "Evo.Infrastructure.UI",
            ["com.evo.infrastructure.yandex"] = "Evo.Infrastructure.Yandex",
            ["com.evo.infrastructure.yandex.ads"] = "Evo.Infrastructure.Yandex.Ads",
            ["com.evo.infrastructure.yandex.analytics"] = "Evo.Infrastructure.Yandex.Analytics",
            ["com.evo.infrastructure.yandex.leaderboards"] = "Evo.Infrastructure.Yandex.Leaderboards",
            ["com.evo.infrastructure.yandex.platform"] = "Evo.Infrastructure.Yandex.Platform",
            ["com.evo.infrastructure.yandex.save"] = "Evo.Infrastructure.Yandex.Save"
        };

        private static readonly HashSet<string> PackagesWaitingForSdkFacade = new(StringComparer.OrdinalIgnoreCase)
        {
            "com.evo.infrastructure.crazygames",
            "com.evo.infrastructure.crazygames.ads",
            "com.evo.infrastructure.crazygames.leaderboards",
            "com.evo.infrastructure.crazygames.platform",
            "com.evo.infrastructure.crazygames.save",
            "com.evo.infrastructure.yandex",
            "com.evo.infrastructure.yandex.ads",
            "com.evo.infrastructure.yandex.analytics",
            "com.evo.infrastructure.yandex.leaderboards",
            "com.evo.infrastructure.yandex.platform",
            "com.evo.infrastructure.yandex.save"
        };

        private static readonly Dictionary<string, string[]> ExternalReferences = new(StringComparer.OrdinalIgnoreCase)
        {
            ["com.evo.infrastructure.ads"] = new[] { "R3", "UniTask", "VContainer" },
            ["com.evo.infrastructure.analytics"] = new[] { "UniTask", "VContainer" },
            ["com.evo.infrastructure.audio"] = new[] { "UniTask", "VContainer" },
            ["com.evo.infrastructure.config"] = new[] { "VContainer" },
            ["com.evo.infrastructure.di"] = new[] { "VContainer" },
            ["com.evo.infrastructure.editor-tools"] = new[] { "Unity.Addressables.Editor", "Unity.Localization.Editor" },
            ["com.evo.infrastructure.leaderboards"] = new[] { "UniTask", "VContainer" },
            ["com.evo.infrastructure.loading"] = new[] { "UniTask", "VContainer" },
            ["com.evo.infrastructure.localization"] = new[] { "UniTask", "Unity.Localization", "VContainer" },
            ["com.evo.infrastructure.platform"] = new[] { "VContainer" },
            ["com.evo.infrastructure.resources"] = new[] { "UniTask", "Unity.Addressables", "Unity.ResourceManager", "VContainer" },
            ["com.evo.infrastructure.save"] = new[] { "UniTask", "VContainer" },
            ["com.evo.infrastructure.scene"] = new[] { "UniTask", "VContainer" },
            ["com.evo.infrastructure.ui"] = new[] { "ObservableCollections", "ObservableCollections.R3", "PrimeTween.Runtime", "R3", "R3.Unity", "UniTask", "Unity.Addressables", "Unity.InputSystem", "Unity.ResourceManager", "Unity.TextMeshPro", "VContainer" }
        };

        private static readonly SdkAsmdefRequirement[] SdkAsmdefs =
        {
            new("com.evo.infrastructure.analytics.adjust", "Runtime/Sdk/Evo.Infrastructure.Analytics.Adjust.Sdk.asmdef", "AdjustSdk.Scripts", "EVO_ADJUST_SDK", "com.adjust.sdk"),
            new("com.evo.infrastructure.analytics.appmetrica", "Runtime/Sdk/Evo.Infrastructure.Analytics.AppMetrica.Sdk.asmdef", "AppMetrica", "EVO_APPMETRICA_SDK", "io.appmetrica.analytics"),
            new("com.evo.infrastructure.analytics.firebase", "Runtime/Sdk/Evo.Infrastructure.Analytics.Firebase.Sdk.asmdef", "Firebase.Analytics", "EVO_FIREBASE_ANALYTICS_SDK", null),
            new("com.evo.infrastructure.ads.applovin", "Runtime/Sdk/Evo.Infrastructure.Ads.AppLovin.Sdk.asmdef", "MaxSdk.Scripts", "EVO_APPLOVIN_MAX_SDK", "com.applovin.mediation.ads")
        };

        [MenuItem("EvoTools/Asmdefs/Validate Evo asmdefs")]
        public static void ValidateAsmdefs()
        {
            var report = BuildAsmdefReport(write: false);
            if (report.Count == 0)
            {
                Debug.Log("[Evo Asmdefs] No issues found.");
                return;
            }

            Debug.LogWarning("[Evo Asmdefs]\n" + string.Join("\n", report));
        }

        [MenuItem("EvoTools/Asmdefs/Generate or Update Evo asmdefs")]
        public static void GenerateOrUpdateAsmdefs()
        {
            var report = BuildAsmdefReport(write: true);
            AssetDatabase.Refresh();
            Debug.Log("[Evo Asmdefs]\n" + (report.Count == 0 ? "Asmdefs are up to date." : string.Join("\n", report)));
        }

        private static List<string> BuildAsmdefReport(bool write)
        {
            var report = new List<string>();
            var packageDirs = Directory.GetDirectories(PackagesRoot, PackagePrefix + "*", SearchOption.TopDirectoryOnly);
            foreach (var packageDir in packageDirs)
            {
                var packageId = Path.GetFileName(packageDir);
                if (string.IsNullOrWhiteSpace(packageId) || !PackageAssemblyNames.TryGetValue(packageId, out var assemblyName))
                {
                    continue;
                }

                if (PackagesWaitingForSdkFacade.Contains(packageId))
                {
                    report.Add($"Skipped asmdef generation for {packageId}: platform SDK assembly references must be verified first.");
                    continue;
                }

                var runtimePath = Path.Combine(packageDir, "Runtime");
                var editorPath = Path.Combine(packageDir, "Editor");
                if (Directory.Exists(runtimePath) && HasCsFiles(runtimePath))
                {
                    EnsureAsmdef(packageId, runtimePath, assemblyName, Array.Empty<string>(), report, write);
                }

                if (Directory.Exists(editorPath) && HasCsFiles(editorPath))
                {
                    EnsureAsmdef(packageId, editorPath, assemblyName.EndsWith(".Editor", StringComparison.Ordinal) ? assemblyName : assemblyName + ".Editor", new[] { "Editor" }, report, write);
                }
            }

            ValidateSdkAsmdefs(report);

            return report;
        }

        private static void ValidateSdkAsmdefs(List<string> report)
        {
            foreach (var requirement in SdkAsmdefs)
            {
                var path = Path.Combine(PackagesRoot, requirement.PackageId, requirement.RelativePath).Replace("\\", "/");
                if (!File.Exists(path))
                {
                    report.Add($"Missing guarded SDK asmdef: {path}");
                    continue;
                }

                var asmdef = JsonUtility.FromJson<AsmdefModel>(File.ReadAllText(path));
                var hasVersionDefine = string.IsNullOrEmpty(requirement.VersionDefinePackage) ||
                    (asmdef?.versionDefines ?? Array.Empty<VersionDefineModel>()).Any(value =>
                        string.Equals(value.name, requirement.VersionDefinePackage, StringComparison.Ordinal) &&
                        string.Equals(value.define, requirement.Define, StringComparison.Ordinal));
                if (asmdef == null || !(asmdef.references ?? Array.Empty<string>()).Contains(requirement.SdkAssembly) ||
                    !(asmdef.defineConstraints ?? Array.Empty<string>()).Contains(requirement.Define) || !hasVersionDefine)
                {
                    report.Add($"Invalid guarded SDK asmdef: {path}. Expected reference '{requirement.SdkAssembly}', constraint '{requirement.Define}', and its package version define when applicable.");
                }
            }
        }

        private static void EnsureAsmdef(
            string packageId,
            string assemblyRoot,
            string assemblyName,
            string[] includePlatforms,
            List<string> report,
            bool write)
        {
            var asmdefPath = Path.Combine(assemblyRoot, assemblyName + ".asmdef").Replace("\\", "/");
            var expected = CreateExpectedAsmdef(packageId, assemblyRoot, assemblyName, includePlatforms);
            if (!File.Exists(asmdefPath))
            {
                report.Add($"Missing asmdef: {asmdefPath}");
                if (write)
                {
                    WriteAsmdef(asmdefPath, expected);
                }

                return;
            }

            var current = JsonUtility.FromJson<AsmdefModel>(File.ReadAllText(asmdefPath));
            var merged = MergeAsmdef(current, expected);
            if (!AsmdefEquals(current, merged))
            {
                report.Add($"Outdated asmdef: {asmdefPath}");
                if (write)
                {
                    WriteAsmdef(asmdefPath, merged);
                }
            }
        }

        private static AsmdefModel CreateExpectedAsmdef(string packageId, string assemblyRoot, string assemblyName, string[] includePlatforms)
        {
            var references = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var dependency in ReadPackageDependencies(packageId))
            {
                if (PackageAssemblyNames.TryGetValue(dependency, out var dependencyAssembly))
                {
                    references.Add(dependencyAssembly);
                }
            }

            if (ExternalReferences.TryGetValue(packageId, out var externalReferences))
            {
                foreach (var reference in externalReferences)
                {
                    references.Add(reference);
                }
            }

            AddOdinReferencesIfNeeded(assemblyRoot, includePlatforms, references);

            return new AsmdefModel
            {
                name = assemblyName,
                references = references.ToArray(),
                includePlatforms = includePlatforms ?? Array.Empty<string>(),
                excludePlatforms = Array.Empty<string>(),
                allowUnsafeCode = false,
                overrideReferences = false,
                precompiledReferences = Array.Empty<string>(),
                autoReferenced = true,
                defineConstraints = Array.Empty<string>(),
                versionDefines = Array.Empty<VersionDefineModel>(),
                noEngineReferences = false
            };
        }

        private static IEnumerable<string> ReadPackageDependencies(string packageId)
        {
            var packageJsonPath = Path.Combine(PackagesRoot, packageId, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                yield break;
            }

            var json = File.ReadAllText(packageJsonPath);
            foreach (Match match in Regex.Matches(json, "\"(com\\.evo\\.infrastructure\\.[^\"]+)\"\\s*:"))
            {
                yield return match.Groups[1].Value;
            }
        }

        private static void AddOdinReferencesIfNeeded(string assemblyRoot, string[] includePlatforms, ISet<string> references)
        {
            var source = string.Join("\n", Directory.GetFiles(assemblyRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            if (source.IndexOf("Sirenix.", StringComparison.Ordinal) < 0)
            {
                return;
            }

            AddReferenceIfAssemblyExists(references, "Sirenix.OdinInspector.Attributes");
            if (includePlatforms != null && includePlatforms.Contains("Editor"))
            {
                AddReferenceIfAssemblyExists(references, "Sirenix.OdinInspector.Editor");
                AddReferenceIfAssemblyExists(references, "Sirenix.Utilities.Editor");
                AddReferenceIfAssemblyExists(references, "Sirenix.Utilities");
            }
        }

        private static void AddReferenceIfAssemblyExists(ISet<string> references, string assemblyName)
        {
            if (CompilationPipeline.GetAssemblies().Any(assembly => string.Equals(assembly.name, assemblyName, StringComparison.Ordinal)))
            {
                references.Add(assemblyName);
            }
        }

        private static AsmdefModel MergeAsmdef(AsmdefModel current, AsmdefModel expected)
        {
            if (current == null)
            {
                return expected;
            }

            current.name = expected.name;
            current.references = Merge(current.references, expected.references);
            current.includePlatforms = expected.includePlatforms;
            current.excludePlatforms ??= Array.Empty<string>();
            current.precompiledReferences ??= Array.Empty<string>();
            current.defineConstraints ??= Array.Empty<string>();
            current.versionDefines ??= Array.Empty<VersionDefineModel>();
            return current;
        }

        private static string[] Merge(string[] current, string[] expected)
        {
            return (current ?? Array.Empty<string>())
                .Concat(expected ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool AsmdefEquals(AsmdefModel left, AsmdefModel right)
        {
            return JsonUtility.ToJson(left, prettyPrint: true) == JsonUtility.ToJson(right, prettyPrint: true);
        }

        private static void WriteAsmdef(string path, AsmdefModel model)
        {
            File.WriteAllText(path, JsonUtility.ToJson(model, prettyPrint: true) + Environment.NewLine);
        }

        private static bool HasCsFiles(string path)
        {
            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).Length > 0;
        }

        [Serializable]
        private sealed class AsmdefModel
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public VersionDefineModel[] versionDefines;
            public bool noEngineReferences;
        }

        [Serializable]
        private sealed class VersionDefineModel
        {
            public string name;
            public string expression;
            public string define;
        }

        private readonly struct SdkAsmdefRequirement
        {
            public readonly string PackageId;
            public readonly string RelativePath;
            public readonly string SdkAssembly;
            public readonly string Define;
            public readonly string VersionDefinePackage;

            public SdkAsmdefRequirement(string packageId, string relativePath, string sdkAssembly, string define, string versionDefinePackage)
            {
                PackageId = packageId;
                RelativePath = relativePath;
                SdkAssembly = sdkAssembly;
                Define = define;
                VersionDefinePackage = versionDefinePackage;
            }
        }
    }
}
#endif
