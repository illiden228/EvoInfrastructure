# Evo Infrastructure: Technical Overview

## Goal

Evo Infrastructure is a Unity package set for fast game prototyping and repeated project bootstrap.

Primary goals:

- start a new game quickly;
- install only the runtime systems the project uses;
- keep project-specific code editable in `Assets/_Project`;
- let platform packages implement shared abstractions without coupling base features to portals;
- keep package internals extractable into separate repositories later.

## Package Model

`com.evo.infrastructure.runtime` was removed. Runtime functionality is split into independent feature packages.

### `com.evo.infrastructure.core`

Editor-only bootstrap package.

Responsibilities:

- setup wizard;
- dependency checks;
- package installation;
- scaffold generation into `Assets/_Project`;
- project diagnostics;
- generated code update tools.

It should not contain runtime gameplay services.

### `com.evo.infrastructure.di`

Small common DI helper package.

Responsibilities:

- `EvoFeatureRegistry`;
- `RegisterEvoFeatures(...)`;
- shared extension surface for feature packages.

Feature packages add `UseXxx(...)` extension methods to this registry.

### Feature Packages

Current runtime features:

- `com.evo.infrastructure.config`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.focus`
- `com.evo.infrastructure.resources`
- `com.evo.infrastructure.scene`
- `com.evo.infrastructure.loading`
- `com.evo.infrastructure.ui`
- `com.evo.infrastructure.audio`
- `com.evo.infrastructure.save`
- `com.evo.infrastructure.ads`
- `com.evo.infrastructure.analytics`
- `com.evo.infrastructure.leaderboards`
- `com.evo.infrastructure.localization`
- `com.evo.infrastructure.pooling`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.editor-tools`
- `com.evo.infrastructure.build`

Each feature package owns its interfaces, default implementation, VContainer registration extension, and only the direct dependencies it needs.

### Platform Packages

Current platform packages are split by platform and feature:

- `com.evo.infrastructure.yandex`
- `com.evo.infrastructure.yandex.platform`
- `com.evo.infrastructure.yandex.ads`
- `com.evo.infrastructure.yandex.analytics`
- `com.evo.infrastructure.yandex.save`
- `com.evo.infrastructure.yandex.leaderboards`
- `com.evo.infrastructure.crazygames`
- `com.evo.infrastructure.crazygames.platform`
- `com.evo.infrastructure.crazygames.ads`
- `com.evo.infrastructure.crazygames.save`
- `com.evo.infrastructure.crazygames.leaderboards`

Platform packages depend on feature abstractions and register platform-specific implementations through feature registration extensions:

```csharp
builder.RegisterEvoFeatures(features =>
{
    features.UseAds();
    features.UseSave();
    features.UseYandexPlatform();
    features.UseYandexAds();
    features.UseYandexSave();
});
```

There are no separate "full" platform packages. The setup wizard provides category-level selection for installing all Yandex or all CrazyGames adapter packages.

Yandex and CrazyGames SDK references remain guarded by their existing scripting defines. This is still the correct model while SDKs are installed as project/plugin assets rather than stable asmdef packages.

Platform package installation means "this backend is available to the project", not "this backend is always active".

Examples:

- A project can install Yandex and CrazyGames packages but build Android. Runtime platform checks must keep Yandex/Crazy backends inactive.
- `UseCrazyGamesAds()` can be present in a project lifetime scope. The adapter still reports unavailable if `CRAZY` is missing or the runtime is not supported.
- Yandex adapters are still compiled behind PluginYG2 defines until a Yandex facade isolates direct PluginYG2 references.

### Platform SDK Assembly Boundaries

CrazySDK and PluginYG2 are not treated as stable UPM package dependencies.

Current rules:

- Do not auto-generate asmdefs for `com.evo.infrastructure.yandex*` or `com.evo.infrastructure.crazygames*` until SDK assembly references are verified.
- CrazySDK usually has no asmdef by default. If a project wants Evo Crazy packages behind asmdefs, create a CrazySDK asmdef in the project first and configure Evo to reference that assembly.
- `#if CRAZY` and `#if YandexGamesPlatform_yg` prevent code from compiling when defines are missing, but they do not solve asmdef references when defines are present.
- Direct CrazySDK calls are centralized in `CrazyGamesSdk`; adapter packages should not call CrazySDK directly.
- Yandex should follow the same facade pattern before Yandex platform asmdefs are enabled.

