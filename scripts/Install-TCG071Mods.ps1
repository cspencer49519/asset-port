#Requires -Version 5.1
<#
.SYNOPSIS
  Installs TCGShopExpansionMod071Patch (and optional ported sharedassets) into a 0.71 game folder.

.EXAMPLE
  .\Install-TCG071Mods.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $GamePath = "",
    [switch] $SkipAssets,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step([string] $Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string] $Message) {
    Write-Host " OK  $Message" -ForegroundColor Green
}

function Write-Warn([string] $Message) {
    Write-Host "WARN $Message" -ForegroundColor Yellow
}

function Resolve-ReleaseRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    $candidate = Split-Path -Parent $scriptDir
    if (Test-Path (Join-Path $candidate "manifest.json")) {
        return $candidate
    }
    throw "Could not find manifest.json (expected release root parent of scripts/)."
}

function Resolve-GamePath([string] $ExplicitPath, [string] $ReleaseRoot) {
    if ($ExplicitPath) {
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $sibling = Join-Path (Split-Path -Parent $ReleaseRoot) "TCG Card Shop Simulator"
    if (Test-Path (Join-Path $sibling "Card Shop Simulator.exe")) {
        Write-Warn "Using sibling game folder: $sibling"
        return (Resolve-Path -LiteralPath $sibling).Path
    }

    $steamRoots = @(
        "$env:ProgramFiles(x86)\Steam\steamapps\common\TCG Card Shop Simulator",
        "$env:ProgramFiles\Steam\steamapps\common\TCG Card Shop Simulator"
    )
    foreach ($root in $steamRoots) {
        if (Test-Path (Join-Path $root "Card Shop Simulator.exe")) {
            Write-Warn "Using Steam default path: $root"
            return (Resolve-Path -LiteralPath $root).Path
        }
    }

    throw "Game folder not found. Pass -GamePath to the folder containing Card Shop Simulator.exe"
}

function Resolve-PatchDll([string] $ReleaseRoot) {
    $releaseDll = Join-Path $ReleaseRoot "patches\TCGShopExpansionMod071Patch.dll"
    if (Test-Path $releaseDll) {
        return (Resolve-Path -LiteralPath $releaseDll).Path
    }

    $devDll = Join-Path $ReleaseRoot "TCGShopExpansionMod071Patch\bin\Release\netstandard2.1\TCGShopExpansionMod071Patch.dll"
    if (Test-Path $devDll) {
        Write-Warn "Using dev build output for patch DLL."
        return (Resolve-Path -LiteralPath $devDll).Path
    }

    throw "Patch DLL not found. Run scripts\Build-Release.ps1 or dotnet build first."
}

function Read-Manifest([string] $ReleaseRoot) {
    $manifestPath = Join-Path $ReleaseRoot "manifest.json"
    if (-not (Test-Path $manifestPath)) {
        throw "Missing manifest.json at $manifestPath"
    }
    return Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
}

function Ensure-Directory([string] $Path) {
    if (-not (Test-Path $Path)) {
        if ($PSCmdlet.ShouldProcess($Path, "Create directory")) {
            New-Item -ItemType Directory -Path $Path -Force | Out-Null
        }
    }
}

function Copy-WithBackup([string] $Source, [string] $Destination, [string] $BackupDir) {
    if (-not (Test-Path $Source)) {
        return $false
    }

    Ensure-Directory (Split-Path -Parent $Destination)
    if (Test-Path $Destination) {
        Ensure-Directory $BackupDir
        $backupName = Join-Path $BackupDir (Split-Path -Leaf $Destination)
        if ($PSCmdlet.ShouldProcess($Destination, "Backup to $backupName")) {
            Copy-Item -LiteralPath $Destination -Destination $backupName -Force
            Write-Ok "Backed up $(Split-Path -Leaf $Destination)"
        }
    }

    if ($PSCmdlet.ShouldProcess($Destination, "Copy from $Source")) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        Write-Ok "Installed $(Split-Path -Leaf $Destination)"
    }
    return $true
}

