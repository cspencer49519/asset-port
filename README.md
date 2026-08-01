# TCG Card Shop Simulator — 0.70.3 Mod Tooling

Compatibility patch and install tooling for **Genobear Real TCG Overhaul** on **TCG Card Shop Simulator 0.70.3**.

**Current release:** **1.2.6**

## Players — start here

1. **[docs/START_HERE.md](docs/START_HERE.md)** — short install checklist  
2. **[docs/INSTALL-0703.md](docs/INSTALL-0703.md)** — full step-by-step guide  
3. **[docs/release-notes/v1.2.6.md](docs/release-notes/v1.2.6.md)** — what’s new

**What the release zip does for you:** copies the 0.70.3 compatibility patch **and** the pre-ported `sharedassets0` trio (Genobear card frames). You do **not** need to port assets yourself.

After installing Nexus mods and Genobear, run one script pair from the extracted release zip:

```powershell
.\scripts\Install-TCG0703Mods.ps1 -GamePath "YOUR_STEAM_GAME_FOLDER"
.\scripts\Verify-TCG0703Install.ps1 -GamePath "YOUR_STEAM_GAME_FOLDER"
```

```bat
scripts\Install-TCG0703Mods.bat "YOUR_STEAM_GAME_FOLDER"
scripts\Verify-TCG0703Install.bat "YOUR_STEAM_GAME_FOLDER"
```

```bash
./scripts/Install-TCG0703Mods.sh --game-path "YOUR_STEAM_GAME_FOLDER"
./scripts/Verify-TCG0703Install.sh --game-path "YOUR_STEAM_GAME_FOLDER"
```

Download a release zip from GitLab Releases (or run `scripts/Build-Release.ps1` locally).

| Doc | Purpose |
|-----|---------|
| [START_HERE.md](docs/START_HERE.md) | Fast path for players |
| [INSTALL-0703.md](docs/INSTALL-0703.md) | Full install guide |
| [VERSION_MATRIX.md](docs/VERSION_MATRIX.md) | Pinned mod versions + F1 settings |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Log errors and fixes |
| [DEVELOPERS.md](docs/DEVELOPERS.md) | Build from source, release process |

## What this repo provides

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod0703Patch/` | BepInEx Harmony patch (**1.2.6**) — ExpansionMod + graded slabs + pack/display fixes on 0.70.3 |
| `manifest.json` | Version pins for install/verify scripts |
| `scripts/` | `Install-TCG0703Mods` / `Verify-TCG0703Install` (`.ps1`, `.bat`, `.sh`), `Build-Release.ps1`, `Publish-GitLabRelease.ps1` |
| `port_*.py` | Maintainer asset port tools (not required for players) |

## Maintainers — build & publish release

```powershell
.\scripts\Build-Release.ps1
$env:GITLAB_TOKEN = 'glpat-...'
.\scripts\Publish-GitLabRelease.ps1
```

Upload creates GitLab release **v1.2.6** with full, patch-only, and Thunderstore zips. See [DEVELOPERS.md](docs/DEVELOPERS.md).

## Legacy notes

- [GENOBEAR_REQUIREMENTS.md](GENOBEAR_REQUIREMENTS.md) — install checklist
- [SHELF_ERROR_FIX.md](SHELF_ERROR_FIX.md) — `m_CardBorderList` / shelf load crash

## Remote

GitLab: `git@192.168.0.50:tcg-cardshopmods/asset-port.git`  
GitHub: `https://github.com/cspencer49519/asset-port`

## Changelog

### 1.2.6

- Fix graded Destiny/Trainer/Ghost cards on shop display stands showing the mirrored front instead of an opaque card back
- LateUpdate graded-slab face repair re-arms the shelf back instead of wiping it every frame
- Graded shelf `m_CardBack` faces rearward so UI Cull Off GradedFace art is not visible from behind the stand
- More reliable shelf interactable lookup via parent `Card3dUIGroup` registry

### 1.2.4

- Thunderstore README: fingerprinted tested dependency versions from a working local install
- Genobear-first install docs with Nexus links for required mods
- Patch DLL + pre-ported `sharedassets0` trio in the Thunderstore zip
