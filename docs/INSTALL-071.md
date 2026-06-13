# Install — Genobear / Real TCG Overhaul on game 0.71

This guide is for **players**. You do not need Visual Studio or Python.

Tested stack: **TCG Card Shop Simulator 0.71** + **Genobear Real TCG Overhaul** + **TCGShopExpansionMod071Patch 1.0.49**.

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

## Quick install (Windows)

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

## What the installer does

- Copies `TCGShopExpansionMod071Patch.dll` to `BepInEx/plugins/TCGShopExpansionMod071Patch/`
- Optionally installs ported `sharedassets0.assets` (+ `.resS`, `.resource`) with a timestamped backup
- Does **not** install Nexus mods or Genobear art (you must add those manually)

## Install flags

| Flag | Effect |
|------|--------|
| `-GamePath` | Path to game root (folder containing `Card Shop Simulator.exe`) |
| `-SkipAssets` | Only install the 0.71 patch DLL |
| `-WhatIf` | Show actions without copying files |
| `-Force` | Overwrite patch DLL without prompt |

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
