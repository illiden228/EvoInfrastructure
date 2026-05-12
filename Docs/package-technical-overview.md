# Evo Infrastructure: Technical Overview

## Goal

Evo Infrastructure is a Unity package set for fast game prototyping and repeated project bootstrap.

Primary goals:

- start a new game quickly;
- reuse stable runtime systems across projects;
- keep project-specific code editable in `Assets/_Project`;
- allow package internals to evolve without breaking every game;
- support platform modules such as Yandex without forcing them into every project.

## Recommended Package Split

### `com.evo.infrastructure.core`

Editor-only bootstrap package.

Responsibilities:

- setup wizard;
- module selection;
- dependency checks;
- package installation;
- scaffold generation into `Assets/_Project`;
- project diagnostics;
- generated code update tools.

It should not contain runtime gameplay services.

### `com.evo.infrastructure.runtime`

Foundation runtime package.

Responsibilities:

- loading orchestration;
- scene loading;
- config service;
- resource loading/provider abstraction;
- UI runtime base;
- localization wrapper;
- platform info abstraction;
- debug/logging abstraction;
- common editor tooling for configs/catalogs.

This package should use package namespaces such as `Evo.Infrastructure.*`, not `_Project.Scripts.*`.

### Feature Packages

Can be created gradually.

Examples:

- `com.evo.infrastructure.save`
- `com.evo.infrastructure.ads`
- `com.evo.infrastructure.analytics`
- `com.evo.infrastructure.inventory`
- `com.evo.infrastructure.stats`
- `com.evo.infrastructure.economy`
- `com.evo.infrastructure.store`

For the current prototype, these can temporarily stay inside `runtime` or one larger `gameplay` package. Split only when the module becomes stable enough.

### Platform Packages

Example:

- `com.evo.infrastructure.yandex`

Responsibilities:

- Yandex ads adapter;
- Yandex analytics adapter;
- Yandex leaderboard adapter;
- Yandex cloud save backend;
- Yandex platform info provider;
- Yandex store adapter if needed.

Platform package depends on feature abstractions, not the other way around.

## Dependency Direction

Correct direction:

```text
Game Project
  -> Platform Packages
  -> Feature Packages
  -> Runtime Foundation
```

Incorrect direction:

```text
Runtime -> Yandex
Save -> Yandex
Inventory -> Yandex
```

Yandex should implement interfaces from save/ads/analytics/etc.

## Unity Connection Modes

### For Game Development

Use Git tags through Unity Package Manager.

Example:

```json
{
  "dependencies": {
    "com.evo.infrastructure.core": "https://github.com/illiden228/EvoInfrastructure.git?path=Packages/com.evo.infrastructure.core#v0.4.0"
  }
}
```

Then open:

```text
EvoTools/Setup
```

The setup wizard installs selected modules and generates project scaffold.

### For Package Development

Use local package paths.

Example:

```json
{
  "dependencies": {
    "com.evo.infrastructure.runtime": "file:D:/Repos/EvoInfrastructure/Packages/com.evo.infrastructure.runtime",
    "com.evo.infrastructure.core": "file:D:/Repos/EvoInfrastructure/Packages/com.evo.infrastructure.core"
  }
}
```

This makes packages editable while testing them in a Unity project.

### For Emergency Project Fixes

Use embedded packages:

```text
UnityProject/Packages/com.evo.infrastructure.runtime
```

This is convenient for one-off fixes, but worse for shared package evolution.

## Recommended Workflow For Current Prototype

Because the current priority is fast delivery:

1. Keep the package split simple.
2. Use `core`, `runtime`, `yandex`.
3. Generate editable scaffold into `Assets/_Project`.
4. Do not over-split inventory/stats/economy yet.
5. Add clean extension points now, split modules later.

Minimum useful package set:

```text
com.evo.infrastructure.core
com.evo.infrastructure.runtime
com.evo.infrastructure.yandex
```

Optional later:

```text
com.evo.infrastructure.gameplay
com.evo.infrastructure.inventory
com.evo.infrastructure.stats
com.evo.infrastructure.economy
com.evo.infrastructure.store
```

## Project Scaffold

Generated into:

```text
Assets/_Project
```

Typical scaffold:

