# Evo Infrastructure RuStore Purchases

RuStore Pay adapter for `com.evo.infrastructure.purchases`. The package contains no vendor binaries and does not use the deprecated `RuStoreBillingClient` API.

## Compatibility and SDK installation

Install the official UPM tarballs `ru.rustore.core` and `ru.rustore.pay` separately from the [RuStore Pay Unity releases](https://gitflic.ru/project/rustore/unity-rustore-pay-sdk/release). The adapter targets the RuStore Pay 10.2+ API and is enabled by the `ru.rustore.pay` package version define.

The Evo packages support Unity 2022.3. RuStore Pay 10.5.0 declares Unity 6000.0 as its minimum, so it must not be installed into a Unity 2022.3 project. Select a Pay SDK release that explicitly supports the project's Unity version (verify the SDK `package.json` before importing). Do not fall back to the deprecated BillingClient SDK.

RuStore currently requires an Android player, minimum API level 24, target API level 34, custom Gradle/manifest templates, and an External Dependency Manager force resolve. Follow the selected SDK release documentation because these requirements may change.

## Registration

Create `RuStorePurchaseAdapterConfig`, add it to the project `ConfigCatalog`, and register core before the adapter:

```csharp
features.UsePurchases();
features.UseRuStorePurchases();
```

Add an override with adapter ID `rustore` and the product ID from RuStore Console to every product intended for RuStore. Store prices, titles, descriptions, currency, and availability come from RuStore; grants and fulfillment remain project-owned.

## Payment flow

- `OneStep` charges immediately and needs no capture after fulfillment.
- `PreferTwoStep` asks RuStore for two-stage payment but the selected payment method may fall back to one-stage.
- `RequireTwoStep` exposes only payment methods supporting a guaranteed hold.

For a two-stage purchase, the adapter returns the pending transaction, core fulfills and persists its grants, and only then calls `ConfirmTwoStepPurchase`. Restored `PAID` transactions follow the same path. `invoiceId` is stored in `PurchaseTransaction.Receipt` for optional server validation; `purchaseId` is the idempotency transaction ID.

Missing config, an incompatible/missing SDK, a non-Android player, unavailable payments, and an empty store catalog finish initialization as unavailable rather than blocking startup.
