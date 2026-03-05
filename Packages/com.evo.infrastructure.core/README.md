# com.evo.infrastructure.core

Installer/bootstrap package.

After installing this package open:
`Tools/Evo/Infrastructure Setup Wizard`

Wizard steps:
1. Install dependencies (VContainer, UniTask, NuGetForUnity)
2. Create project folder structure
3. Install main infrastructure package (`com.evo.infrastructure.runtime`) from git tag
4. Create starter runtime scaffold (scenes + basic assets)

Note:
- Install `R3` (and `ObservableCollections` if needed) using NuGetForUnity before step 3.
