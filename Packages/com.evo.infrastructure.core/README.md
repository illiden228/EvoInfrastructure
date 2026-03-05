# com.evo.infrastructure.core

Core infrastructure package for Unity:

- DI bootstrap (`RuntimeProjectLifetimeScope`, `RuntimeEntryPoint`)
- Loading pipeline and loading steps contracts
- Scene loading abstractions
- Resource/config/localization/audio/ui base services

## Notes

- Some code still uses original namespaces from source project.
- Odin-specific inspector attributes are wrapped with `#if ODIN_INSPECTOR`.
