# Install — Genobear / Real TCG Overhaul on game 0.71

This guide is for **players**. You do not need Visual Studio.

Tested stack: **TCG Card Shop Simulator 0.71** + **Genobear Real TCG Overhaul** + **TCGShopExpansionMod071Patch 1.0.49**.

The release zip includes install/verify scripts for **PowerShell**, **Windows CMD**, and **Linux/macOS (bash)**. CMD and shell scripts use **Python** (`python` / `python3`) to read `manifest.json`.

## What you need

| Item | Where |
|------|--------|
| Game **0.71** on Steam | [TCG Card Shop Simulator](https://store.steampowered.com/app/3077020/) |
| BepInEx Pack | [Nexus mod 27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27) |
| Add New Cards Mod | [Nexus mod 3](https://www.nexusmods.com/tcgcardshopsimulator/mods/3) |
| More Card Expansions **1.8.7** | [Nexus mod 48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) |
| TextureReplacer | [Nexus mod 26](https://www.nexusmods.com/tcgcardshopsimulator/mods/26) |
| Genobear pack + **cardart.assets** (~15 GB) | Genobear / MEGA (see pack README) |
| **0.71 patch** (this repo release) | GitLab release zip or `dist/` from maintainer |
| Ported **sharedassets0** trio (optional zip) | Release `assets/` folder or Genobear 0.71 add-on |

## Quick install (Windows — PowerShell)

1. Install the game and run it **once** (creates folders).
2. Install **BepInEx** into the game folder (see Nexus mod 27).
3. Install Nexus mods **3**, **48**, and **26** into `BepInEx/plugins/` per each mod’s instructions.
4. Install **Genobear** (New Cards data, ArtExpander, `cardart.assets`, configs) per Genobear README.
5. Download the **TCG-071-Genobear** release zip from GitLab.
6. Extract anywhere (e.g. `Downloads\TCG-071-Genobear`).
7. Open PowerShell in that folder and run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\Install-TCG071Mods.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

Use your real game path. If the game is a **sibling** of `asset-port` on a dev machine, you can omit `-GamePath`.

8. Run the verifier:

```powershell
.\scripts\Verify-TCG071Install.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

9. Launch the game. Press **F1** → **com.DarkDragoon.TCGShopExpansionMod** → enable settings listed in [VERSION_MATRIX.md](VERSION_MATRIX.md).

10. **Smoke test:** start or load a save → open a **Tetramon** pack → check **display case** shelves.

## Quick install (Windows — CMD)

Same steps 1–6 as above, then from Command Prompt in the extracted release folder:

```bat
scripts\Install-TCG071Mods.bat "D:\Steam\steamapps\common\TCG Card Shop Simulator"
scripts\Verify-TCG071Install.bat "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

Requires `python` or `py -3` on PATH. If Python is unavailable, use the PowerShell scripts above.

Flags: `/GamePath`, `/SkipAssets`, `/Force`, `/WhatIf`.

## Quick install (Linux / macOS — Steam or Proton)

Same Nexus/Genobear setup as Windows. The game runs under Proton on Linux; install paths still use `Card Shop Simulator.exe` inside the Steam game folder.

1. Download and extract the release zip.
2. Ensure `python3` is installed.
3. From the extracted folder:

```bash
chmod +x scripts/*.sh
./scripts/Install-TCG071Mods.sh --game-path "$HOME/.steam/steam/steamapps/common/TCG Card Shop Simulator"
./scripts/Verify-TCG071Install.sh --game-path "$HOME/.steam/steam/steamapps/common/TCG Card Shop Simulator"
```

If Steam is installed under `~/.local/share/Steam/`, the installer auto-detects that path when `--game-path` is omitted. macOS users can omit `--game-path` if the game lives under `~/Library/Application Support/Steam/steamapps/common/`.

Flags: `--game-path`, `--skip-assets`, `--force`, `--dry-run`.

## What the installer does

- Copies `TCGShopExpansionMod071Patch.dll` to `BepInEx/plugins/TCGShopExpansionMod071Patch/`
- Optionally installs ported `sharedassets0.assets` (+ `.resS`, `.resource`) with a timestamped backup
- Does **not** install Nexus mods or Genobear art (you must add those manually)

## Install flags

| PowerShell | CMD | bash | Effect |
|------------|-----|------|--------|
| `-GamePath` | `/GamePath` or first arg | `--game-path` or first arg | Path to game root |
| `-SkipAssets` | `/SkipAssets` | `--skip-assets` | Only install the 0.71 patch DLL |
| `-WhatIf` | `/WhatIf` | `--dry-run` | Show actions without copying files |
| `-Force` | `/Force` | `--force` | Overwrite patch DLL without prompt |

## Manual install (no script)

1. Create folder: `BepInEx/plugins/TCGShopExpansionMod071Patch/`
2. Copy `patches/TCGShopExpansionMod071Patch.dll` from the release zip into that folder.
3. (Optional) Back up then replace these three files in `Card Shop Simulator_Data/` from release `assets/`:
   - `sharedassets0.assets`
   - `sharedassets0.assets.resS`
   - `sharedassets0.resource`

## After install

Check `BepInEx/LogOutput.log` for:

```
TCGShopExpansionMod 0.71 Patch 1.0.49
Patched ExpansionMod for game 0.71
ArtExpander bridge ready
```

If something fails, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

## Developers

Build from source: [DEVELOPERS.md](DEVELOPERS.md).