$releaseRoot = Resolve-ReleaseRoot
$manifest = Read-Manifest $releaseRoot
$gameRoot = Resolve-GamePath -ExplicitPath $GamePath -ReleaseRoot $releaseRoot
$patchDll = Resolve-PatchDll $releaseRoot

Write-Step "Release root: $releaseRoot"
Write-Step "Game root:    $gameRoot"
Write-Step "Patch DLL:    $patchDll"

$exePath = Join-Path $gameRoot $manifest.paths.gameExe
if (-not (Test-Path $exePath)) {
    throw "Invalid game folder (missing $($manifest.paths.gameExe)): $gameRoot"
}

$bepInEx = Join-Path $gameRoot "BepInEx"
if (-not (Test-Path $bepInEx)) {
    Write-Warn "BepInEx folder not found. Install BepInEx (Nexus mod 27) before playing."
}

$pluginDir = Join-Path $gameRoot "BepInEx\plugins\TCGShopExpansionMod071Patch"
$pluginDll = Join-Path $pluginDir "TCGShopExpansionMod071Patch.dll"

Ensure-Directory $pluginDir

if ((Test-Path $pluginDll) -and -not $Force) {
    $existing = (Get-Item -LiteralPath $pluginDll).Length
    $incoming = (Get-Item -LiteralPath $patchDll).Length
    if ($existing -eq $incoming) {
        Write-Ok "Patch DLL already installed (same size)."
    }
    else {
        Write-Warn "Patch DLL exists and differs. Re-run with -Force to overwrite."
    }
}
elseif ($PSCmdlet.ShouldProcess($pluginDll, "Install patch DLL")) {
    Copy-Item -LiteralPath $patchDll -Destination $pluginDll -Force
    Write-Ok "Installed patch DLL v$($manifest.patchVersion)"
}

if (-not $SkipAssets) {
    Write-Step "Ported sharedassets trio (Genobear card frames)"
    $dataDir = Join-Path $gameRoot $manifest.paths.dataFolder
    $assetsSource = Join-Path $releaseRoot "assets"
    if (-not (Test-Path $assetsSource)) {
        $assetsSource = Join-Path $releaseRoot "output"
    }

    if (-not (Test-Path $assetsSource)) {
        Write-Warn "No assets/ or output/ folder in release — skipping sharedassets install."
        Write-Warn "Card frames will stay vanilla until you get a full release zip with assets/."
    }
    else {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $backupDir = Join-Path $dataDir "_backup_sharedassets_$timestamp"
        $names = @(
            (Split-Path -Leaf $manifest.paths.sharedAssets),
            (Split-Path -Leaf $manifest.paths.sharedAssetsResS),
            (Split-Path -Leaf $manifest.paths.sharedAssetsResource)
        )
        $installedAny = $false
        foreach ($name in $names) {
            $src = Join-Path $assetsSource $name
            $dst = Join-Path $dataDir $name
            if (Copy-WithBackup -Source $src -Destination $dst -BackupDir $backupDir) {
                $installedAny = $true
            }
            else {
                Write-Warn "Missing source asset: $src"
            }
        }
        if ($installedAny) {
            Write-Ok "Sharedassets backup folder: $backupDir"
        }
    }
}
else {
    Write-Warn "Skipped sharedassets install (-SkipAssets)."
}

Write-Host ""
Write-Step "Manual steps still required"
Write-Host "  1. Install Nexus mods + Genobear (phases 1-3 in docs/INSTALL-071.md) if not done yet"
Write-Host "  2. Run: .\scripts\Verify-TCG071Install.ps1 -GamePath `"$gameRoot`""
Write-Host "  3. Launch game, press F1, configure ExpansionMod (see docs/VERSION_MATRIX.md)"
Write-Host "  4. Do not use -SkipAssets on a normal install — card frames need the ported trio"
Write-Host ""
Write-Ok "Install complete."
