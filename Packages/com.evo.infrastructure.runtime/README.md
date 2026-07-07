# com.evo.infrastructure.runtime

Main runtime package for Evo infrastructure.

Contains:
- Loading pipeline and loading steps contracts
- Scene flow runtime
- Infrastructure services (resource/config/save/audio/ui and others)
- Reusable runtime modules for project starter bootstrap
- Editor tools menu for runtime package utilities

Install this package after dependency preinstall (VContainer, R3, UniTask, PrimeTween).

## EvoTools Build CI

CI builds can run without editor dialogs through:

```bash
Unity.exe -batchmode -quit -projectPath <path> -executeMethod Evo.Infrastructure.Editor.EvoTools.Build.EvoBuildCiEntryPoint.BuildFromEnvironment
```

Build tag format:

```text
{platform}_{buildType}_{artifactType}_{version}_{buildNumber}_{debugInfo?}
```

Examples:

```text
google_release_aab_8.5.17_123
google_develop_apk_8.5.17_124_debug
rustore_release_aab_8.5.17_125
ios_release_ipa_8.5.17_126
```

Supported inputs:

- `CI_BUILD_TAG`, or `-buildTag google_release_aab_8.5.17_123`
- `CI_OUTPUT_PATH`, or `-outputPath Builds/google/release/app.aab`
- `CI_BUILD_CONFIG_GUID`, or `-ciConfigGuid <guid>` for an `EvoBuildCiConfig` asset
- `CI_BUILD_PROFILE_GUID` / `-profileGuid <guid>` or `CI_BUILD_PROFILE_ID` / `-profileId <id>` to bypass tag mapping
- `CI_BUILD_GLOBAL_CONFIG_GUID` / `-globalConfigGuid <guid>` when not assigned in `EvoBuildCiConfig`
- `CI_PLATFORM_CATALOG_GUID` / `-platformCatalogGuid <guid>` when not assigned in `EvoBuildCiConfig`

Create an `EvoBuildCiConfig` asset from `Assets/Create/EvoTools/Build/CI Config` and map tag parts to `PlatformBuildProfile` assets. Leave a rule field empty to treat it as a wildcard; for example, empty `debugInfo` matches both tags with and without `_debug`.

CI versioning uses the tag as the source of truth:

- `PlayerSettings.bundleVersion` is set to tag `version`.
- Android `PlayerSettings.Android.bundleVersionCode` is set to tag `buildNumber`.
- iOS `PlayerSettings.iOS.buildNumber` is set to tag `buildNumber`.
- Existing manual version bump steps stay enabled for menu/manual builds, but skip their bump during CI when the tag already supplies version/build number. This prevents `..._123` from becoming `124` after `IncrementAndroidVersionCodeStep` or `IncrementIosBuildNumberStep`.

Android signing remains environment-based through the existing `ApplyAndroidSigningPasswordsStep` defaults:

- `EVO_ANDROID_KEYSTORE_PASS`
- `EVO_ANDROID_KEYALIAS_PASS`

The CI entrypoint writes `Builds/ci-build-result.json` with `success`, `outputPath`, `profileId`, parsed tag fields, messages and errors.

## Exclude Folders From Build

`ExcludeFoldersFromBuildStep` temporarily excludes configured `Assets/...` folders by moving each folder to the same path with the excluded suffix, `~` by default. For example, `Assets/_Downloads` becomes `Assets/_Downloads~` before the build and is restored during cleanup. The step moves the folder `.meta` together with the folder.

Default behavior is conservative for existing projects:

- `Missing Folder Behavior = Fail`: missing source folders fail the build.
- `Conflict Behavior = Fail`: if both `Assets/Folder` and `Assets/Folder~` exist, the build fails with a recovery message.

For CI workspaces where folders may be intentionally deleted before the build or left excluded after an interrupted previous run, use:

- `Missing Folder Behavior = Skip`
- `Conflict Behavior = DeleteExcluded`

In this mode:

- If the source folder exists and the excluded folder does not, the step moves source to excluded and restores it during cleanup.
- If both source and excluded are absent, the step logs that the folder is already absent and continues.
- If only the excluded folder exists, the step treats it as already excluded, tracks it, and restores it during cleanup.
- If both source and excluded exist, the source folder wins: the step deletes the stale excluded folder and excluded `.meta`, then excludes the source folder again.

Cleanup is idempotent. Repeated cleanup calls do not fail when the folder was already restored or already absent. If cleanup sees both source and excluded folders at restore time, it reports an error instead of deleting data; the next build start can resolve that duplicate when `Conflict Behavior = DeleteExcluded`.

Recommended SpaceRangers CI setup after updating this package:

1. Keep `Assets/_Downloads` in `folderPaths`.
2. Set `Missing Folder Behavior` to `Skip`.
3. Set `Conflict Behavior` to `DeleteExcluded`.
4. Remove the temporary project-side workaround that clears `folderPaths`.
