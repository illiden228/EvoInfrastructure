# Google Play Games Save

```csharp
features.UseSave();
features.UseGooglePlayGames();
features.UseGooglePlayGamesSave(new GooglePlayGamesSaveOptions { slotName = "main" });
```

Saved Games must be enabled in Play Console and Android Setup must be completed with the current official Google Play Games Plugin for Unity release. This package targets the Google Play Games Services v2 API. It does not use the legacy `PlayGamesClientConfiguration.EnableSavedGames()` flow: Saved Games are accessed through `PlayGamesPlatform.Instance.SavedGame` after normal platform authentication.

The backend serializes complete open/read and open/commit cycles per slot, resolves snapshot conflicts using the most recently saved snapshot, and never prevents local save backends from operating.
