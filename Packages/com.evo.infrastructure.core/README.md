# com.evo.infrastructure.core

Installer/bootstrap package.

After installing this package open:
`EvoTools/Setup`

Wizard workflow:
1. Analyze installed packages and project state.
2. Install selected Unity/vendor dependencies.
3. Add reactive NuGet dependencies when selected.
4. Install selected Evo feature packages from the configured git tag.
5. Import optional SDK packages such as PluginYG2/Odin when selected.
6. Create project folder structure when selected.

Package installation and scaffold creation are separate operations. `Preview Scaffold`
performs a dry run first; apply the plan only after reviewing Create/Preserve/Conflict
items. Existing scripts, scenes and Build Settings entries are preserved.

The new-project scaffold creates EntryPoint, additive Loading, Transition and
addressable Gameplay scenes. EntryPoint and Transition are added to Build Settings;
Gameplay is assigned as startup/gameplay scene in ProjectConfig.

Existing project `RuntimeProjectLifetimeScope` files are project-owned. The wizard should not overwrite manual edits; update feature registration manually when adding packages to an existing game.

Legacy migration:

- detects `com.evo.infrastructure.runtime`;
- selects replacement feature packages;
- removes only the old aggregate runtime package after prerequisites are ready;
- leaves project scripts untouched.

Asmdef tools:

- `EvoTools/Asmdefs/Validate Evo asmdefs`
- `EvoTools/Asmdefs/Generate or Update Evo asmdefs`

Platform SDK packages are skipped by asmdef generation until their SDK assembly references are known.