```text
Assets/_Project/Scripts/Runtime/EntryPoint/RuntimeProjectLifetimeScope.cs
Assets/_Project/Scripts/Runtime/EntryPoint/RuntimeEntryPoint.cs
Assets/_Project/Scripts/Runtime/Loading/ProjectStartupStepProvider.cs
Assets/_Project/Scripts/Runtime/Loading/ProjectGameplayStepProvider.cs
Assets/_Project/Scripts/Runtime/Config/ProjectConfig.cs
Assets/_Project/Scripts/Runtime/Save/GameSaveData.cs
Assets/_Project/Configs/ProjectConfig.asset
Assets/_Project/Configs/SceneCatalog.asset
Assets/_Project/Scenes/EntryPointScene.unity
Assets/_Project/Scenes/LoadingScene.unity
Assets/_Project/Scenes/MainMenuScene.unity
```

Scaffold is project-owned code. Developers may edit it.

Package code should be treated as reusable library code.

## Runtime Bootstrap

Startup sequence:

```text
EntryPointScene
  -> RuntimeProjectLifetimeScope
  -> VContainer registrations
  -> RuntimeEntryPoint
  -> ILoadingService.LoadStartupAsync()
  -> Loading pipeline
  -> Startup scene
```

`RuntimeEntryPoint` should stay thin:

```csharp
public sealed class RuntimeEntryPoint : IAsyncStartable
{
    private readonly ILoadingService _loading;

    public RuntimeEntryPoint(ILoadingService loading)
    {
        _loading = loading;
    }

    public UniTask StartAsync(CancellationToken token)
    {
        return _loading.LoadStartupAsync(token);
    }
}
```

## Loading System

Main abstractions:

```text
ILoadingService
LoadingRequest
LoadingContext
ILoadingStepProvider
LoadingStepCollection
LoadingStepDescriptor
ILoadingStep
LoadingRunner
SceneCatalog
SceneDefinition
```

Loading service does not hardcode project rules. It asks providers to collect steps.

Example request:

```csharp
await loading.LoadSceneAsync(new SceneId("training"));
```

Internally:

```text
SceneCatalog finds training scene
Request receives flow gameplay-load
Request receives tags gameplay, training
Providers add matching steps
Runner executes sorted steps
```

## Loading Steps And DI

Steps should be created by DI, not manually with `new`.

Registration example:

```csharp
builder.Register<ILoadingService, LoadingService>(Lifetime.Singleton);
builder.Register<LoadingPipelineBuilder>(Lifetime.Singleton);
builder.Register<LoadingRunner>(Lifetime.Singleton);

builder.Register<ILoadingStepProvider, ProjectStartupStepProvider>(Lifetime.Singleton);
builder.Register<ILoadingStepProvider, ProjectGameplayStepProvider>(Lifetime.Singleton);
builder.Register<ILoadingStepProvider, ProjectTrainingStepProvider>(Lifetime.Singleton);

builder.Register<FadeInStep>(Lifetime.Transient);
builder.Register<LoadTargetSceneStep>(Lifetime.Transient);
builder.Register<PreloadGameplayAssetsStep>(Lifetime.Transient);
builder.Register<LoadTrainingPresetStep>(Lifetime.Transient);
```

Provider adds descriptors:

```csharp
steps.Add<LoadTrainingPresetStep>(order: 700, weight: 1f);
```

Runner resolves the actual step from `IObjectResolver`:

```csharp
var step = (ILoadingStep)_resolver.Resolve(descriptor.StepType);
await step.ExecuteAsync(context, progress, token);
```

Because VContainer creates the step, every step may receive working services:

```csharp
public sealed class PreloadGameplayAssetsStep : ILoadingStep
{
    private readonly IResourceProviderService _resources;
    private readonly IGameplayLoadState _state;

    public PreloadGameplayAssetsStep(
        IResourceProviderService resources,
        IGameplayLoadState state)
    {
        _resources = resources;
        _state = state;
    }

    public async UniTask ExecuteAsync(
        LoadingContext context,
        ILoadingProgress progress,
        CancellationToken token)
    {
        await _resources.LoadAsync(_state.CommonGameplayAssets, token);
    }
}
```

## Avoiding String And Object Overuse

Avoid using `Dictionary<string, object>` as the main loading API.

Preferred:

```text
SceneId
LoadingFlowId
LoadingTag
LoadingTagSet
ILoadingPayload
typed runtime state services
```

