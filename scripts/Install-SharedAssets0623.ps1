param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"
$assetPort = Split-Path $PSScriptRoot -Parent
$sourceDir = Join-Path $assetPort "output-0623"
$dataDir = Join-Path $GameDir "Card Shop Simulator_Data"

$trio = @(
    "sharedassets0.assets",
    "sharedassets0.assets.resS",
    "sharedassets0.resource"
)

foreach ($name in $trio) {
    $src = Join-Path $sourceDir $name
    if (-not (Test-Path $src)) {
        throw "Missing ported file: $src`nRun: python asset-port/port_assets_0623.py"
    }
}

if (-not (Test-Path $dataDir)) {
    throw "Game data folder not found: $dataDir"
}

if (-not $SkipBackup) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupDir = Join-Path $dataDir "_backup_sharedassets_$timestamp"
    New-Item -ItemType Directory -Path $backupDir | Out-Null
    foreach ($name in $trio) {
        $existing = Join-Path $dataDir $name
        if (Test-Path $existing) {
            Copy-Item $existing $backupDir
        }
    }
    Write-Host "Backed up originals to $backupDir"
}

foreach ($name in $trio) {
    Copy-Item (Join-Path $sourceDir $name) (Join-Path $dataDir $name) -Force
    $size = (Get-Item (Join-Path $dataDir $name)).Length
    Write-Host "Installed $name ($size bytes)"
}

Write-Host "Done. Launch the game to verify sharedassets0 loads."
