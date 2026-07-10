# Evo Firebase Analytics

Install the Firebase Unity SDK (`Firebase.App` and `Firebase.Analytics`) separately; it is not bundled with this package. Because the official Unity SDK can be imported as Assets rather than UPM, add `EVO_FIREBASE_ANALYTICS_SDK` to Scripting Define Symbols after the two assemblies are present. The SDK bridge asmdef is excluded otherwise, so projects remain compile-safe.

```csharp
features.UseAnalytics();
features.UseFirebaseAnalytics();
```

Create `FirebaseAnalyticsAdapterConfig`, add it to `AnalyticsAdapterCatalog`, and rebuild `ScriptableConfigCatalog` from `EvoTools/Configs/Rebuild Config Catalogs`.
