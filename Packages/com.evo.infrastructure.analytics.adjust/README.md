# Evo Adjust Analytics

Install `com.adjust.sdk` separately. The SDK bridge references `AdjustSdk.Scripts` and is guarded by the package version define; the Adjust SDK is not bundled.

```csharp
features.UseAnalytics();
features.UseAdjustAnalytics();
```

Create `AdjustAnalyticsAdapterConfig`, add it to `AnalyticsAdapterCatalog`, then rebuild the config catalog. Missing app/purchase tokens disable only the affected adapter path and never block analytics initialization.
