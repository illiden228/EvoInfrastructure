# Evo Firebase Analytics

Install the Firebase Unity SDK (`Firebase.App` and `Firebase.Analytics`) separately; it is not bundled with this package. The Evo Setup wizard detects both UPM assemblies and the official precompiled DLLs imported under `Assets`, then offers to enable `EVO_FIREBASE_ANALYTICS_SDK`. The SDK bridge asmdef is excluded otherwise, so projects remain compile-safe.

```csharp
features.UseAnalytics();
features.UseFirebaseAnalytics();
```

Create `FirebaseAnalyticsAdapterConfig`, add it to `AnalyticsAdapterCatalog`, and rebuild `ScriptableConfigCatalog` from `EvoTools/Config Maintenance/Rebuild Config Catalogs`.
