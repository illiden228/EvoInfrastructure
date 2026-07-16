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

Unity IAP 5.0.4 maps Google Play's unique purchase token to `Order.Info.TransactionID`.
The adapter therefore preserves that value as `PurchaseTransaction.TransactionId` and also exposes
it as `PurchaseToken` for Google Play verification. When available, the Google Play `orderId` is
read from the nested unified receipt payload. Receipt parsing is best-effort and malformed or
unknown formats never fail the purchase flow.

Restored non-consumables preserve real store transaction, receipt, token and order fields whenever
Unity IAP returns them. If a confirmed entitlement has no transaction identity, it falls back to
`unity-iap-entitlement:<storeProductId>`. Other missing transaction IDs fall back to a stable
receipt-derived recovery ID. Subscription expiry, renewal and receipt changes must be reconciled by
the project's fulfillment/backend validation policy; a subscription must not be modeled as an
unverified consumable reward.

Unity IAP Codeless auto-initialization must be disabled when this adapter is
used, because the adapter owns the `StoreController` lifecycle.

The package requires Unity IAP 5.0 or newer and registers its adapter through a
direct assembly reference so it remains available in IL2CPP builds.
