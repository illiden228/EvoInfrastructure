# com.evo.infrastructure.core

Installer/bootstrap package.

After installing this package open:
`Tools/EvoTools/evo.infrastructure/Setup Wizard`

Wizard steps:
1. Install dependencies (VContainer, UniTask, NuGetForUnity)
2. Create project folder structure
3. Install `R3` from Git URL
4. Install main infrastructure package (`com.evo.infrastructure.runtime`) from git tag
5. Create starter runtime scaffold (scenes + basic assets)

Note:
- Install `ObservableCollections` via NuGetForUnity before step 4.
