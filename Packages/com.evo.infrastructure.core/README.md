# com.evo.infrastructure.core

Installer/bootstrap package.

After installing this package open:
`EvoTools/Setup`

Wizard steps:
1. Install dependencies (VContainer, UniTask, NuGetForUnity)
2. Create project folder structure
3. Add `R3` + `ObservableCollections` to NuGet packages.config
4. Install main infrastructure package (`com.evo.infrastructure.runtime`) from git tag
5. Create starter runtime scaffold (scenes + basic assets)
