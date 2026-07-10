# Evo AppLovin MAX Ads

Install `com.applovin.mediation.ads` from the AppLovin scoped registry separately. The SDK bridge references `MaxSdk.Scripts` and is guarded by a package version define; MAX is not bundled.

```csharp
features.UseAds();
features.UseAppLovinAds();
```

Create `AppLovinAdsAdapterConfig`, add it to `AdsAdapterCatalog`, then rebuild the config catalog. Missing SDK key or ad unit ids leaves the factory/ads service operational while the adapter reports itself unavailable.