### Asmdef Utility

`com.evo.infrastructure.core` contains editor commands:

```text
EvoTools/Asmdefs/Validate Evo asmdefs
EvoTools/Asmdefs/Generate or Update Evo asmdefs
```

The utility can validate/generate non-platform Evo asmdefs and add known Evo/vendor references. It also tries to add Odin references only when Sirenix assemblies are present.

It intentionally skips Yandex and CrazyGames SDK packages until their SDK assembly names are known.

### Save Backend Selection

`com.evo.infrastructure.save` does not contain platform-specific backend selection rules. Every registered and available backend participates by default, sorted by backend priority.

Projects can opt out per backend through `SaveStorageOptions.backendSelection`:

```csharp
features.UseSave(new SaveStorageOptions
{
    backendSelection = SaveBackendSelectionPolicy
        .AllEnabled()
        .SetUsage("file", SaveBackendUsage.Disabled)
        .SetUsage("prefs", SaveBackendUsage.SaveOnly)
});
```

Common presets:

```csharp
// Only cloud save participates.
features.UseSave(new SaveStorageOptions
{
    backendSelection = SaveBackendSelectionPolicy.Only("yandex")
});

// Save to cloud, but allow local data to be loaded as fallback.
features.UseSave(new SaveStorageOptions
{
    backendSelection = SaveBackendSelectionPolicy.CloudPrimaryWithLocalLoadFallback("yandex")
});
```

Known backend ids:

- `file`
- `prefs`
- `yandex`
- `crazy`

## Dependency Direction

Correct direction:

```text
Game Project
  -> Platform Packages
  -> Feature Packages
  -> com.evo.infrastructure.di
```

Incorrect direction:

```text
Ads -> Yandex
Save -> CrazyGames
Feature Package -> Platform Package
```

Platform packages implement interfaces from save/ads/analytics/leaderboards/platform, not the other way around.

## Unity Connection Modes

### For Game Development

Use Git tags through Unity Package Manager.

Example:

```json
{
  "dependencies": {
    "com.evo.infrastructure.core": "https://github.com/illiden228/EvoInfrastructure.git?path=Packages/com.evo.infrastructure.core#v0.5.0"
  }
}
```

Then open:

```text
EvoTools/Setup
```

The setup wizard installs selected Unity/vendor dependencies, Evo feature packages, platform modules, and generated project scaffold.

### For Package Development

Use local package paths.

Example:

```json
{
  "dependencies": {
    "com.evo.infrastructure.core": "file:D:/Repos/EvoInfrastructure/Packages/com.evo.infrastructure.core",
    "com.evo.infrastructure.di": "file:D:/Repos/EvoInfrastructure/Packages/com.evo.infrastructure.di",
    "com.evo.infrastructure.ads": "file:D:/Repos/EvoInfrastructure/Packages/com.evo.infrastructure.ads"
  }
}
```

This makes packages editable while testing them in a Unity project.

### For Emergency Project Fixes

Use embedded packages:

```text
UnityProject/Packages/com.evo.infrastructure.ads
```

This is convenient for one-off fixes, but worse for shared package evolution.

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
```

The setup wizard should not overwrite an existing project `RuntimeProjectLifetimeScope`. If the file already exists and was edited by a game project, use the wizard diagnostics as guidance and add missing `Use...` registrations manually.

## Migration From `com.evo.infrastructure.runtime`

The old aggregate runtime package is replaced by feature packages.

Migration policy:

- detect `com.evo.infrastructure.runtime` in `Packages/manifest.json`;
- select replacement feature packages;
- install missing prerequisites first;
- add selected feature packages;
- remove only the old aggregate runtime package;
- do not rewrite project-owned `RuntimeProjectLifetimeScope`.

After migration, manually review feature registration in the project lifetime scope.

## Dependency Map

See `Docs/package-dependencies.md`.