Use strings only in authoring/config assets if needed.

In code prefer wrappers:

```csharp
public readonly struct SceneId
{
    public readonly string Value;
}
```

For performance-sensitive code this can later become:

```csharp
public readonly struct SceneId
{
    public readonly int Hash;
}
```

Payload examples:

```csharp
public interface ILoadingPayload { }

public sealed class TrainingPayload : ILoadingPayload
{
    public int BotDifficulty;
}

public sealed class MatchRestartPayload : ILoadingPayload
{
    public string MatchPresetId;
}
```

Use payload casts only at flow boundaries.

## Feature Modules

### Save

Responsibility:

- save/load orchestration;
- backend selection;
- migration;
- conflict resolution;
- local save backend.

Project owns concrete save model:

```csharp
public sealed class GameSaveData
{
    public int SchemaVersion;
    public PlayerProgressSave Progress;
    public InventorySaveData Inventory;
    public EconomySaveData Economy;
}
```

Yandex package provides a backend, not the whole save model.

### Ads

Responsibility:

- common `IAdsService`;
- adapter selection;
- fallback;
- timeout;
- analytics hooks.

Yandex provides `YandexGamesAdsAdapter`.

### Analytics

Responsibility:

- common `IAnalyticsService`;
- event mapping;
- adapter routing.

Project owns event catalog/keys.

### Inventory / Stats / Economy

For the current prototype, keep these in project code or a temporary `gameplay` package unless they are already stable.

Recommended future split:

```text
stats: modifiers, stat containers, formulas
inventory: items, slots, equipment, stacks
economy: currencies, prices, rewards
store: platform purchases
```

## Yandex Specific Notes

Do not keep `partial class SavesYG` inside the UPM package if Yandex plugin compiles in another assembly.

Generate it into the project instead:

```text
Assets/_Project/Scripts/Generated/Yandex/SavesYG.Evo.cs
```

This avoids assembly/partial class issues.

## Testing Strategy

Yes, create a separate empty Unity project for package tests.

Recommended projects:

```text
EvoInfrastructure
EvoInfrastructureSandbox
EvoPrototypeGame
```

### `EvoInfrastructureSandbox`

Purpose:

- test package installation;
- test setup wizard;
- test generated scaffold;
- test Unity compilation;
- test basic play mode startup;
- test package updates.

Connect packages through local file paths while developing.

### `EvoPrototypeGame`

Purpose:

- actual game project;
- use package like a real consumer;
- prefer Git tag or local file path depending on urgency.

For fast prototyping, local file paths are acceptable. Before delivery, pin to a Git tag.

## Manual Test Checklist

For every package change:

```text
Open sandbox project
Clear Library if needed
Install core only
Run EvoTools/Setup
Install runtime/yandex modules
Generate scaffold
Open EntryPointScene
Press Play
Verify loading screen appears
Verify startup scene loads
Verify services resolve from DI
Verify no console compile errors
```

For Yandex:

```text
Install Yandex plugin
Enable required defines
Generate SavesYG partial into Assets
Build WebGL
Verify cloud save path does not compile in Editor-only assumptions
```

## Automated Tests

Add tests gradually.

Start with EditMode tests:

- `SceneCatalog` lookup;
- `LoadingPipelineBuilder` order;
- provider matching by flow/tags;
- duplicate step handling;
- save migration;
- config catalog lookup.

Then PlayMode tests:

- `LoadingService.LoadStartupAsync`;
- scene transition;
- loading screen show/hide;
- resource preload with test addressables.

For now, manual sandbox testing is enough for the prototype. Add automated tests around the loading builder first because it is pure C# and high-value.

## Practical Development Process

For the current third project:

1. Create or use a sandbox Unity project.
2. Connect packages via `file:` paths.
3. Fix only blockers in package code.
4. Generate scaffold into the new game.
5. Implement project-specific providers and steps in `Assets/_Project`.
6. Keep prototype-specific hacks in project code.
7. After the prototype stabilizes, move reusable parts back into packages.
8. Tag a package version before delivery.

Rule:

```text
If it is needed only by this game this week, keep it in Assets/_Project.
If it is clearly useful in the next 3 projects, move it into a package.
```

## Documentation Files

Architecture diagram:

```text
Docs/loading-architecture.drawio
```

This document:

```text
Docs/package-technical-overview.md
```
