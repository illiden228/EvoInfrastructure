# Evo Infrastructure Achievements

Platform-neutral achievement routing. Register the core before one or more platform adapters:

```csharp
builder.RegisterEvoFeatures(features => features.UseAchievements());
```

Gameplay uses logical achievement keys; platform packages own the mapping to store identifiers.
