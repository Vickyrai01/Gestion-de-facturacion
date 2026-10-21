param([string]$NuGetPath = (Join-Path $PSScriptRoot 'nuget.exe'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path -LiteralPath $NuGetPath)) {
    Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/v6.14.0/nuget.exe' -OutFile $NuGetPath -UseBasicParsing
}
$packagesDir = Join-Path $projectRoot 'packages'
foreach ($project in @('legacyGestionDeFacturacion.web', 'FacturacionLegacy.Datos', 'FacturacionLegacy.Tools', 'FacturacionLegacy.Wcf', 'FacturacionLegacy.WindowsService')) {
    & $NuGetPath restore (Join-Path $projectRoot "$project/packages.config") -PackagesDirectory $packagesDir -Source https://api.nuget.org/v3/index.json -NonInteractive
    if ($LASTEXITCODE -ne 0) { throw "Falló la restauración de $project" }
}
