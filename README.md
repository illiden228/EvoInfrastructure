# Evo Infrastructure UPM

UPM monorepo with split Unity packages.

## Connect From Unity Project

1. Install `com.evo.infrastructure.core`.
2. Open `EvoTools/Setup`.
3. Let the wizard install Unity/vendor dependencies and selected Evo feature packages.
4. Install only the platform packages you need.
5. Keep your project `RuntimeProjectLifetimeScope` under project ownership. The wizard creates a starter file only when needed; existing project edits should be updated manually.

Example snippet is in:

- `Docs/manifest.snippet.json`

Package dependency map:

- `Docs/package-dependencies.md`
- `Docs/package-technical-overview.md`

## Migration From Old Runtime Package

`com.evo.infrastructure.runtime` is deprecated and removed from the split package model.

Use `EvoTools/Setup` to migrate:

1. Analyze installed packages.
2. Use the legacy runtime migration block if `com.evo.infrastructure.runtime` is detected.
3. Install selected prerequisites first if the wizard asks for them.
4. Run migration to add replacement feature packages and remove only the old aggregate runtime package.
5. Manually review your existing `RuntimeProjectLifetimeScope` and add the required `Use...` calls.

The migration does not overwrite existing project lifetime scopes.

## Platform SDK Notes

Yandex and CrazyGames SDKs are treated as project/plugin assets, not stable UPM dependencies.

- Yandex uses PluginYG2 and its existing scripting defines.
- CrazyGames uses CrazySDK and the `CRAZY` define.
- Platform package asmdefs are intentionally not generated automatically until the SDK assembly reference is known.
- If CrazySDK has no asmdef, create one in the project first and then wire Evo Crazy asmdefs explicitly.

## Installation Model

For this monorepo, the supported install path is `com.evo.infrastructure.core` plus `EvoTools/Setup`.

Directly installing an arbitrary feature or platform adapter package by git path is not guaranteed to bring its dependency closure or external vendor packages. The wizard owns dependency selection, install order, diagnostics, and migration from the old runtime package.

## Main Packages

- `Packages/com.evo.infrastructure.core`
- `Packages/com.evo.infrastructure.di`
- `Packages/com.evo.infrastructure.debug`
- `Packages/com.evo.infrastructure.config`
- `Packages/com.evo.infrastructure.platform`
- `Packages/com.evo.infrastructure.focus`
- `Packages/com.evo.infrastructure.resources`
- `Packages/com.evo.infrastructure.scene`
- `Packages/com.evo.infrastructure.loading`
- `Packages/com.evo.infrastructure.ui`
- `Packages/com.evo.infrastructure.audio`
- `Packages/com.evo.infrastructure.save`
- `Packages/com.evo.infrastructure.ads`
- `Packages/com.evo.infrastructure.analytics`
- `Packages/com.evo.infrastructure.leaderboards`
- `Packages/com.evo.infrastructure.localization`
- `Packages/com.evo.infrastructure.pooling`
- `Packages/com.evo.infrastructure.build`
- `Packages/com.evo.infrastructure.editor-tools`
- `Packages/com.evo.infrastructure.yandex`
- `Packages/com.evo.infrastructure.yandex.platform`
- `Packages/com.evo.infrastructure.yandex.ads`
- `Packages/com.evo.infrastructure.yandex.analytics`
- `Packages/com.evo.infrastructure.yandex.save`
- `Packages/com.evo.infrastructure.yandex.leaderboards`
- `Packages/com.evo.infrastructure.crazygames`
- `Packages/com.evo.infrastructure.crazygames.platform`
- `Packages/com.evo.infrastructure.crazygames.ads`
- `Packages/com.evo.infrastructure.crazygames.save`
- `Packages/com.evo.infrastructure.crazygames.leaderboards`
