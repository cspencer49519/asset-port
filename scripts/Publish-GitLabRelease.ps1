#Requires -Version 5.1
<#
.SYNOPSIS
  Creates a GitLab release and uploads the release zip as a downloadable asset.

.DESCRIPTION
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

Write-Host "Uploading $(Split-Path -Leaf $zipFile) ($([math]::Round((Get-Item $zipFile).Length / 1MB, 2)) MB)..." -ForegroundColor Cyan
$uploadUri = "$baseUrl/projects/$encodedProject/uploads"
$uploadForm = @{
    file = Get-Item -LiteralPath $zipFile
}
try {
    $upload = Invoke-RestMethod -Method Post -Uri $uploadUri -Headers $headers -Form $uploadForm
}
catch {
    throw "Upload failed: $($_.Exception.Message)"
}

$assetUrl = "http://$GitLabHost/$ProjectPath$($upload.url)"
Write-Host "Uploaded: $assetUrl" -ForegroundColor Green

$notesPath = Join-Path $repoRoot "docs\release-notes\$tagName.md"
$description = if (Test-Path $notesPath) {
    Get-Content -Raw -LiteralPath $notesPath
}
else {
    @"
# TCGShopExpansionMod 0.71 Patch $version

Download **$distName.zip** for patch DLL, install scripts, docs, and ported sharedassets0 trio.

See [INSTALL-071.md](http://$GitLabHost/$ProjectPath/-/blob/$tagName/docs/INSTALL-071.md) for player install steps.
"@
}

$releaseUri = "$baseUrl/projects/$encodedProject/releases"
$releasePayload = @{
    name         = "TCGShopExpansionMod 0.71 Patch $version"
    tag_name     = $tagName
    description  = $description
    assets       = @{
        links = @(
            @{
                name      = "$distName.zip"
                url       = $assetUrl
                link_type = "package"
            }
        )
    }
} | ConvertTo-Json -Depth 6

try {
    $release = Invoke-RestMethod -Method Post -Uri $releaseUri -Headers $headers -ContentType "application/json" -Body $releasePayload
}
catch {
    $err = $_.ErrorDetails.Message
    if ($err -match "already exists") {
        Write-Host "Release exists; updating with uploaded asset link..." -ForegroundColor Yellow
        $updateUri = "$releaseUri/$([uri]::EscapeDataString($tagName))"
        $release = Invoke-RestMethod -Method Put -Uri $updateUri -Headers $headers -ContentType "application/json" -Body $releasePayload
    }
    else {
        throw "Release create failed: $err"
    }
}

$releaseWebUrl = "http://$GitLabHost/$ProjectPath/-/releases/$tagName"
Write-Host ""
Write-Host "Release created: $releaseWebUrl" -ForegroundColor Green
Write-Host "Asset: $assetUrl" -ForegroundColor Green
