using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Analytics.Config;

namespace _Project.Scripts.Infrastructure.Services.Analytics.Mapping
{
    public sealed class AnalyticsEventMapper
    {
        private readonly AnalyticsEventMappingConfig _config;

        public AnalyticsEventMapper(AnalyticsEventMappingConfig config)
        {
            _config = config;
        }

        public string ResolveEventKey(string canonicalEventKey, string adapterId, string platformId)
        {
            if (string.IsNullOrWhiteSpace(canonicalEventKey) || _config == null)
            {
                return canonicalEventKey;
            }

            if (!TryGetEntry(canonicalEventKey, out var entry))
            {
                return canonicalEventKey;
            }

            var resolved = string.IsNullOrWhiteSpace(entry.DefaultEventKey)
                ? canonicalEventKey
                : entry.DefaultEventKey;

            if (TryGetPlatformOverride(entry.PlatformOverrides, platformId, out var platformOverride) &&
                !string.IsNullOrWhiteSpace(platformOverride.EventKey))
            {
                resolved = platformOverride.EventKey;
            }

            if (TryGetAdapterOverride(entry.AdapterOverrides, adapterId, out var adapterOverride))
            {
                if (!string.IsNullOrWhiteSpace(adapterOverride.EventKey))
                {
                    resolved = adapterOverride.EventKey;
                }

                if (TryGetPlatformOverride(adapterOverride.PlatformOverrides, platformId, out var adapterPlatformOverride) &&
                    !string.IsNullOrWhiteSpace(adapterPlatformOverride.EventKey))
                {
                    resolved = adapterPlatformOverride.EventKey;
                }
            }

            return resolved;
        }

        public IReadOnlyDictionary<string, object> MapParameters(
            string canonicalEventKey,
            string adapterId,
            string platformId,
            IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0 || _config == null)
            {
                return parameters;
            }

            if (!TryGetEntry(canonicalEventKey, out var entry))
            {
                return parameters;
            }

            var mapped = Copy(parameters);
            ApplyMappings(mapped, entry.ParameterMappings);

            if (TryGetPlatformOverride(entry.PlatformOverrides, platformId, out var platformOverride))
            {
                ApplyMappings(mapped, platformOverride.ParameterMappings);
            }

            if (TryGetAdapterOverride(entry.AdapterOverrides, adapterId, out var adapterOverride))
            {
                ApplyMappings(mapped, adapterOverride.ParameterMappings);
                if (TryGetPlatformOverride(adapterOverride.PlatformOverrides, platformId, out var adapterPlatformOverride))
                {
                    ApplyMappings(mapped, adapterPlatformOverride.ParameterMappings);
                }
            }

            return mapped;
        }

        private bool TryGetEntry(string canonicalEventKey, out AnalyticsEventMappingConfig.EventMappingEntry entry)
        {
            var mappings = _config.EventMappings;
            for (var i = 0; i < mappings.Count; i++)
            {
                var candidate = mappings[i];
                if (string.Equals(candidate.CanonicalEventKey, canonicalEventKey, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private static bool TryGetAdapterOverride(
            List<AnalyticsEventMappingConfig.AdapterOverride> overrides,
            string adapterId,
            out AnalyticsEventMappingConfig.AdapterOverride result)
        {
            if (overrides != null && !string.IsNullOrWhiteSpace(adapterId))
            {
                for (var i = 0; i < overrides.Count; i++)
                {
                    var candidate = overrides[i];
                    if (string.Equals(candidate.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private static bool TryGetPlatformOverride(
            List<AnalyticsEventMappingConfig.PlatformOverride> overrides,
            string platformId,
            out AnalyticsEventMappingConfig.PlatformOverride result)
        {
            if (overrides != null && !string.IsNullOrWhiteSpace(platformId))
            {
                for (var i = 0; i < overrides.Count; i++)
                {
                    var candidate = overrides[i];
                    if (string.Equals(candidate.PlatformId, platformId, StringComparison.OrdinalIgnoreCase))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private static Dictionary<string, object> Copy(IReadOnlyDictionary<string, object> parameters)
        {
            var copy = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in parameters)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }

        private static void ApplyMappings(
            Dictionary<string, object> target,
            List<AnalyticsEventMappingConfig.ParameterMapping> mappings)
        {
            if (target == null || mappings == null || mappings.Count == 0)
            {
                return;
            }

            for (var i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                if (string.IsNullOrWhiteSpace(mapping.SourceKey) || string.IsNullOrWhiteSpace(mapping.TargetKey))
                {
                    continue;
                }

                if (!target.TryGetValue(mapping.SourceKey, out var value))
                {
                    continue;
                }

                target.Remove(mapping.SourceKey);
                target[mapping.TargetKey] = value;
            }
        }
    }
}
