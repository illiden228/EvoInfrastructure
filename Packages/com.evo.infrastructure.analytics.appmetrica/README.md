# Evo AppMetrica Analytics

Install `io.appmetrica.analytics` separately. The package uses an asmdef version define and compiles its SDK bridge only while the vendor package/assembly is available; AppMetrica is not bundled.

```csharp
features.UseAnalytics();
features.UseAppMetricaAnalytics();
```

Create `AppMetricaAnalyticsAdapterConfig`, add it to `AnalyticsAdapterCatalog`, then rebuild the config catalog.
