# Evo AppMetrica Analytics

Install `io.appmetrica.analytics` separately. The package uses an asmdef version define and compiles its SDK bridge only while the vendor package/assembly is available; AppMetrica is not bundled.
The bridge registers its strongly typed adapter at Unity startup and `AlwaysLinkAssembly` preserves the
conditional SDK bridge for IL2CPP. The adapter waits for AppMetrica's `OnActivation` callback (up to 10 seconds);
a missing config, activation failure, or timeout disables only this adapter and never blocks application startup.

```csharp
features.UseAnalytics();
features.UseAppMetricaAnalytics();
```

Create `AppMetricaAnalyticsAdapterConfig`, add it to `AnalyticsAdapterCatalog`, then rebuild the config catalog.
