# Evo Unity IAP Purchases

Unity IAP v5 adapter for `com.evo.infrastructure.purchases`. The official
`com.unity.purchasing` package is referenced as a UPM dependency; no SDK source
or binary is vendored.

```csharp
features.UsePurchases();
features.UseUnityIapPurchases();
```

Configure store product IDs as `unity-iap` overrides in `PurchaseCatalogConfig`, using explicit
`PlatformCatalog` IDs such as `google_play` and `app_store` for each store SKU.
The adapter connects to the store, fetches configured products and pending
purchases, and only confirms a transaction after the core fulfillment handler
has granted and persisted its rewards.

Restored non-consumables use a stable entitlement identity
(`unity-iap-entitlement:<storeProductId>`), because confirmed orders may omit a
transaction ID. Subscriptions retain their store transaction identity, falling
back to a receipt-derived recovery ID. Subscription expiry, renewal and receipt
changes must be reconciled by the project's fulfillment/backend validation
policy; a subscription must not be modeled as an unverified consumable reward.

Unity IAP Codeless auto-initialization must be disabled when this adapter is
used, because the adapter owns the `StoreController` lifecycle.

The SDK bridge is guarded by an asmdef version define for Unity IAP 5.0 or
newer. If the SDK assembly is unavailable, registration remains compile-safe and
the purchase service degrades to unavailable instead of blocking startup.
