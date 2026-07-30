#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the patch DLL and assembles dist/TCG-0703-Genobear-{version} for release upload.
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
$distName = "TCG-0703-Genobear-$version"
$distRoot = Join-Path $repoRoot "dist\$distName"

Write-Host "Building TCGShopExpansionMod0703Patch..." -ForegroundColor Cyan
$csproj = Join-Path $repoRoot "TCGShopExpansionMod0703Patch\TCGShopExpansionMod0703Patch.csproj"
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

$builtDll = Join-Path $repoRoot "TCGShopExpansionMod0703Patch\bin\$Configuration\netstandard2.1\TCGShopExpansionMod0703Patch.dll"
if (-not (Test-Path $builtDll)) {
    throw "Built DLL not found: $builtDll"
}

if (Test-Path $distRoot) {
    Remove-Item -Recurse -Force $distRoot
}

$dirs = @(
    "$distRoot\patches",
    "$distRoot\scripts",
    "$distRoot\assets"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Path $d -Force | Out-Null
}

Copy-Item -LiteralPath $builtDll -Destination "$distRoot\patches\TCGShopExpansionMod0703Patch.dll"
Copy-Item -LiteralPath (Join-Path $repoRoot "manifest.json") -Destination "$distRoot\manifest.json"
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination "$distRoot\README.md"
# Include nested docs (release-notes/, etc.) so players get START_HERE + changelog.
Copy-Item -LiteralPath (Join-Path $repoRoot "docs") -Destination "$distRoot\docs" -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Install-TCG0703Mods.ps1") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Verify-TCG0703Install.ps1") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Install-TCG0703Mods.bat") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Verify-TCG0703Install.bat") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Install-TCG0703Mods.sh") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Verify-TCG0703Install.sh") -Destination "$distRoot\scripts\"
Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\read_manifest.py") -Destination "$distRoot\scripts\"

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

# Thunderstore package: flat zip root with icon/README/manifest + BepInEx plugin + Data sharedassets.
$tsMetaDir = Join-Path $repoRoot "thunderstore"
$tsIcon = Join-Path $tsMetaDir "icon.png"
$tsReadme = Join-Path $tsMetaDir "README.md"
$tsManifestTemplate = Join-Path $tsMetaDir "manifest.json"
foreach ($required in @($tsIcon, $tsReadme, $tsManifestTemplate)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Thunderstore metadata missing: $required"
    }
}

$assetFiles = @("sharedassets0.assets", "sharedassets0.assets.resS", "sharedassets0.resource")
$assetSourceDir = $null
foreach ($src in $assetSources) {
    if (-not (Test-Path $src)) { continue }
    $allPresent = $true
    foreach ($f in $assetFiles) {
        if (-not (Test-Path (Join-Path $src $f))) { $allPresent = $false; break }
    }
    if ($allPresent) {
        $assetSourceDir = $src
        break
    }
}
if (-not $assetSourceDir) {
    throw "Thunderstore package requires sharedassets trio in assets/ or output/ (sharedassets0.assets, .assets.resS, .resource)."
}

$tsDistName = "TCGPatch-TCGShopExpansionMod_0703_Patch-$version"
$tsStage = Join-Path $repoRoot "dist\$tsDistName-stage"
$tsZipPath = Join-Path $repoRoot "dist\$tsDistName.zip"
if (Test-Path $tsStage) { Remove-Item -Recurse -Force $tsStage }
if (Test-Path $tsZipPath) { Remove-Item -Force $tsZipPath }

$tsPluginDir = Join-Path $tsStage "BepInEx\plugins\TCGShopExpansionMod0703Patch"
$tsDataDir = Join-Path $tsStage "Card Shop Simulator_Data"
New-Item -ItemType Directory -Path $tsPluginDir -Force | Out-Null
New-Item -ItemType Directory -Path $tsDataDir -Force | Out-Null

Copy-Item -LiteralPath $builtDll -Destination (Join-Path $tsPluginDir "TCGShopExpansionMod0703Patch.dll")
Copy-Item -LiteralPath $tsIcon -Destination (Join-Path $tsStage "icon.png")
Copy-Item -LiteralPath $tsReadme -Destination (Join-Path $tsStage "README.md")
foreach ($f in $assetFiles) {
    Copy-Item -LiteralPath (Join-Path $assetSourceDir $f) -Destination (Join-Path $tsDataDir $f)
}

$tsManifest = Get-Content -Raw -LiteralPath $tsManifestTemplate | ConvertFrom-Json
$tsManifest.version_number = $version
$tsManifestJson = $tsManifest | ConvertTo-Json -Depth 4
$tsManifestPath = Join-Path $tsStage "manifest.json"
[System.IO.File]::WriteAllText($tsManifestPath, $tsManifestJson, [System.Text.UTF8Encoding]::new($false))

Compress-Archive -Path (Join-Path $tsStage "*") -DestinationPath $tsZipPath
Remove-Item -Recurse -Force $tsStage

Write-Host ""
Write-Host "Release folder: $distRoot" -ForegroundColor Green
Write-Host "Release zip:    $zipPath" -ForegroundColor Green
Write-Host "Thunderstore:   $tsZipPath" -ForegroundColor Green
if (-not $copiedAssets) {
    Write-Host "Note: Installer zip has no assets/ folder; Thunderstore zip includes sharedassets from $assetSourceDir." -ForegroundColor Yellow
}
