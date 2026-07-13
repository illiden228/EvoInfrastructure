using System;
using System.Collections.Generic;
using System.Linq;
using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseCatalogValidator
    {
        public static IReadOnlyList<PurchaseCatalogIssue> Validate(
            PurchaseCatalogConfig catalog,
            PlatformCatalog platformCatalog = null)
        {
            var issues = new List<PurchaseCatalogIssue>();
            if (catalog?.Products == null)
            {
                return issues;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var defaultStoreIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var product in catalog.Products)
            {
                if (product == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(product.Id))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        null,
                        "Product has an empty logical ID."));
                    continue;
                }

                if (!ids.Add(product.Id))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        product.Id,
                        "Logical ID is duplicated."));
                }

                if (string.IsNullOrWhiteSpace(product.FulfillmentKey))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        product.Id,
                        "Fulfillment key is empty."));
                }

                if (string.IsNullOrWhiteSpace(product.DefaultStoreProductId) &&
                    (product.Overrides == null || product.Overrides.Count == 0))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Warning,
                        product.Id,
                        "No default or target-specific store product ID is configured."));
                }

                if (!string.IsNullOrWhiteSpace(product.DefaultStoreProductId) &&
                    !defaultStoreIds.Add(product.DefaultStoreProductId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        product.Id,
                        $"Default store product ID '{product.DefaultStoreProductId}' is mapped by multiple products."));
                }

                ValidateOverrides(product, platformCatalog, issues);
            }

            return issues;
        }

        private static void ValidateOverrides(
            PurchaseProductDefinition product,
            PlatformCatalog platformCatalog,
            ICollection<PurchaseCatalogIssue> issues)
        {
            if (product.Overrides == null)
            {
                return;
            }

            var selectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < product.Overrides.Count; index++)
            {
                var target = product.Overrides[index];
                if (target == null)
                {
                    continue;
                }

                var selector = BuildSelector(target);
                if (!selectors.Add(selector))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        product.Id,
                            $"Duplicate target override selector '{selector}'."));
                }

                ValidatePlatformIds(product.Id, selector, target.PlatformIds, platformCatalog, issues);

                if (string.IsNullOrWhiteSpace(target.StoreProductId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Warning,
                        product.Id,
                        $"Target override '{selector}' has no store product ID."));
                }

                ValidateOverlappingOverride(product, target, index, issues);
            }
        }

        private static void ValidateOverlappingOverride(
            PurchaseProductDefinition product,
            PurchaseTargetOverride target,
            int targetIndex,
            ICollection<PurchaseCatalogIssue> issues)
        {
            for (var otherIndex = 0; otherIndex < targetIndex; otherIndex++)
            {
                var other = product.Overrides[otherIndex];
                if (other == null ||
                    other.Priority != target.Priority ||
                    !string.Equals(other.AdapterId, target.AdapterId, StringComparison.OrdinalIgnoreCase) ||
                    !Overlaps(other.PlatformIds, target.PlatformIds) ||
                    PurchasePlatformIdUtility.CountDistinct(other.PlatformIds) !=
                    PurchasePlatformIdUtility.CountDistinct(target.PlatformIds))
                {
                    continue;
                }

                issues.Add(new PurchaseCatalogIssue(
                    PurchaseCatalogIssueSeverity.Error,
                    product.Id,
                    $"Target overrides {otherIndex} and {targetIndex} overlap for adapter " +
                    $"'{target.AdapterId}' with the same priority."));
                return;
            }
        }

        private static string BuildSelector(PurchaseTargetOverride target)
        {
            var platformIds = target.PlatformIds == null
                ? string.Empty
                : string.Join(",", target.PlatformIds
                    .Select(PurchasePlatformIdUtility.Normalize)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
            return $"{target.AdapterId}|{platformIds}|{target.Priority}";
        }

        private static void ValidatePlatformIds(
            string productId,
            string selector,
            IReadOnlyList<string> platformIds,
            PlatformCatalog platformCatalog,
            ICollection<PurchaseCatalogIssue> issues)
        {
            if (platformIds == null || platformIds.Count == 0)
            {
                issues.Add(new PurchaseCatalogIssue(
                    PurchaseCatalogIssueSeverity.Error,
                    productId,
                    $"Target override '{selector}' has no platform IDs."));
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < platformIds.Count; index++)
            {
                var platformId = PurchasePlatformIdUtility.Normalize(platformIds[index]);
                if (platformId.Length == 0)
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        productId,
                        $"Target override '{selector}' has an empty platform ID at index {index}."));
                    continue;
                }

                if (!seen.Add(platformId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        productId,
                        $"Target override '{selector}' repeats platform ID '{platformId}'."));
                }

                if (platformCatalog != null && !IsKnownPlatformId(platformCatalog, platformId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        productId,
                        $"Target override '{selector}' uses unknown platform ID '{platformId}'."));
                }
            }
        }

        private static bool Overlaps(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index++)
            {
                if (PurchasePlatformIdUtility.Matches(right, left[index]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsKnownPlatformId(PlatformCatalog platformCatalog, string platformId)
        {
            if (platformCatalog?.Entries == null)
            {
                return false;
            }

            for (var index = 0; index < platformCatalog.Entries.Count; index++)
            {
                var entry = platformCatalog.Entries[index];
                if (entry != null && string.Equals(
                        PurchasePlatformIdUtility.Normalize(entry.PlatformId),
                        PurchasePlatformIdUtility.Normalize(platformId),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

