# Evo Adjust Analytics

Install `com.adjust.sdk` separately. The SDK bridge references `AdjustSdk.Scripts` and is guarded by the package version define; the Adjust SDK is not bundled.
The bridge registers its strongly typed adapter at Unity startup; it does not use runtime type-name lookup.
`AlwaysLinkAssembly` preserves the conditional SDK bridge for IL2CPP builds.

```csharp
features.UseAnalytics();
features.UseAdjustAnalytics();
```

Create `AdjustAnalyticsAdapterConfig`, add it to `AnalyticsAdapterCatalog`, then rebuild the config catalog. Missing app/purchase tokens disable only the affected adapter path and never block analytics initialization.

Adjust tracking is disabled in the Unity Editor by default, including for Production configs. Enable
`Allow Editor Tracking` explicitly only when an Editor session is expected to send real SDK events.
Choose `Sandbox` through the config asset for test traffic; no production code change is required.

On Android, purchases with both a store product ID and purchase token use
`VerifyAndTrackPlayStorePurchase`. If verification inputs are missing, the adapter logs one warning
and sends one ordinary unverified revenue event so analytics is not lost. iOS purchases continue to
use `VerifyAndTrackAppStorePurchase`. A single dispatch selects exactly one of verification or
fallback tracking.

The game project owns the purchase-to-analytics handoff. Call
`IAnalyticsService.TrackPurchase(PurchaseEventData)` only after successful fulfillment, pass the
store SKU as `ItemId` when store verification requires it, and do not report restored transactions
unless that is an explicit product analytics decision.
