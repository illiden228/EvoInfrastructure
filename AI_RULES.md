# AI Rules

## Runtime Reflection and IL2CPP
- Do not use runtime reflection (`Type.GetType`, `Activator`, `AppDomain` assembly scans, or reflected member lookup) for dependency injection, feature registration, adapter discovery, or other fixed dependencies.
- Prefer strongly typed interfaces, factories, explicit DI registrations, and compile-time contracts.
- Optional SDK bridges must remain compile-safe when the vendor SDK is absent. Use guarded SDK assemblies, typed registration bridges, direct asmdef references, and `[assembly: AlwaysLinkAssembly]` when `RuntimeInitializeOnLoadMethod` is the only linker root.
- Do not rely on `link.xml` stored inside a UPM package. Unity 2022.3 does not support package-local linker XML as a reliable preservation mechanism.
- Treat serialized assembly-qualified type names and dynamic generic construction as stripping risks. Prefer explicit registries or serialized asset references where practical.
- Runtime reflection is allowed only when a genuinely dynamic or unstable third-party contract makes a typed API impractical. Isolate it in one bridge, document the reason, preserve all required IL2CPP members, and add focused tests.
- Editor-only reflection is allowed when required for tooling, but it must not leak into player assemblies.
- After changing optional SDK registration or reflection-sensitive code, validate asmdef references and run an IL2CPP build with managed stripping. Verify that expected bridge assemblies remain in `ManagedStripped` and that runtime diagnostics list every configured adapter.

