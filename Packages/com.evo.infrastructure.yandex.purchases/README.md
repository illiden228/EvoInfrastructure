# Evo Infrastructure Yandex Purchases

PluginYG2 Payments adapter for `com.evo.infrastructure.purchases`.

## External SDK strategy

The package does not embed PluginYG2. Import PluginYG2 and enable its **Payments** module in the PluginYG2 project settings. Yandex Games also requires cloud persistence through the PluginYG2 Storage or PlayerStats module. Without the `Payments_yg` scripting define the package remains compile-safe; the feature registers an unavailable adapter that finishes initialization without blocking startup.

Configure product mappings in `PurchaseCatalogConfig`. The logical offer ID and grants belong to the game catalog; the Yandex override supplies the product ID configured in the Yandex Games dashboard. Titles, descriptions, images, prices and currencies are taken from `YG2.purchases` at runtime.

```csharp
using Evo.Infrastructure.Services.Purchases;
using Evo.Infrastructure.Services.Purchases.Yandex;

builder.RegisterEvoFeatures(features =>
{
    features.UsePurchases();
    features.UseYandexPurchases();
});
```

## Recovery and consumption

PluginYG2 reports purchases by product ID and does not expose a unique transaction ID through its public Payments API. The adapter therefore treats Yandex purchases as pending by product ID. A successful purchase is first passed to the core fulfillment flow and only then consumed with `YG2.ConsumePurchaseByID(id, false)`. Fulfillment must be idempotent and persist its journal before returning success. The local recovery marker is removed only after `YG2.purchases` reports the product as consumed; invoking the SDK method alone is not considered confirmation.

At startup the adapter reads `YG2.purchases`; only entries whose `consumed` flag is false and which have a locally recorded pending purchase attempt are returned from restore. Do not add PluginYG2's `ConsumePurchasesYG` component alongside this adapter, because it may consume purchases before the game's fulfillment succeeds.

For Editor simulation, fill the Payments module's **Purchases** array as described in the PluginYG2 documentation.
