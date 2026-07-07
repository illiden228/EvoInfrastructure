# com.evo.infrastructure.yandex

Yandex-specific integrations for `com.evo.infrastructure.runtime`:

- Yandex ads adapter/factory
- Yandex analytics adapter
- Yandex leaderboard adapter
- Yandex cloud save backend
- Yandex platform info provider
- Yandex platform lifecycle provider

## Required in target project

- Yandex Games plugin package and scripting define symbols (`YandexGamesPlatform_yg`, etc.).

The Evo Setup wizard can import PluginYG2 from the latest GitHub release as an
optional step. If the step is skipped, install PluginYG2 manually before using
the Yandex adapters.

This package intentionally has no runtime asmdef. YG2 is usually installed into
`Assets/PluginYourGames` without an asmdef, so a separate `Evo.Infrastructure.Yandex`
assembly cannot reference it. The package compiles with the project scripts and
registers adapters through `Evo.Infrastructure.Services.Yandex.YandexRuntimeInstaller`.
