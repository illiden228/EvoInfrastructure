# Evo Infrastructure Package Dependencies

Direct dependencies by package. External installer dependencies are packages or assemblies the setup wizard installs into the Unity project.

The supported monorepo install path is `com.evo.infrastructure.core` plus `EvoTools/Setup`. Direct git-path installation of an arbitrary package is not guaranteed to install the full dependency closure.

## Core and Common

`com.evo.infrastructure.core`
- external: Unity UI / UGUI for starter scaffold editor helpers

`com.evo.infrastructure.di`
- external: VContainer

`com.evo.infrastructure.debug`
- none

`com.evo.infrastructure.editor-tools`
- `com.evo.infrastructure.ads`
- `com.evo.infrastructure.analytics`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.leaderboards`
- `com.evo.infrastructure.resources`
- `com.evo.infrastructure.save`
- `com.evo.infrastructure.scene`
- `com.evo.infrastructure.ui`
- external: Addressables
- external: Unity Localization

## Runtime Features

`com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- external: VContainer

`com.evo.infrastructure.platform`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- external: VContainer

`com.evo.infrastructure.focus`
- `com.evo.infrastructure.di`
- external: VContainer
- external: R3
- external: Unity Input System

`com.evo.infrastructure.resources`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- external: VContainer
- external: UniTask
- external: Addressables

`com.evo.infrastructure.scene`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- `com.evo.infrastructure.resources`
- external: VContainer
- external: UniTask
- external: Addressables

`com.evo.infrastructure.loading`
- `com.evo.infrastructure.analytics`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- `com.evo.infrastructure.localization`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.resources`
- `com.evo.infrastructure.save`
- `com.evo.infrastructure.scene`
- external: VContainer
- external: UniTask
- external: Addressables

`com.evo.infrastructure.ui`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- `com.evo.infrastructure.resources`
- `com.evo.infrastructure.scene`
- external: VContainer
- external: UniTask
- external: R3
- external: R3.Unity
- external: ObservableCollections
- external: ObservableCollections.R3
- external: Addressables
- external: TextMeshPro
- external: Unity Input System
- external: UGUI
- external: PrimeTween

`com.evo.infrastructure.audio`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- `com.evo.infrastructure.resources`
- external: VContainer
- external: UniTask
- external: Addressables

`com.evo.infrastructure.save`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- external: VContainer
- external: UniTask

`com.evo.infrastructure.ads`
- `com.evo.infrastructure.analytics`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- `com.evo.infrastructure.platform`
- external: VContainer
- external: UniTask
- external: R3

`com.evo.infrastructure.analytics`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- `com.evo.infrastructure.platform`
- external: VContainer
- external: UniTask

`com.evo.infrastructure.leaderboards`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- external: VContainer

`com.evo.infrastructure.localization`
- `com.evo.infrastructure.debug`
- `com.evo.infrastructure.di`
- external: VContainer
- external: UniTask
- external: Unity Localization

`com.evo.infrastructure.pooling`
- none

`com.evo.infrastructure.build`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.platform`

## Platform Packages

`com.evo.infrastructure.yandex`
- `com.evo.infrastructure.config`
- external SDK/plugin: PluginYG2, guarded by existing Yandex scripting defines
- note: platform SDK asmdef generation is skipped until PluginYG2 assembly references are verified

`com.evo.infrastructure.yandex.platform`
- `com.evo.infrastructure.yandex`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.di`
- external SDK/plugin: PluginYG2, guarded by existing Yandex scripting defines

`com.evo.infrastructure.yandex.ads`
- `com.evo.infrastructure.yandex`
- `com.evo.infrastructure.ads`
- `com.evo.infrastructure.di`
- external SDK/plugin: PluginYG2, guarded by existing Yandex scripting defines

`com.evo.infrastructure.yandex.analytics`
- `com.evo.infrastructure.yandex`
- `com.evo.infrastructure.analytics`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.di`
- external SDK/plugin: PluginYG2, guarded by existing Yandex scripting defines

