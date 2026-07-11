using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseCatalogValidator
    {
        public static IReadOnlyList<PurchaseCatalogIssue> Validate(PurchaseCatalogConfig catalog)
        {
            var issues = new List<PurchaseCatalogIssue>();
            if (catalog?.Offers == null)
            {
                return issues;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var defaultStoreIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var offer in catalog.Offers)
            {
                if (offer == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(offer.Id))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        null,
                        "Offer has an empty logical ID."));
                    continue;
                }

                if (!ids.Add(offer.Id))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        offer.Id,
                        "Logical ID is duplicated."));
                }

                if (string.IsNullOrWhiteSpace(offer.FulfillmentKey))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        offer.Id,
                        "Fulfillment key is empty."));
                }

                if (string.IsNullOrWhiteSpace(offer.DefaultStoreProductId) &&
                    (offer.Overrides == null || offer.Overrides.Count == 0))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Warning,
                        offer.Id,
                        "No default or target-specific store product ID is configured."));
                }

                if (!string.IsNullOrWhiteSpace(offer.DefaultStoreProductId) &&
                    !defaultStoreIds.Add(offer.DefaultStoreProductId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        offer.Id,
                        $"Default store product ID '{offer.DefaultStoreProductId}' is mapped by multiple offers."));
                }

                ValidateOverrides(offer, issues);
            }

            return issues;
        }

        private static void ValidateOverrides(
            PurchaseOfferDefinition offer,
            ICollection<PurchaseCatalogIssue> issues)
        {
            if (offer.Overrides == null)
            {
                return;
            }

            var selectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < offer.Overrides.Count; index++)
            {
                var target = offer.Overrides[index];
                if (target == null)
                {
                    continue;
                }

                var selector = $"{target.AdapterId}|{(int)target.Platforms}|{target.Priority}";
                if (!selectors.Add(selector))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Error,
                        offer.Id,
                            $"Duplicate target override selector '{selector}'."));
                }

                if (string.IsNullOrWhiteSpace(target.StoreProductId))
                {
                    issues.Add(new PurchaseCatalogIssue(
                        PurchaseCatalogIssueSeverity.Warning,
                        offer.Id,
                        $"Target override '{selector}' has no store product ID."));
                }

                ValidateOverlappingOverride(offer, target, index, issues);
            }
        }

        private static void ValidateOverlappingOverride(
            PurchaseOfferDefinition offer,
            PurchaseTargetOverride target,
            int targetIndex,
            ICollection<PurchaseCatalogIssue> issues)
        {
            for (var otherIndex = 0; otherIndex < targetIndex; otherIndex++)
            {
                var other = offer.Overrides[otherIndex];
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
                    offer.Id,
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

