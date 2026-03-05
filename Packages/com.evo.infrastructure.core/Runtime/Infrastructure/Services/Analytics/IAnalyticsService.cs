using System.Collections.Generic;

namespace _Project.Scripts.Infrastructure.Services.Analytics
{
    public interface IAnalyticsService
    {
        void TrackCustom(string eventKey, IReadOnlyDictionary<string, object> parameters = null);
        void TrackPurchase(PurchaseEventData purchaseEventData, IReadOnlyDictionary<string, object> parameters = null);
        void TrackAd(AdEventData adEventData, IReadOnlyDictionary<string, object> parameters = null);
    }
}
