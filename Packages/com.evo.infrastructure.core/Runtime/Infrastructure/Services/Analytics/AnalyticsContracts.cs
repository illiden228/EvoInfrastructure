using System;
using System.Collections.Generic;

namespace _Project.Scripts.Infrastructure.Services.Analytics
{
    public enum AnalyticsEventType
    {
        Custom = 0,
        Purchase = 1,
        Ad = 2
    }

    public enum AnalyticsRuntimePlatform
    {
        Any = 0,
        Android = 1,
        IOS = 2,
        WebGL = 3,
        Standalone = 4,
        Editor = 5
    }

    public readonly struct PurchaseEventData
    {
        public readonly string EventKey;
        public readonly string Currency;
        public readonly decimal Revenue;
        public readonly string TransactionId;
        public readonly string ItemId;
        public readonly string PurchaseToken;
        public readonly string ReceiptJson;

        public PurchaseEventData(
            string eventKey,
            string currency,
            decimal revenue,
            string transactionId,
            string itemId,
            string purchaseToken = null,
            string receiptJson = null)
        {
            EventKey = eventKey;
            Currency = currency;
            Revenue = revenue;
            TransactionId = transactionId;
            ItemId = itemId;
            PurchaseToken = purchaseToken;
            ReceiptJson = receiptJson;
        }
    }

    public readonly struct AdEventData
    {
        public readonly string EventKey;
        public readonly string Platform;
        public readonly string NetworkName;
        public readonly string UnitId;
        public readonly string Format;
        public readonly string Placement;
        public readonly string NetworkPlacement;
        public readonly string Currency;
        public readonly double Revenue;
        public readonly string CountryCode;

        public AdEventData(
            string eventKey,
            string platform,
            string networkName,
            string unitId,
            string format,
            string placement,
            string networkPlacement,
            string currency,
            double revenue,
            string countryCode)
        {
            EventKey = eventKey;
            Platform = platform;
            NetworkName = networkName;
            UnitId = unitId;
            Format = format;
            Placement = placement;
            NetworkPlacement = networkPlacement;
            Currency = currency;
            Revenue = revenue;
            CountryCode = countryCode;
        }
    }

    public readonly struct AnalyticsDispatchEvent
    {
        public readonly AnalyticsEventType EventType;
        public readonly string EventKey;
        public readonly IReadOnlyDictionary<string, object> Parameters;
        public readonly PurchaseEventData PurchaseEventData;
        public readonly AdEventData AdEventData;

        public AnalyticsDispatchEvent(string eventKey, IReadOnlyDictionary<string, object> parameters = null)
        {
            EventType = AnalyticsEventType.Custom;
            EventKey = eventKey;
            Parameters = parameters;
            PurchaseEventData = default;
            AdEventData = default;
        }

        public AnalyticsDispatchEvent(PurchaseEventData purchaseEventData, IReadOnlyDictionary<string, object> parameters = null)
        {
            EventType = AnalyticsEventType.Purchase;
            EventKey = purchaseEventData.EventKey;
            Parameters = parameters;
            PurchaseEventData = purchaseEventData;
            AdEventData = default;
        }

        public AnalyticsDispatchEvent(AdEventData adEventData, IReadOnlyDictionary<string, object> parameters = null)
        {
            EventType = AnalyticsEventType.Ad;
            EventKey = adEventData.EventKey;
            Parameters = parameters;
            PurchaseEventData = default;
            AdEventData = adEventData;
        }
    }
}
