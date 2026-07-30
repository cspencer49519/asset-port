# TCGShopExpansionMod 0.70.3 Patch

Compatibility patch for **TCG Card Shop Simulator 0.70.3** so Genobear Real TCG Overhaul / More Card Expansions works on this game build. This package includes the patch DLL and the **pre-ported `sharedassets0` trio** (Genobear card frames for 0.70.3).

## Install (Thunderstore Mod Manager)

1. Install this package with the mod manager (requires [BepInExPack](https://thunderstore.io/c/tcg-card-shop-simulator/p/BepInEx/BepInExPack/)).
2. The manager places `TCGShopExpansionMod0703Patch.dll` under `BepInEx/plugins` correctly.
3. **Sharedassets are not installed by the mod manager.** Copy them manually (see below).

## Sharedassets (required for Genobear card frames)

Thunderstore Mod Manager does not install files into `Card Shop Simulator_Data/`. After installing this package:

1. Open the package folder (or download the zip manually from the package page / GitLab release).
2. Copy these three files into your game’s `Card Shop Simulator_Data/` folder (next to the vanilla `sharedassets0.assets`):

   - `sharedassets0.assets`
   - `sharedassets0.assets.resS`
   - `sharedassets0.resource`

3. Back up the vanilla trio first if you want an easy restore.

**Do not** use raw Genobear 0.62 sharedassets on game 0.70.3 — use only the pre-ported trio from this package.

## Manual install (whole zip)

Extract the zip into the game root (where `Card Shop Simulator.exe` lives) so that:

- `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll` exists
- `Card Shop Simulator_Data/sharedassets0.*` are replaced by the packaged trio

## Prerequisites (not on Thunderstore)

Install these before or alongside this patch:

| Dependency | Source |
|------------|--------|
| Add New Cards Mod | [Nexus mod 3](https://www.nexusmods.com/tcgcardshopsimulator/mods/3) |
| More Card Expansions **1.8.7** | [Nexus mod 48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) |
| TextureReplacer | [Nexus mod 26](https://www.nexusmods.com/tcgcardshopsimulator/mods/26) |
| Genobear Real TCG Overhaul (`ArtExpander` + `cardart.assets`) | Genobear pack / MEGA |

Optional: [Configuration Manager](https://www.nexusmods.com/tcgcardshopsimulator/mods/31) (F1 in-game settings).

## Alternative GitLab installer zip

GitLab Releases also ship `TCG-0703-Genobear-*.zip` with install/verify scripts that copy the patch and sharedassets for you. Prefer that if you are not using a Thunderstore mod manager. Start with `docs/START_HERE.md` inside the zip.

## Team

Published under Thunderstore team **TCGPatch**.
