# Evo Infrastructure Google Play Games

Shared Android-only session for Google Play Games features. Install the official Google Play Games Plugin for Unity separately, complete its Android setup, and enable `EVO_GOOGLE_PLAY_GAMES_SDK`.

The SDK is intentionally not vendored or referenced as a local package. Without it, projects remain compile-safe and Google features stay unavailable.

```csharp
features.UseGooglePlayGames(new GooglePlayGamesOptions
{
    authenticationTimeoutMs = 15000
});
```

Registration starts one bounded automatic authentication attempt through the VContainer async-start lifecycle. Interactive authentication is only requested explicitly through the identity service.
