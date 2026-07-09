# com.evo.infrastructure.crazygames

CrazyGames core package for shared CrazySDK runtime config and helper logic.

Install feature adapters independently:

- `com.evo.infrastructure.crazygames.platform`
- `com.evo.infrastructure.crazygames.ads`
- `com.evo.infrastructure.crazygames.save`
- `com.evo.infrastructure.crazygames.leaderboards`

Target projects must provide CrazySDK and the `CRAZY` scripting define.

CrazySDK is expected to be installed by the game project. It is not embedded into this package.

Important asmdef note:

- CrazySDK usually has no asmdef by default.
- Evo does not auto-generate asmdefs for CrazyGames packages until the project provides or selects a CrazySDK assembly.
- If you want asmdef isolation, create a CrazySDK asmdef manually first, commonly with the `CRAZY` define constraint, then configure Evo Crazy asmdefs to reference it.

Runtime behavior:

- Direct CrazySDK calls are centralized in `CrazyGamesSdk`.
- Adapter packages should call the Evo facade, not CrazySDK directly.
- Feature registration can be present in a project even when the current build target is not CrazyGames. Backends should report unavailable outside supported runtime/define conditions.
