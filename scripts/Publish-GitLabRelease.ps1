#Requires -Version 5.1
<#
.SYNOPSIS
  Creates a GitLab release and uploads release zips as native downloadable assets.

.DESCRIPTION
  Uses glab release upload --use-package-registry so assets download via
  /-/releases/{tag}/downloads/... (external URL links 404 on private GitLab).

  Requires a GitLab Personal Access Token with api scope.
  Set GITLAB_TOKEN or GLAB_TOKEN, or pass -Token.

  Example:
    $env:GITLAB_TOKEN = 'glpat-...'
    .\scripts\Publish-GitLabRelease.ps1
#>
[CmdletBinding()]
param(
    [string] $Tag,
    [string] $ZipPath,
    [string] $GitLabHost = "192.168.0.50",
    [string] $ProjectPath = "tcg-cardshopmods/asset-port",
    [string] $Token,
    [switch] $SkipTagPush
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$manifest = Get-Content -Raw (Join-Path $repoRoot "manifest.json") | ConvertFrom-Json
$version = $manifest.patchVersion
$tagName = if ($Tag) { $Tag } else { "v$version" }
$distName = "TCG-071-Genobear-$version"
$zipFile = if ($ZipPath) { $ZipPath } else { Join-Path $repoRoot "dist\$distName.zip" }

$apiToken = $Token
if (-not $apiToken) { $apiToken = $env:GITLAB_TOKEN }
if (-not $apiToken) { $apiToken = $env:GLAB_TOKEN }
if (-not $apiToken) {
    throw "GitLab API token required. Set GITLAB_TOKEN or pass -Token (Personal Access Token with api scope)."
}

if (-not (Test-Path -LiteralPath $zipFile)) {
    Write-Host "Release zip not found; running Build-Release.ps1..." -ForegroundColor Yellow
    & (Join-Path $repoRoot "scripts\Build-Release.ps1")
    if (-not (Test-Path -LiteralPath $zipFile)) {
        throw "Release zip still missing: $zipFile"
    }
}

$patchOnlyZip = Join-Path $repoRoot "dist\$distName-patch-only.zip"
if (-not (Test-Path -LiteralPath $patchOnlyZip)) {
    Write-Host "Building patch-only zip (no sharedassets)..." -ForegroundColor Yellow
    $tempRoot = Join-Path $env:TEMP "tcg071-patch-$version"
    $folderName = $distName
    if (Test-Path $tempRoot) { Remove-Item -Recurse -Force $tempRoot }
    New-Item -ItemType Directory -Path "$tempRoot\$folderName\patches", "$tempRoot\$folderName\scripts", "$tempRoot\$folderName\docs" -Force | Out-Null
    $builtDll = Join-Path $repoRoot "TCGShopExpansionMod071Patch\bin\Release\netstandard2.1\TCGShopExpansionMod071Patch.dll"
    if (-not (Test-Path $builtDll)) {
        & (Join-Path $repoRoot "scripts\Build-Release.ps1")
        $builtDll = Join-Path $repoRoot "TCGShopExpansionMod071Patch\bin\Release\netstandard2.1\TCGShopExpansionMod071Patch.dll"
    }
    Copy-Item -LiteralPath $builtDll -Destination "$tempRoot\$folderName\patches\"
    Copy-Item -LiteralPath (Join-Path $repoRoot "manifest.json") -Destination "$tempRoot\$folderName\"
    Get-ChildItem -LiteralPath (Join-Path $repoRoot "docs") -File | Copy-Item -Destination "$tempRoot\$folderName\docs\"
    Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Install-TCG071Mods.ps1") -Destination "$tempRoot\$folderName\scripts\"
    Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\Verify-TCG071Install.ps1") -Destination "$tempRoot\$folderName\scripts\"
    if (Test-Path $patchOnlyZip) { Remove-Item -Force $patchOnlyZip }
    Compress-Archive -Path "$tempRoot\$folderName" -DestinationPath $patchOnlyZip
}

$git = "C:\Program Files\Git\bin\git.exe"
if (-not (Test-Path $git)) {
    $gitCmd = Get-Command git -ErrorAction SilentlyContinue
    if ($gitCmd) { $git = $gitCmd.Source } else { throw "git not found" }
}

if (-not $SkipTagPush) {
    $existingTag = & $git -C $repoRoot tag -l $tagName
    if (-not $existingTag) {
        Write-Host "Creating annotated tag $tagName..." -ForegroundColor Cyan
        $notesPath = Join-Path $repoRoot "docs\release-notes\$tagName.md"
        if (Test-Path $notesPath) {
            & $git -C $repoRoot tag -a $tagName -F $notesPath
        }
        else {
            & $git -C $repoRoot tag -a $tagName -m "TCGShopExpansionMod 0.71 Patch $version"
        }
    }
    Write-Host "Pushing tag $tagName..." -ForegroundColor Cyan
    & $git -C $repoRoot push origin $tagName
    if ($LASTEXITCODE -ne 0) { throw "git push origin $tagName failed" }
}

$encodedProject = [uri]::EscapeDataString($ProjectPath)
$baseUrl = "http://$GitLabHost/api/v4"
$headers = @{ "PRIVATE-TOKEN" = $apiToken }
$releaseUri = "$baseUrl/projects/$encodedProject/releases"

$glab = Get-Command glab -ErrorAction SilentlyContinue
if (-not $glab) {
    $fallbackGlab = Join-Path $env:LOCALAPPDATA "Programs\glab\glab.exe"
    if (Test-Path $fallbackGlab) { $glab = $fallbackGlab } else { throw "glab CLI not found. Install from https://gitlab.com/gitlab-org/cli" }
}
else {
    $glab = $glab.Source
}

Write-Host "Configuring glab for $GitLabHost..." -ForegroundColor Cyan
& $glab auth login --hostname $GitLabHost --token $apiToken --api-host $GitLabHost --api-protocol http --git-protocol ssh | Out-Null
$env:GITLAB_HOST = $GitLabHost

$notesPath = Join-Path $repoRoot "docs\release-notes\$tagName.md"
$description = if (Test-Path $notesPath) {
    Get-Content -Raw -LiteralPath $notesPath
}
else {
    @"
# TCGShopExpansionMod 0.71 Patch $version

Download release assets from this page (requires GitLab login) or use curl with a deploy token below.

See [INSTALL-071.md](http://$GitLabHost/$ProjectPath/-/blob/$tagName/docs/INSTALL-071.md) for player install steps.
"@
}

$releasePayload = @{
    name        = "TCGShopExpansionMod 0.71 Patch $version"
    tag_name    = $tagName
    description = $description
} | ConvertTo-Json -Depth 4

try {
    Invoke-RestMethod -Method Post -Uri $releaseUri -Headers $headers -ContentType "application/json" -Body $releasePayload | Out-Null
}
catch {
    $err = $_.ErrorDetails.Message
    if ($err -match "already exists") {
        Write-Host "Release exists; updating description..." -ForegroundColor Yellow
        $updateUri = "$releaseUri/$([uri]::EscapeDataString($tagName))"
        $updatePayload = @{
            name        = "TCGShopExpansionMod 0.71 Patch $version"
            description = $description
        } | ConvertTo-Json -Depth 4
        Invoke-RestMethod -Method Put -Uri $updateUri -Headers $headers -ContentType "application/json" -Body $updatePayload | Out-Null
    }
    else {
        throw "Release create failed: $err"
    }
}

# Remove broken external links from prior publish attempts.
$linksUri = "$releaseUri/$([uri]::EscapeDataString($tagName))/assets/links"
$existingLinks = Invoke-RestMethod -Method Get -Uri "$releaseUri/$([uri]::EscapeDataString($tagName))" -Headers $headers
foreach ($old in $existingLinks.assets.links) {
    Invoke-RestMethod -Method Delete -Uri "$linksUri/$($old.id)" -Headers $headers | Out-Null
    Write-Host "Removed old link: $($old.name)" -ForegroundColor Yellow
}

$packageName = "tcg-071-genobear"
$uploads = @(
    @{ Path = $zipFile; Label = "$distName-full.zip" }
    @{ Path = $patchOnlyZip; Label = "$distName-patch-only.zip" }
)

foreach ($item in $uploads) {
    $spec = "$($item.Path)#$($item.Label)#package"
    Write-Host "Uploading $($item.Label) via package registry..." -ForegroundColor Cyan
    & $glab release upload $tagName -R $ProjectPath --use-package-registry --package-name $packageName $spec
    if ($LASTEXITCODE -ne 0) {
        throw "glab release upload failed for $($item.Label)"
    }
}

$final = Invoke-RestMethod -Method Get -Uri "$releaseUri/$([uri]::EscapeDataString($tagName))" -Headers $headers
Write-Host ""
Write-Host "Release: http://$GitLabHost/$ProjectPath/-/releases/$tagName" -ForegroundColor Green
foreach ($link in $final.assets.links) {
    Write-Host "  $($link.name)" -ForegroundColor Green
    Write-Host "    $($link.direct_asset_url)" -ForegroundColor DarkGray
}
