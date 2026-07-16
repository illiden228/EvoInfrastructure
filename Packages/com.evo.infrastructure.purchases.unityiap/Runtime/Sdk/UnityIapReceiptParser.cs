using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    internal static class UnityIapReceiptParser
    {
        internal static bool TryGetGooglePurchaseData(
            string receipt,
            out string purchaseToken,
            out string orderId,
            out string transactionId)
        {
            purchaseToken = null;
            orderId = null;
            transactionId = null;
            if (string.IsNullOrWhiteSpace(receipt))
                return false;

            try
            {
                var unified = JsonUtility.FromJson<UnifiedReceiptData>(receipt);
                var isGoogle = string.Equals(unified?.Store, GooglePlay.Name, StringComparison.OrdinalIgnoreCase);
                if (unified != null)
                    transactionId = NullIfWhiteSpace(unified.TransactionID);

                var platformReceipt = NullIfWhiteSpace(unified?.Payload) ?? receipt;
                if (TryParsePlatformReceipt(platformReceipt, out purchaseToken, out orderId))
                {
                    if (isGoogle || !string.IsNullOrWhiteSpace(purchaseToken))
                        return true;

                    purchaseToken = null;
                    orderId = null;
                }

                return isGoogle;
            }
            catch (Exception)
            {
                purchaseToken = null;
                orderId = null;
                transactionId = null;
                return false;
            }
        }

        private static bool TryParsePlatformReceipt(string receipt, out string purchaseToken, out string orderId)
        {
            purchaseToken = null;
            orderId = null;
            if (string.IsNullOrWhiteSpace(receipt))
                return false;

            var wrapper = JsonUtility.FromJson<GoogleReceiptWrapper>(receipt);
            var purchaseJson = NullIfWhiteSpace(wrapper?.json) ?? receipt;
            var purchase = JsonUtility.FromJson<GooglePurchaseData>(purchaseJson);
            purchaseToken = NullIfWhiteSpace(purchase?.purchaseToken);
            orderId = NullIfWhiteSpace(purchase?.orderId);
            return purchaseToken != null || orderId != null;
        }

        private static string NullIfWhiteSpace(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        [Serializable]
        private sealed class UnifiedReceiptData
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        [Serializable]
        private sealed class GoogleReceiptWrapper
        {
            public string json;
        }

        [Serializable]
        private sealed class GooglePurchaseData
        {
            public string orderId;
            public string purchaseToken;
        }
    }
}
