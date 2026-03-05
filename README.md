# Evo Infrastructure UPM

UPM monorepo with split packages:

- `com.evo.infrastructure.core`
- `com.evo.infrastructure.runtime`
- `com.evo.infrastructure.yandex`

## Connect From Unity Project

1. Add scoped registry and package dependency to `Packages/manifest.json`.
2. Install `com.evo.infrastructure.core`.
3. Open `Tools/EvoTools/evo.infrastructure/Setup Wizard` and run steps.
4. Install Yandex plugin only if you use `com.evo.infrastructure.yandex`.

Example snippet is in:

- `Docs/manifest.snippet.json`

## Packages

- `Packages/com.evo.infrastructure.core`
- `Packages/com.evo.infrastructure.runtime`
- `Packages/com.evo.infrastructure.yandex`
