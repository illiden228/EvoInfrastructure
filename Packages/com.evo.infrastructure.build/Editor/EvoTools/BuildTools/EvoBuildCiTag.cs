using System;
using System.Text.RegularExpressions;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [Serializable]
    public readonly struct EvoBuildCiTag
    {
        private static readonly Regex VersionRegex = new(@"^\d+\.\d+\.\d+([-.][A-Za-z0-9]+)*$", RegexOptions.Compiled);

        public EvoBuildCiTag(
            string rawTag,
            string platform,
            string buildType,
            string artifactType,
            string version,
            int buildNumber,
            string debugInfo)
        {
            RawTag = rawTag ?? string.Empty;
            Platform = platform ?? string.Empty;
            BuildType = buildType ?? string.Empty;
            ArtifactType = artifactType ?? string.Empty;
            Version = version ?? string.Empty;
            BuildNumber = buildNumber;
            DebugInfo = debugInfo ?? string.Empty;
        }

        public string RawTag { get; }
        public string Platform { get; }
        public string BuildType { get; }
        public string ArtifactType { get; }
        public string Version { get; }
        public int BuildNumber { get; }
        public string DebugInfo { get; }
        public bool HasDebugInfo => !string.IsNullOrWhiteSpace(DebugInfo);

        public static bool TryParse(string value, out EvoBuildCiTag tag, out string error)
        {
            tag = default;
            error = string.Empty;
            value = value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "CI build tag is empty.";
                return false;
            }

            var parts = value.Split('_');
            if (parts.Length != 5 && parts.Length != 6)
            {
                error = "CI build tag must match {platform}_{buildType}_{artifactType}_{version}_{buildNumber}_{debugInfo?}.";
                return false;
            }

            if (!ValidateToken(parts[0], "platform", out error) ||
                !ValidateToken(parts[1], "buildType", out error) ||
                !ValidateToken(parts[2], "artifactType", out error))
            {
                return false;
            }

            if (!VersionRegex.IsMatch(parts[3]))
            {
                error = $"CI build tag version '{parts[3]}' is invalid. Expected semver-like value, for example 8.5.17.";
                return false;
            }

            if (!int.TryParse(parts[4], out var buildNumber) || buildNumber <= 0)
            {
                error = $"CI build tag buildNumber '{parts[4]}' is invalid. Expected positive integer.";
                return false;
            }

            if (parts.Length == 6 && !ValidateToken(parts[5], "debugInfo", out error))
            {
                return false;
            }

            tag = new EvoBuildCiTag(
                value,
                Normalize(parts[0]),
                Normalize(parts[1]),
                Normalize(parts[2]),
                parts[3],
                buildNumber,
                parts.Length == 6 ? Normalize(parts[5]) : string.Empty);
            return true;
        }

        private static bool ValidateToken(string value, string name, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"CI build tag {name} is empty.";
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    error = $"CI build tag {name} '{value}' contains unsupported character '{c}'. Use letters, digits or '-'.";
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
