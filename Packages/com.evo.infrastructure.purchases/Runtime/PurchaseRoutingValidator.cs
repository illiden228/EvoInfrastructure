using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseRoutingValidator
    {
        public static IReadOnlyList<PurchaseCatalogIssue> Validate(
            PurchaseRoutingConfig routing,
            PlatformCatalog platformCatalog)
        {
            var issues = new List<PurchaseCatalogIssue>();
            if (platformCatalog == null)
            {
                issues.Add(new PurchaseCatalogIssue(
                    PurchaseCatalogIssueSeverity.Error,
                    null,
                    "PlatformCatalog is missing."));
                return issues;
            }

            var currentPlatformId = PurchasePlatformIdUtility.Normalize(platformCatalog.CurrentPlatformId);
            if (currentPlatformId.Length == 0)
            {
                issues.Add(new PurchaseCatalogIssue(
                    PurchaseCatalogIssueSeverity.Error,
                    null,
                    "PlatformCatalog.CurrentPlatformId is empty."));
            }
            else if (!PurchaseCatalogValidator.IsKnownPlatformId(platformCatalog, currentPlatformId))
            {
                issues.Add(new PurchaseCatalogIssue(
                    PurchaseCatalogIssueSeverity.Error,
                    null,
                    $"PlatformCatalog.CurrentPlatformId '{currentPlatformId}' is unknown."));
            }

            if (routing?.Adapters == null)
            {
                return issues;
            }

            for (var bindingIndex = 0; bindingIndex < routing.Adapters.Count; bindingIndex++)
            {
                var binding = routing.Adapters[bindingIndex];
                if (binding == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.AdapterId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        null,
                        $"Adapter binding at index {bindingIndex} has an empty adapter ID."));
                }

                ValidatePlatformIds(binding, bindingIndex, platformCatalog, issues);
            }

            return issues;
        }

        private static void ValidatePlatformIds(
            PurchaseAdapterBinding binding,
            int bindingIndex,
            PlatformCatalog platformCatalog,
            ICollection<PurchaseCatalogIssue> issues)
        {
            if (binding.PlatformIds == null || binding.PlatformIds.Count == 0)
            {
                issues.Add(new PurchaseCatalogIssue(
                    PurchaseCatalogIssueSeverity.Error,
                    null,
                    $"Adapter binding '{binding.AdapterId}' at index {bindingIndex} has no platform IDs."));
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var platformIndex = 0; platformIndex < binding.PlatformIds.Count; platformIndex++)
            {
                var platformId = PurchasePlatformIdUtility.Normalize(binding.PlatformIds[platformIndex]);
                if (platformId.Length == 0)
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        null,
                        $"Adapter binding '{binding.AdapterId}' has an empty platform ID at index {platformIndex}."));
                    continue;
                }

                if (!seen.Add(platformId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        null,
                        $"Adapter binding '{binding.AdapterId}' repeats platform ID '{platformId}'."));
                }

                if (!PurchaseCatalogValidator.IsKnownPlatformId(platformCatalog, platformId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        null,
                        $"Adapter binding '{binding.AdapterId}' uses unknown platform ID '{platformId}'."));
                }
            }
        }
    }
}
