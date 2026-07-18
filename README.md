# TCG Card Shop Simulator — 0.70.3 Mod Tooling

Compatibility patch and install tooling for **Genobear Real TCG Overhaul** on **TCG Card Shop Simulator 0.70.3**.

## Players — start here

**[docs/INSTALL-0703.md](docs/INSTALL-0703.md)** — full step-by-step guide (Nexus mods, Genobear, then one install script).

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

Download a release zip from GitLab (or run `scripts/Build-Release.ps1` locally).

| Doc | Purpose |
|-----|---------|
| [INSTALL-0703.md](docs/INSTALL-0703.md) | Full install guide |
| [VERSION_MATRIX.md](docs/VERSION_MATRIX.md) | Pinned mod versions + F1 settings |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Log errors and fixes |
| [DEVELOPERS.md](docs/DEVELOPERS.md) | Build from source, release process |

## What this repo provides

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod0703Patch/` | BepInEx Harmony patch (v1.0.49) — ExpansionMod + pack/display fixes on 0.70.3 |
| `manifest.json` | Version pins for install/verify scripts |
| `scripts/` | `Install-TCG0703Mods` / `Verify-TCG0703Install` (`.ps1`, `.bat`, `.sh`), `read_manifest.py`, `Build-Release.ps1` |
| `port_*.py` | Maintainer asset port tools (not required for players) |

## Maintainers — build release

```powershell
.\scripts\Build-Release.ps1
```

Upload `dist/TCG-0703-Genobear-*.zip` to GitLab Releases. See [DEVELOPERS.md](docs/DEVELOPERS.md).

## Legacy notes

- [GENOBEAR_REQUIREMENTS.md](GENOBEAR_REQUIREMENTS.md) — install checklist
- [SHELF_ERROR_FIX.md](SHELF_ERROR_FIX.md) — `m_CardBorderList` / shelf load crash

## Remote

GitLab: `git@192.168.0.50:tcg-cardshopmods/asset-port.git`
