# TCG Card Shop Simulator — 0.71 Mod Tooling

Compatibility patch and install tooling for **Genobear Real TCG Overhaul** on **TCG Card Shop Simulator 0.71**.

## Players — start here

**[docs/INSTALL-071.md](docs/INSTALL-071.md)** — step-by-step install (no coding required)

After installing mods from Nexus and Genobear, use one of these script pairs from the release zip:

```powershell
.\scripts\Install-TCG071Mods.ps1 -GamePath "YOUR_STEAM_GAME_FOLDER"
.\scripts\Verify-TCG071Install.ps1 -GamePath "YOUR_STEAM_GAME_FOLDER"
```

```bat
scripts\Install-TCG071Mods.bat "YOUR_STEAM_GAME_FOLDER"
scripts\Verify-TCG071Install.bat "YOUR_STEAM_GAME_FOLDER"
```

```bash
./scripts/Install-TCG071Mods.sh --game-path "YOUR_STEAM_GAME_FOLDER"
./scripts/Verify-TCG071Install.sh --game-path "YOUR_STEAM_GAME_FOLDER"
```

Download a release zip from GitLab (or run `scripts/Build-Release.ps1` locally).

| Doc | Purpose |
|-----|---------|
| [INSTALL-071.md](docs/INSTALL-071.md) | Full install guide |
| [VERSION_MATRIX.md](docs/VERSION_MATRIX.md) | Pinned mod versions + F1 settings |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Log errors and fixes |
| [DEVELOPERS.md](docs/DEVELOPERS.md) | Build from source, release process |

## What this repo provides

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod071Patch/` | BepInEx Harmony patch (v1.0.49) — ExpansionMod + pack/display fixes on 0.71 |
| `manifest.json` | Version pins for install/verify scripts |
| `scripts/` | `Install-TCG071Mods` / `Verify-TCG071Install` (`.ps1`, `.bat`, `.sh`), `read_manifest.py`, `Build-Release.ps1` |
| `port_*.py` | Maintainer asset port tools (not required for players) |

## Maintainers — build release

```powershell
.\scripts\Build-Release.ps1
```

Upload `dist/TCG-071-Genobear-*.zip` to GitLab Releases. See [DEVELOPERS.md](docs/DEVELOPERS.md).

## Legacy notes

- [GENOBEAR_REQUIREMENTS.md](GENOBEAR_REQUIREMENTS.md) — install checklist
- [SHELF_ERROR_FIX.md](SHELF_ERROR_FIX.md) — `m_CardBorderList` / shelf load crash

## Remote

GitLab: `git@192.168.0.50:tcg-cardshopmods/asset-port.git`
