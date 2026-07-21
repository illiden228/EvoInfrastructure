# Reflection audit

This document records the reflection policy for Evo Infrastructure 0.5.25.

## Player runtime

Player assemblies do not use `System.Reflection`, `Type.GetType`, assembly scans, or
`Activator.CreateInstance`. Optional SDK integrations register strongly typed factories through
`EvoOptionalFeatureRegistry`, including the optional registrations emitted by the starter scaffold.

`ScriptableObjectConfigProvider` indexes configs by the actual `Type` of the referenced asset.
`ScriptableConfigEntry.TypeName` remains serialized only as editor migration and diagnostic
metadata; runtime lookup does not depend on an assembly-qualified string.

## Retained editor-only reflection

The following uses are intentional and must remain isolated from player assemblies:

- `InfrastructureSetupWizardWindow` creates and rebuilds assets from packages that may not be
  installed yet. The core package cannot reference these optional packages without introducing a
  dependency cycle, so the setup bridge resolves their public types and methods dynamically.
- The same wizard invokes Addressables editor APIs dynamically because Addressables is optional for
  `com.evo.infrastructure.core`. A typed bridge can replace this if an Addressables-specific editor
  package is introduced later.
- `InfrastructureSetupWizardWindow.FindTypeByName` uses `TypeCache` for Unity object types and an
  assembly-scan fallback only for optional non-Unity SDK/editor types such as Addressables settings
  and Odin attributes.
- `DefaultCatalogEditorAdapter` reflects serialized list fields only as a compatibility fallback for
  legacy project-owned catalogs that do not implement `ICatalogEditorMetadata`. Infrastructure
  catalogs use typed adapters and serialized-property key access.
- `ConfigHubWindow` reads `GameConfigAttribute` metadata to choose an editor category. Type discovery
  itself uses `TypeCache` and does not scan assemblies.
- Editor tests may inspect private implementation details when a public test seam would make the
  production API worse. These calls are never compiled into a player.

New runtime reflection requires an explicit design review. New editor reflection must document why a
typed API, `SerializedObject`, `TypeCache`, or explicit registration cannot be used.
