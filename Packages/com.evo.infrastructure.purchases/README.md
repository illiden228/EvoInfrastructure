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

Fulfillment handlers must be idempotent by `AdapterId + TransactionId` and persist the reward
before returning success. The adapter confirms or consumes a transaction only after successful
fulfillment. Missing config, adapter, or handler completes initialization as unavailable and does
not block the loading pipeline.
