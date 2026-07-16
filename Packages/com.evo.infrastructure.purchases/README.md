# Evo Infrastructure Purchases

Store-independent purchase catalog and fulfillment pipeline for Unity 2022.3+.

```csharp
builder.RegisterEvoFeatures(features =>
{
    features.UsePurchases();
    features.UseUnityIapPurchases(); // install an adapter package separately
});

builder.Register<MyPurchaseFulfillmentHandler>(Lifetime.Singleton)
    .As<IPurchaseFulfillmentHandler>();
```

Create `PurchaseCatalogConfig` and `PurchaseRoutingConfig` assets. A logical product owns its
rewards and fulfillment key; target overrides map it to adapter/platform store IDs. Store data
is authoritative for availability, localized text, price, and currency.

## Platform routing

Purchases uses `PlatformCatalog.CurrentPlatformId` from `com.evo.infrastructure.platform` as the
store/distribution platform. Configure every adapter binding and every SKU override with platform
IDs present in that catalog, for example `google_play`, `app_store`, or `rustore`. IDs are trimmed
and compared case-insensitively. Empty and unknown IDs are validation errors. A missing, empty, or
unknown current platform leaves the purchase service unavailable and never selects a fallback
adapter.

`PurchaseTargetOverride.AdapterId` is selected from the adapter IDs configured in project
`PurchaseRoutingConfig` assets. The empty `<any adapter>` option keeps an override adapter-agnostic.

## Fulfillment handlers

`FulfillmentKey` selects the `IPurchaseFulfillmentHandler` that grants and persists the purchased
content after the store reports success. Exactly one registered handler must return `true` from
`CanFulfill(key)`. Leave the field empty to use the logical product `Id` as the key. It is not a
store SKU and does not affect adapter routing.

`Application.platform` is not used for store routing. In the Unity Editor, only adapter bindings
marked **Editor Mock** are eligible, even when `CurrentPlatformId` is a real store such as
`google_play`. If no matching Editor Mock is configured, no store adapter is started.

## Breaking change in 0.5.16

`PurchasePlatformMask` routing was removed from `PurchaseAdapterBinding`, `PurchaseTargetOverride`,
`PurchaseCatalogResolver`, and `PurchaseService`. Existing serialized `platforms` mask values are
ignored and do not fall back to an OS mapping. This cannot be migrated automatically because an
Android mask does not distinguish Google Play from RuStore.

Before upgrading, replace every routing mask and target override mask with explicit `platformIds`.
For Editor simulation, create a binding for the mock adapter, assign the business platform IDs it
simulates, and enable **Editor Mock**. `PurchasePlatformMask` remains as an obsolete source symbol
for migration diagnostics and will be removed in a future major cleanup.

Fulfillment handlers must be idempotent by `AdapterId + TransactionId` and persist the reward
before returning success. The adapter confirms or consumes a transaction only after successful
fulfillment. Missing config, adapter, or handler completes initialization as unavailable and does
not block the loading pipeline.

## Purchase transaction contract

`PurchaseTransaction.ProductId` is the project's logical product ID, while `StoreProductId` is the
SKU used by the selected store. `TransactionId` is the adapter/store transaction identity used for
idempotent fulfillment and confirmation. `Receipt` preserves the store receipt, `PurchaseToken`
contains a verification token when the store exposes one, and `OrderId` is populated only when it
can be extracted reliably. `Price` and `CurrencyCode` are localized store metadata; `IsRestored`
distinguishes fetched entitlements from a newly completed purchase.

The purchases package never sends analytics. After successful fulfillment, the project may map a
transaction to `PurchaseEventData` and call `IAnalyticsService.TrackPurchase(...)`. Restored
transactions should only be reported when the project's analytics policy explicitly requires it.
