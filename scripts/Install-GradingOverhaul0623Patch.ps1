param(
    [string]$GameDir = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "TCG Card Shop Simulator-0.62.3"),
    [switch]$Revert
)

$ErrorActionPreference = "Stop"
$assetPort = Split-Path $PSScriptRoot -Parent
$pluginDir = Join-Path $GameDir "BepInEx\plugins\Grading Overhaul"
$target = Join-Path $pluginDir "Grading Overhaul.dll"
$patched = Join-Path $assetPort "tools\Grading Overhaul.patched.dll"
$backup = Join-Path $pluginDir "Grading Overhaul.dll.original-0623"

if ($Revert) {
    if (-not (Test-Path $backup)) {
        throw "No backup found: $backup"
    }
    Copy-Item $backup $target -Force
    Write-Host "Restored original Grading Overhaul.dll"
    exit 0
}

if (-not (Test-Path $target)) {
    throw "Grading Overhaul not installed: $target"
}

if (-not (Test-Path $backup)) {
    Copy-Item $target $backup
    Write-Host "Backed up current Grading Overhaul.dll to $backup"
}

if (-not (Test-Path $patched)) {
    throw "Missing patched DLL: $patched`nRun: dotnet run --project asset-port/tools/GradingOverhaul0623Patcher"
}

dotnet run --project (Join-Path $assetPort "tools\GradingOverhaul0623Patcher\GradingOverhaul0623Patcher.csproj")
if ($LASTEXITCODE -ne 0) { throw "GradingOverhaul0623Patcher failed" }

Copy-Item $patched $target -Force
Write-Host "Installed 0.62.3-compatible Grading Overhaul.dll"
Write-Host "Relaunch the game to test."
