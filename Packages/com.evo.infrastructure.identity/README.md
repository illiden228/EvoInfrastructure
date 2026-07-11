# Evo Infrastructure Identity

Platform-neutral player identity. Register core before one or more providers:
```csharp
features.UseIdentity();
features.UseYandexIdentity(); // or UseCrazyGamesIdentity()
```
Automatic authentication must never display an account dialog. Use `Interactive` only after an explicit player action.
