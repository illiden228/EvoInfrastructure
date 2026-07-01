using System;
using System.Collections.Generic;
using System.IO;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public static class EvoBuildCiEntryPoint
    {
        private const string DefaultResultPath = "Builds/ci-build-result.json";

        public static void BuildFromEnvironment()
        {
            var request = CreateRequestFromEnvironment();
            var result = Build(request);
            LogResult(result);
            WriteResultJson(result, request);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(result != null && result.Success && result.BuildSucceeded ? 0 : 1);
            }
        }

        public static EvoBuildApplyResult Build(EvoBuildCiRequest request)
        {
            request ??= new EvoBuildCiRequest();
            var result = new EvoBuildApplyResult();
            if (!ResolveRequest(request, result))
            {
                return result;
            }

            LogRequest(request);
            return EvoBuildExecutor.Build(
                request.GlobalConfig,
                request.Profile,
                request.PlatformCatalog,
                new EvoBuildExecutorOptions
                {
                    BuildAndRun = false,
                    Interactive = false,
                    RevealOutput = false,
                    OutputPathOverride = request.OutputPathOverride,
                    CiRequest = request
                });
        }

        private static EvoBuildCiRequest CreateRequestFromEnvironment()
        {
            var args = CommandLineArgs.Parse(Environment.GetCommandLineArgs());
            return new EvoBuildCiRequest
            {
                BuildTag = First(Environment.GetEnvironmentVariable("CI_BUILD_TAG"), args.Get("buildTag")),
                OutputPathOverride = First(Environment.GetEnvironmentVariable("CI_OUTPUT_PATH"), args.Get("outputPath")),
                ProfileGuid = First(Environment.GetEnvironmentVariable("CI_BUILD_PROFILE_GUID"), args.Get("profileGuid")),
                ProfileId = First(Environment.GetEnvironmentVariable("CI_BUILD_PROFILE_ID"), args.Get("profileId")),
                GlobalConfigGuid = First(Environment.GetEnvironmentVariable("CI_BUILD_GLOBAL_CONFIG_GUID"), args.Get("globalConfigGuid")),
                PlatformCatalogGuid = First(Environment.GetEnvironmentVariable("CI_PLATFORM_CATALOG_GUID"), args.Get("platformCatalogGuid")),
                CiConfigGuid = First(Environment.GetEnvironmentVariable("CI_BUILD_CONFIG_GUID"), args.Get("ciConfigGuid"))
            };
        }

        private static bool ResolveRequest(EvoBuildCiRequest request, EvoBuildApplyResult result)
        {
            if (!EvoBuildCiTag.TryParse(request.BuildTag, out var tag, out var tagError))
            {
                result.AddError(tagError);
                return false;
            }

            request.ParsedTag = tag;
            request.CiConfig ??= LoadByGuid<EvoBuildCiConfig>(request.CiConfigGuid) ?? FindSingleAsset<EvoBuildCiConfig>(result, required: false);
            request.GlobalConfig ??= LoadByGuid<BuildGlobalConfig>(request.GlobalConfigGuid) ?? request.CiConfig?.GlobalConfig ?? FindSingleAsset<BuildGlobalConfig>(result, required: false);
            request.PlatformCatalog ??= LoadByGuid<PlatformCatalog>(request.PlatformCatalogGuid) ?? request.CiConfig?.PlatformCatalog ?? FindSingleAsset<PlatformCatalog>(result, required: false);
            request.Profile ??= LoadByGuid<PlatformBuildProfile>(request.ProfileGuid);
            request.Profile ??= ResolveProfileById(request.GlobalConfig, request.ProfileId);
            request.Profile ??= request.CiConfig?.ResolveProfile(tag);

            if (request.GlobalConfig == null)
            {
                result.AddError("BuildGlobalConfig is missing. Set CI_BUILD_GLOBAL_CONFIG_GUID/-globalConfigGuid or assign it in EvoBuildCiConfig.");
            }

            if (request.Profile == null)
            {
                result.AddError($"Build profile was not resolved for tag '{tag.RawTag}'. Set CI_BUILD_PROFILE_GUID/-profileGuid, CI_BUILD_PROFILE_ID/-profileId, or add a matching EvoBuildCiConfig rule.");
            }

            if (request.PlatformCatalog == null)
            {
                result.AddMessage("PlatformCatalog is missing. Build will continue without changing currentPlatformId.");
            }

            return result.Success;
        }

        private static PlatformBuildProfile ResolveProfileById(BuildGlobalConfig globalConfig, string profileId)
        {
            if (globalConfig?.Profiles == null || string.IsNullOrWhiteSpace(profileId))
            {
                return null;
            }

            for (var i = 0; i < globalConfig.Profiles.Count; i++)
            {
                var profile = globalConfig.Profiles[i];
                if (profile != null && string.Equals(profile.ProfileId, profileId, StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }

            return null;
        }

        private static T LoadByGuid<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid.Trim());
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T FindSingleAsset<T>(EvoBuildApplyResult result, bool required) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids == null || guids.Length == 0)
            {
                if (required)
                {
                    result.AddError($"{typeof(T).Name} asset was not found.");
                }

                return null;
            }

            if (guids.Length > 1)
            {
                result.AddMessage($"Found {guids.Length} {typeof(T).Name} assets. Use an explicit GUID to avoid ambiguity; using the first one.");
            }

            return LoadByGuid<T>(guids[0]);
        }

        private static void LogRequest(EvoBuildCiRequest request)
        {
            var tag = request.ParsedTag;
            Debug.Log($"[Evo Build CI] tag={tag.RawTag}, platform={tag.Platform}, buildType={tag.BuildType}, artifactType={tag.ArtifactType}, version={tag.Version}, buildNumber={tag.BuildNumber}, debugInfo={tag.DebugInfo}");
            Debug.Log($"[Evo Build CI] profile={request.Profile?.ProfileId}, outputOverride={request.OutputPathOverride}");
        }

        private static void LogResult(EvoBuildApplyResult result)
        {
            if (result == null)
            {
                Debug.LogError("[Evo Build CI] Result is null.");
                return;
            }

            for (var i = 0; i < result.Messages.Count; i++)
            {
                Debug.Log($"[Evo Build CI] {result.Messages[i]}");
            }

            for (var i = 0; i < result.Errors.Count; i++)
            {
                Debug.LogError($"[Evo Build CI] {result.Errors[i]}");
            }
        }

        private static void WriteResultJson(EvoBuildApplyResult result, EvoBuildCiRequest request)
        {
            try
            {
                var dto = new CiBuildResultDto(result, request);
                var path = DefaultResultPath;
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, JsonUtility.ToJson(dto, true));
                Debug.Log($"[Evo Build CI] Result json saved: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Evo Build CI] Failed to write result json: {ex.Message}");
            }
        }

        private static string First(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : second;
        }

        [Serializable]
        private sealed class CiBuildResultDto
        {
            public bool success;
            public bool buildSucceeded;
            public string outputPath;
            public string profileId;
            public string platform;
            public string buildType;
            public string artifactType;
            public string version;
            public int buildNumber;
            public string debugInfo;
            public string gitTag;
            public string[] messages;
            public string[] errors;

            public CiBuildResultDto(EvoBuildApplyResult result, EvoBuildCiRequest request)
            {
                var tag = request?.ParsedTag ?? default;
                success = result != null && result.Success && result.BuildSucceeded;
                buildSucceeded = result?.BuildSucceeded == true;
                outputPath = result?.OutputPath ?? string.Empty;
                profileId = request?.Profile?.ProfileId ?? string.Empty;
                platform = tag.Platform;
                buildType = tag.BuildType;
                artifactType = tag.ArtifactType;
                version = tag.Version;
                buildNumber = tag.BuildNumber;
                debugInfo = tag.DebugInfo;
                gitTag = tag.RawTag;
                messages = ToArray(result?.Messages);
                errors = ToArray(result?.Errors);
            }

            private static string[] ToArray(IReadOnlyList<string> values)
            {
                if (values == null || values.Count == 0)
                {
                    return Array.Empty<string>();
                }

                var result = new string[values.Count];
                for (var i = 0; i < values.Count; i++)
                {
                    result[i] = values[i];
                }

                return result;
            }
        }

        private sealed class CommandLineArgs
        {
            private readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

            public static CommandLineArgs Parse(string[] args)
            {
                var result = new CommandLineArgs();
                if (args == null)
                {
                    return result;
                }

                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (string.IsNullOrWhiteSpace(arg) || !arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var key = arg.TrimStart('-');
                    if (string.IsNullOrWhiteSpace(key) || i + 1 >= args.Length)
                    {
                        continue;
                    }

                    var value = args[i + 1];
                    if (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("-", StringComparison.Ordinal))
                    {
                        result.values[key] = value;
                        i++;
                    }
                }

                return result;
            }

            public string Get(string key)
            {
                return !string.IsNullOrWhiteSpace(key) && values.TryGetValue(key, out var value) ? value : string.Empty;
            }
        }
    }
}
