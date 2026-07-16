using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Purchasing;

namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    internal static class UnityIapOrderMapper
    {
        internal static PurchaseTransaction ToTransaction(
            Order order,
            bool restored,
            IReadOnlyDictionary<string, string> productByStoreId,
            bool useEntitlementFallback = false)
        {
            var product = FirstProduct(order);
            var storeProductId = product?.definition?.storeSpecificId ?? string.Empty;
            string productId = null;
            productByStoreId?.TryGetValue(storeProductId, out productId);
            var metadata = product?.metadata;
            var receipt = order?.Info?.Receipt;
            var isGoogleReceipt = UnityIapReceiptParser.TryGetGooglePurchaseData(
                receipt,
                out var receiptPurchaseToken,
                out var orderId,
                out var receiptTransactionId);
            var isGoogleOrder = order?.Info?.Google != null || isGoogleReceipt;
            var transactionId = ResolveTransactionId(
                order,
                storeProductId,
                receiptTransactionId,
                receiptPurchaseToken,
                isGoogleOrder,
                useEntitlementFallback);

            // Unity IAP 5.0.4 deliberately uses Google Play's purchaseToken as Order.Info.TransactionID.
            // Keep TransactionId's store contract intact and copy the same value into PurchaseToken only
            // for an order positively identified as Google Play.
            var purchaseToken = receiptPurchaseToken;
            if (string.IsNullOrWhiteSpace(purchaseToken) && isGoogleOrder)
                purchaseToken = order?.Info?.TransactionID;

            return new PurchaseTransaction(
                transactionId,
                productId ?? string.Empty,
                storeProductId,
                UnityIapPurchaseAdapter.Id,
                receipt,
                purchaseToken,
                orderId,
                metadata?.localizedPrice ?? 0,
                metadata?.isoCurrencyCode,
                isRestored: restored);
        }

        internal static Product FirstProduct(Order order) =>
            order?.CartOrdered?.Items()?.FirstOrDefault()?.Product;

        private static string ResolveTransactionId(
            Order order,
            string storeProductId,
            string receiptTransactionId,
            string receiptPurchaseToken,
            bool isGoogleOrder,
            bool useEntitlementFallback)
        {
            var transactionId = order?.Info?.TransactionID;
            if (!string.IsNullOrWhiteSpace(transactionId))
                return transactionId;
            if (!string.IsNullOrWhiteSpace(receiptTransactionId))
                return receiptTransactionId;
            if (isGoogleOrder && !string.IsNullOrWhiteSpace(receiptPurchaseToken))
                return receiptPurchaseToken;
            if (useEntitlementFallback && !string.IsNullOrWhiteSpace(storeProductId))
                return "unity-iap-entitlement:" + storeProductId;

            var receipt = order?.Info?.Receipt;
            if (string.IsNullOrWhiteSpace(receipt) || string.IsNullOrWhiteSpace(storeProductId))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(storeProductId + "\n" + receipt);
            var hash = sha256.ComputeHash(bytes);
            return "unity-iap-recovery-" + BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
