# com.evo.infrastructure.crazygames

CrazyGames-specific integrations for `com.evo.infrastructure.runtime`:

- CrazyGames ads adapter/factory
- CrazyGames cloud save backend through `CrazySDK.Data`
- CrazyGames player auth service
- CrazyGames platform info provider
- CrazyGames platform lifecycle provider
- CrazyGames no-op leaderboard adapter

## Required in target project

- CrazySDK Unity package.
- `CRAZY` scripting define symbol.

This package intentionally has no runtime asmdef. CrazySDK ships its own
`CrazySDK.Runtime` assembly constrained by the `CRAZY` define. Keeping this
package in the project scripts assembly lets it compile in projects where
CrazySDK is installed, while all CrazySDK references remain guarded by
`#if CRAZY` so projects and non-Crazy platform builds are not broken.

Register adapters through:

```csharp
Evo.Infrastructure.Services.CrazyGames.CrazyGamesRuntimeInstaller.Register(builder, configService);
```
