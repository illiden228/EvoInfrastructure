using System;
using AdjustSdk;
using AdjustApi = AdjustSdk.Adjust;

namespace Evo.Infrastructure.Services.Analytics.Adjust
{
    internal interface IAdjustSdkFacade
    {
        void InitSdk(AdjustConfig config);
        void TrackEvent(AdjustEvent adjustEvent);
        void TrackAdRevenue(AdjustAdRevenue adRevenue);
        void VerifyAndTrackAppStorePurchase(
            AdjustEvent adjustEvent,
            Action<AdjustPurchaseVerificationResult> callback);
        void VerifyAndTrackPlayStorePurchase(
            AdjustEvent adjustEvent,
            Action<AdjustPurchaseVerificationResult> callback);
    }

    internal sealed class AdjustSdkFacade : IAdjustSdkFacade
    {
        public void InitSdk(AdjustConfig config) => AdjustApi.InitSdk(config);
        public void TrackEvent(AdjustEvent adjustEvent) => AdjustApi.TrackEvent(adjustEvent);
        public void TrackAdRevenue(AdjustAdRevenue adRevenue) => AdjustApi.TrackAdRevenue(adRevenue);

        public void VerifyAndTrackAppStorePurchase(
            AdjustEvent adjustEvent,
            Action<AdjustPurchaseVerificationResult> callback) =>
            AdjustApi.VerifyAndTrackAppStorePurchase(adjustEvent, callback);

        public void VerifyAndTrackPlayStorePurchase(
            AdjustEvent adjustEvent,
            Action<AdjustPurchaseVerificationResult> callback) =>
            AdjustApi.VerifyAndTrackPlayStorePurchase(adjustEvent, callback);
    }
}
