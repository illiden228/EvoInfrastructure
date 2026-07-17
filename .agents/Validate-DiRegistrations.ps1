param(
    [string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = 'Stop'
$runtimeFiles = Get-ChildItem (Join-Path $RepositoryRoot 'Packages') -Recurse -Filter '*.cs' |
    Where-Object { $_.FullName -match '\\Runtime\\' -and $_.FullName -notmatch '\\Tests\\|\\Editor\\' }
$sources = @{}
foreach ($file in $runtimeFiles) {
    $sources[$file.FullName] = [IO.File]::ReadAllText($file.FullName)
}

$registeredTypes = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($source in $sources.Values) {
    foreach ($match in [regex]::Matches(
        $source,
        '\.(?:Register|RegisterEntryPoint|RegisterComponent)\s*<\s*(?:[\w\.]+\s*,\s*)?([\w\.]+)\s*>')) {
        [void]$registeredTypes.Add(($match.Groups[1].Value -split '\.')[-1])
    }
}

$errors = [Collections.Generic.List[string]]::new()
foreach ($typeName in $registeredTypes) {
    $escaped = [regex]::Escape($typeName)
    $declaration = $sources.GetEnumerator() |
        Where-Object { $_.Value -match "\bclass\s+$escaped\b" } |
        Select-Object -First 1
    if ($null -eq $declaration) {
        continue
    }

    $constructors = [regex]::Matches(
        $declaration.Value,
        "(?ms)(?<inject>\[Inject\]\s*)?(?:public|internal|protected|private)\s+$escaped\s*\(")
    if ($constructors.Count -le 1) {
        continue
    }

    $injectCount = @($constructors | Where-Object { $_.Groups['inject'].Success }).Count
    if ($injectCount -ne 1) {
        $relativePath = [IO.Path]::GetRelativePath($RepositoryRoot, $declaration.Key)
        $errors.Add(
            "${relativePath}: registered type $typeName has $($constructors.Count) constructors and $injectCount [Inject] constructors.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Validated $($registeredTypes.Count) statically registered runtime types: constructor selection is unambiguous."
