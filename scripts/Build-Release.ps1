#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the patch DLL and assembles dist/TCG-071-Genobear-{version} for release upload.
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$manifest = Get-Content -Raw (Join-Path $repoRoot "manifest.json") | ConvertFrom-Json
$version = $manifest.patchVersion
$distName = "TCG-071-Genobear-$version"
$distRoot = Join-Path $repoRoot "dist\$distName"

Write-Host "Building TCGShopExpansionMod071Patch..." -ForegroundColor Cyan
$csproj = Join-Path $repoRoot "TCGShopExpansionMod071Patch\TCGShopExpansionMod071Patch.csproj"
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    $fallback = "${env:ProgramFiles}\dotnet\dotnet.exe"
    if (Test-Path $fallback) { $dotnet = $fallback }
    else { throw "dotnet CLI not found. Install .NET SDK or add dotnet to PATH." }
}
else {
    $dotnet = $dotnet.Source
}
& $dotnet build $csproj -c $Configuration -v minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed"
}

$builtDll = Join-Path $repoRoot "TCGShopExpansionMod071Patch\bin\$Configuration\netstandard2.1\TCGShopExpansionMod071Patch.dll"
if (-not (Test-Path $builtDll)) {
    throw "Built DLL not found: $builtDll"
}

if (Test-Path $distRoot) {
    Remove-Item -Recurse -Force $distRoot
}

$dirs = @(
    "$distRoot\patches",
    "$distRoot\scripts",
    "$distRoot\docs",
    "$distRoot\assets"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Path $d -Force | Out-Null
}

Copy-Item -LiteralPath $builtDll -Destination "$distRoot\patches\TCGShopExpansionMod071Patch.dll"
Copy-Item -LiteralPath (Join-Path $repoRoot "manifest.json") -Destination "$distRoot\manifest.json"
Get-ChildItem -LiteralPath (Join-Path $repoRoot "docs") -File | Copy-Item -Destination "$distRoot\docs\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Install-TCG071Mods.ps1") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Verify-TCG071Install.ps1") -Destination "$distRoot\scripts\"

$assetSources = @(
    (Join-Path $repoRoot "assets"),
    (Join-Path $repoRoot "output")
)
$copiedAssets = $false
foreach ($src in $assetSources) {
    if (-not (Test-Path $src)) { continue }
    $files = @("sharedassets0.assets", "sharedassets0.assets.resS", "sharedassets0.resource")
    foreach ($f in $files) {
        $path = Join-Path $src $f
        if (Test-Path $path) {
            Copy-Item -LiteralPath $path -Destination "$distRoot\assets\"
            $copiedAssets = $true
        }
    }
    if ($copiedAssets) { break }
}

$zipPath = Join-Path $repoRoot "dist\$distName.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path $distRoot -DestinationPath $zipPath

Write-Host ""
Write-Host "Release folder: $distRoot" -ForegroundColor Green
Write-Host "Release zip:    $zipPath" -ForegroundColor Green
if (-not $copiedAssets) {
    Write-Host "Note: No sharedassets trio in assets/ or output/ — patch-only zip." -ForegroundColor Yellow
}