`com.evo.infrastructure.yandex.save`
- `com.evo.infrastructure.yandex`
- `com.evo.infrastructure.save`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.di`
- external SDK/plugin: PluginYG2, guarded by existing Yandex scripting defines

`com.evo.infrastructure.yandex.leaderboards`
- `com.evo.infrastructure.yandex`
- `com.evo.infrastructure.leaderboards`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.di`
- external SDK/plugin: PluginYG2, guarded by existing Yandex scripting defines

`com.evo.infrastructure.crazygames`
- `com.evo.infrastructure.config`
- `com.evo.infrastructure.debug`
- external SDK/plugin: CrazySDK, guarded by `CRAZY`
- note: CrazySDK usually has no asmdef; create/choose a project asmdef before enabling Evo Crazy asmdefs

`com.evo.infrastructure.crazygames.platform`
- `com.evo.infrastructure.crazygames`
- `com.evo.infrastructure.platform`
- `com.evo.infrastructure.di`
- external SDK/plugin: CrazySDK, guarded by `CRAZY`

`com.evo.infrastructure.crazygames.ads`
- `com.evo.infrastructure.crazygames`
- `com.evo.infrastructure.ads`
- `com.evo.infrastructure.di`
- external SDK/plugin: CrazySDK, guarded by `CRAZY`

`com.evo.infrastructure.crazygames.save`
- `com.evo.infrastructure.crazygames`
- `com.evo.infrastructure.save`
- `com.evo.infrastructure.di`
- external SDK/plugin: CrazySDK, guarded by `CRAZY`

`com.evo.infrastructure.crazygames.leaderboards`
- `com.evo.infrastructure.crazygames`
- `com.evo.infrastructure.leaderboards`
- `com.evo.infrastructure.di`
- external SDK/plugin: CrazySDK, guarded by `CRAZY`

## External Installer Set

The setup wizard installs these into the Unity project rather than pinning them as git dependencies inside each package manifest:

- VContainer
- UniTask
- NuGetForUnity
- R3
- ObservableCollections
- ObservableCollections.R3
- PrimeTween
- Addressables
- Unity Localization
- Unity Input System
- UGUI
- TextMeshPro

## Asmdef Notes

SDK adapter packages use two Unity assemblies: a package API/config assembly and a nested SDK bridge assembly. The bridge is guarded by a package `versionDefine` plus `defineConstraints`, and references the vendor assembly explicitly. Firebase is commonly imported as DLL assets, so its bridge uses the manually diagnosed `EVO_FIREBASE_ANALYTICS_SDK` define instead of a UPM version define.

- `com.evo.infrastructure.analytics.firebase` -> analytics, config, DI, debug; external `Firebase.App` and `Firebase.Analytics`
- `com.evo.infrastructure.analytics.appmetrica` -> analytics, config, DI, debug; external `io.appmetrica.analytics` / `AppMetrica`
- `com.evo.infrastructure.analytics.adjust` -> analytics, config, DI, debug, platform; external `com.adjust.sdk` / `AdjustSdk.Scripts`
- `com.evo.infrastructure.ads.applovin` -> ads, analytics, config, DI, debug; external `com.applovin.mediation.ads` / `MaxSdk.Scripts`

Most non-platform Evo packages have or can generate package-local asmdefs.

Platform SDK packages are different:

- `com.evo.infrastructure.yandex*` is skipped by asmdef generation until PluginYG2 assembly references are known.
- `com.evo.infrastructure.crazygames*` is skipped by asmdef generation until the project provides a CrazySDK asmdef and the assembly name is configured.
- `#if` scripting defines do not add assembly references. If an Evo asmdef references code that calls an SDK assembly, that SDK assembly must be explicitly referenced.
- Odin references are optional and should be added only when Sirenix assemblies are present in the project.
