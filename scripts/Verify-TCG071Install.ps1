#Requires -Version 5.1
<#
.SYNOPSIS
  Verifies a 0.71 Genobear + 071 patch install.

.EXAMPLE
  .\Verify-TCG071Install.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
#>
[CmdletBinding()]
param(
    [string] $GamePath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Pass([string] $Message) {
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Write-Fail([string] $Message) {
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Write-Skip([string] $Message) {
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Resolve-ReleaseRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    $candidate = Split-Path -Parent $scriptDir
    if (Test-Path (Join-Path $candidate "manifest.json")) {
        return $candidate
    }
    throw "Could not find manifest.json."
}

function Resolve-GamePath([string] $ExplicitPath, [string] $ReleaseRoot) {
    if ($ExplicitPath) {
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }
    $sibling = Join-Path (Split-Path -Parent $ReleaseRoot) "TCG Card Shop Simulator"
    if (Test-Path (Join-Path $sibling "Card Shop Simulator.exe")) {
        return (Resolve-Path -LiteralPath $sibling).Path
    }
    throw "Pass -GamePath to the folder containing Card Shop Simulator.exe"
}

$releaseRoot = Resolve-ReleaseRoot
$manifest = Get-Content -Raw (Join-Path $releaseRoot "manifest.json") | ConvertFrom-Json
$gameRoot = Resolve-GamePath -ExplicitPath $GamePath -ReleaseRoot $releaseRoot

$failCount = 0
$warnCount = 0

function Test-FileExists([string] $RelativePath, [bool] $Required = $true) {
    $full = Join-Path $gameRoot $RelativePath
    if (Test-Path -LiteralPath $full) {
        Write-Pass $RelativePath
        return $true
    }
    if ($Required) {
        Write-Fail "Missing: $RelativePath"
        $script:failCount++
    }
    else {
        Write-Skip "Missing (optional): $RelativePath"
        $script:warnCount++
    }
    return $false
}

Write-Host "Verifying install at: $gameRoot" -ForegroundColor Cyan
Write-Host "Expected patch version: $($manifest.patchVersion)" -ForegroundColor Cyan
Write-Host ""

Test-FileExists $manifest.paths.gameExe | Out-Null
Test-FileExists "BepInEx" | Out-Null
Test-FileExists $manifest.paths.patchDll | Out-Null
Test-FileExists $manifest.paths.expansionModDll | Out-Null
Test-FileExists $manifest.paths.newCardsModDll | Out-Null
Test-FileExists $manifest.paths.cardArtAssets $false | Out-Null
Test-FileExists $manifest.paths.sharedAssets $false | Out-Null

$logPath = Join-Path $gameRoot $manifest.paths.logFile
if (Test-Path -LiteralPath $logPath) {
    Write-Pass $manifest.paths.logFile
    $logText = Get-Content -Raw -LiteralPath $logPath

    foreach ($marker in $manifest.logSuccessMarkers) {
        if ($logText -match [regex]::Escape($marker)) {
            Write-Pass "Log contains: $marker"
        }
        else {
            Write-Skip "Log missing (launch game once): $marker"
            $warnCount++
        }
    }

    foreach ($bad in $manifest.logFailureMarkers) {
        if ($logText -match [regex]::Escape($bad)) {
            Write-Fail "Log contains failure marker: $bad"
            $failCount++
        }
    }
}
else {
    Write-Skip "No log yet — launch the game once, then re-run this script."
    $warnCount++
}

Write-Host ""
if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host "All checks passed." -ForegroundColor Green
    exit 0
}
if ($failCount -eq 0) {
    Write-Host "Passed with $warnCount warning(s). See docs/TROUBLESHOOTING.md" -ForegroundColor Yellow
    exit 0
}

Write-Host "$failCount failure(s), $warnCount warning(s). See docs/TROUBLESHOOTING.md" -ForegroundColor Red
exit 1
