param([switch]$OmitirCompilacion)
$ErrorActionPreference = 'Stop'
$legacyRoot = Split-Path $PSScriptRoot -Parent
if (-not $OmitirCompilacion) {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    & $msbuild (Join-Path $legacyRoot 'SistemaFacturacion.Legacy.slnx') /t:Build /p:Configuration=Debug /p:MvcBuildViews=true /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del legacy.' }
}
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$testDir = Join-Path $tempRoot ('FacturacionModulo3_' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
try {
    $webBin = Join-Path $legacyRoot 'legacyGestionDeFacturacion.web/bin'
    Get-ChildItem -LiteralPath $webBin -Filter '*.dll' -File | Copy-Item -Destination $testDir
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
    $exe = Join-Path $testDir 'PruebasArquitectura.exe'
    & $compiler /nologo /codepage:65001 /target:exe "/out:$exe" "/reference:$(Join-Path $testDir 'FacturacionLegacy.Datos.dll')" "/reference:$(Join-Path $testDir 'legacyGestionDeFacturacion.web.dll')" "/reference:$(Join-Path $testDir 'System.Web.Mvc.dll')" /reference:System.Web.dll /reference:System.Core.dll /reference:System.ComponentModel.DataAnnotations.dll (Join-Path $PSScriptRoot 'pruebas-modulo-3/PruebasArquitectura.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las pruebas de arquitectura.' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de arquitectura.' }
}
finally {
    if (Test-Path -LiteralPath $testDir) {
        $resolved = (Resolve-Path -LiteralPath $testDir).Path
        if ((Split-Path $resolved -Parent).TrimEnd('\') -ne $tempRoot -or (Split-Path $resolved -Leaf) -notmatch '^FacturacionModulo3_[a-f0-9]{32}$') { throw 'No se eliminará una ruta temporal inesperada.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
