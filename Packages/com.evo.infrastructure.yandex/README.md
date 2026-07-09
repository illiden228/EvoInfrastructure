# com.evo.infrastructure.yandex

Yandex core package for shared PluginYG2 runtime config.

Install feature adapters independently:

- `com.evo.infrastructure.yandex.platform`
- `com.evo.infrastructure.yandex.ads`
- `com.evo.infrastructure.yandex.analytics`
- `com.evo.infrastructure.yandex.save`
- `com.evo.infrastructure.yandex.leaderboards`

The Evo Setup wizard can import PluginYG2 from the latest GitHub release. PluginYG2 is still imported as a `.unitypackage` into `Assets/PluginYourGames`.

Important asmdef note:

- PluginYG2 is treated as a project/plugin asset.
- Evo does not auto-generate asmdefs for Yandex packages until PluginYG2 assembly references are verified.
- Yandex adapters are guarded by PluginYG2 scripting defines such as `YandexGamesPlatform_yg`.
- A Yandex facade should be introduced before removing compile guards from Yandex feature extensions or enabling Yandex package asmdefs.

Runtime behavior:

- Installing Yandex packages means the project can use Yandex backends.
- The active platform/build profile still decides whether Yandex services should participate at runtime.
- Existing project lifetime scopes should be updated manually when adding Yandex feature registrations.
