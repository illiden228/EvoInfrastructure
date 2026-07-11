using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseCatalogValidator
    {
        public static IReadOnlyList<PurchaseCatalogIssue> Validate(PurchaseCatalogConfig catalog)
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

                ValidateOverrides(product, issues);
            }

            return issues;
        }

        private static void ValidateOverrides(
            PurchaseProductDefinition product,
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

                var selector = $"{target.AdapterId}|{(int)target.Platforms}|{target.Priority}";
                if (!selectors.Add(selector))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        product.Id,
                            $"Duplicate target override selector '{selector}'."));
                }

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
                    (other.Platforms & target.Platforms) == PurchasePlatformMask.None ||
                    CountBits((int)other.Platforms) != CountBits((int)target.Platforms))
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

        private static int CountBits(int value)
        {
            var bits = unchecked((uint)value);
            var count = 0;
            while (bits != 0)
            {
                count += (int)(bits & 1u);
                bits >>= 1;
            }

            return count;
        }
    }
}

