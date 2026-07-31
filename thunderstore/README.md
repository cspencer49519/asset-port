# TCGShopExpansionMod 0.70.3 Patch

Compatibility patch for **TCG Card Shop Simulator 0.70.3** so [Genobear Real TCG Overhaul](https://old.thunderstore.io/c/tcg-card-shop-simulator/p/Genobear/Real_TCG_Overhaul/) works with More Card Expansions on this game build.

This package includes the patch DLL and the **pre-ported `sharedassets0` trio** (Genobear card frames for 0.70.3).

## Required

Install **[Genobear Real TCG Overhaul](https://old.thunderstore.io/c/tcg-card-shop-simulator/p/Genobear/Real_TCG_Overhaul/) first**. This patch is the 0.70.3 compatibility layer on top of that stack - it does **not** replace Genobear.

From Genobear's page, the following mods are required for proper functionality. Install them in this order:

1. **BepInEx Pack** **5.4.23.2** - Base mod loader required for all mods ([Nexus](https://www.nexusmods.com/tcgcardshopsimulator/mods/8) | [Thunderstore](https://thunderstore.io/c/tcg-card-shop-simulator/p/BepInEx/BepInExPack/))
2. **TextureReplacer** **1.6.1** - Handles texture modifications ([Nexus](https://www.nexusmods.com/tcgcardshopsimulator/mods/69))
3. **Add New Cards Mod** **1.6.0** - Enables adding new cards to the game ([Nexus](https://www.nexusmods.com/tcgcardshopsimulator/mods/200))
4. **More Card Expansions** **1.8.7** - Enables additional card expansion packs ([Nexus](https://www.nexusmods.com/tcgcardshopsimulator/mods/48))

**Note:** ArtExpander is included in the Genobear Real TCG Overhaul package and does **not** need to be downloaded separately. Credit for that component goes to its original creator ([ArtExpander on Nexus](https://www.nexusmods.com/tcgcardshopsimulator/mods/417)). Local Genobear stack uses **ArtExpander 3.4.3**.

Also required (from Genobear's README, not a Thunderstore dependency):

- **[Genobear Real TCG Overhaul](https://old.thunderstore.io/c/tcg-card-shop-simulator/p/Genobear/Real_TCG_Overhaul/) 5.1.0**
- **HD card art** (`cardart.assets`, ~15 GB) from MEGA - place under `BepInEx/plugins/ArtExpander/` with Genobear's ArtExpander **3.4.3**

### Tested versions (fingerprinted from a working local install)

| Component | Version found | Source |
|-----------|---------------|--------|
| Genobear Real TCG Overhaul | **5.1.0** | Game-root `manifest.json` |
| BepInEx | **5.4.23.2** | `BepInEx/core/BepInEx.dll` |
| TextureReplacer | **1.6.1** | Plugin DLL file version |
| Add New Cards Mod | **1.6.0** | Plugin DLL file version |
| More Card Expansions | **1.8.7** | Plugin DLL product version |
| ArtExpander | **3.4.3** | Plugin DLL product version |
| `cardart.assets` | Present (~14.1 GiB) | `BepInEx/plugins/ArtExpander/cardart.assets` |
| Imazen.WebP (ExpansionMod side) | **10.0.1** | Next to ExpansionMod DLL |

## Install process

Follow Genobear's install order from [Real TCG Overhaul](https://old.thunderstore.io/c/tcg-card-shop-simulator/p/Genobear/Real_TCG_Overhaul/), then apply this patch.

### 1. Game + BepInEx

1. Own **TCG Card Shop Simulator** on Steam.
2. Install **BepInEx Pack** (mod manager or manual into the game folder).
3. Launch the game once so BepInEx generates folders, then quit.

### 2. Core mods (Genobear prerequisites)

Install in this order (mod manager or manual into `BepInEx/plugins` as each mod directs):

1. **TextureReplacer**
2. **Add New Cards Mod**
3. **More Card Expansions** (use **1.8.7**)

### 3. Genobear Real TCG Overhaul

1. Install [**Real TCG Overhaul**](https://old.thunderstore.io/c/tcg-card-shop-simulator/p/Genobear/Real_TCG_Overhaul/) with the mod manager (or extract per that package's README).
2. Download **HD card assets** from MEGA (linked from Genobear's README) and place `cardart.assets` under `BepInEx/plugins/ArtExpander/` (with Genobear's ArtExpander **3.4.3**).
3. **Do not** copy Genobear's raw 0.62 `sharedassets0` files into game 0.70.3 - use the ported trio from **this** package instead (step 5).

### 4. This 0.70.3 patch (Thunderstore Mod Manager)

1. Install **TCGShopExpansionMod_0703_Patch** with the mod manager.
2. The manager places `TCGShopExpansionMod0703Patch.dll` under `BepInEx/plugins` correctly.

### 5. Sharedassets (manual - required for Genobear card frames)

Thunderstore Mod Manager does **not** install files into `Card Shop Simulator_Data/`. After installing this package:

1. Open this package's folder (or download the zip from the package / [GitHub Releases](https://github.com/cspencer49519/asset-port/releases)).
2. Back up your vanilla `Card Shop Simulator_Data/sharedassets0.*` files.
3. Copy these three files into `Card Shop Simulator_Data/`:

   - `sharedassets0.assets`
   - `sharedassets0.assets.resS`
   - `sharedassets0.resource`

### 6. Configure ExpansionMod (F1)

1. Launch the game and press **F1**.
2. Open More Card Expansions / TCG shop expansion mod settings.
3. Enable the expansion / custom config options you need (Genobear typically: enable the main toggles; leave play-table custom images off unless you know you want them).
4. Save and restart if prompted.

## Manual install (this zip only)

Extract this package into the game root (where `Card Shop Simulator.exe` lives) so that:

- `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll` exists
- `Card Shop Simulator_Data/sharedassets0.*` are replaced by the packaged trio

You must still complete steps 1-3 (BepInEx, core mods, Genobear + HD art) first.

## Alternative: GitHub / GitLab installer zip

[GitHub Releases](https://github.com/cspencer49519/asset-port/releases) also ship `TCG-0703-Genobear-*.zip` with install/verify scripts that copy the patch and sharedassets for you. Start with `docs/START_HERE.md` inside that zip after Genobear is installed.

## Team

Published under Thunderstore team **TCGPatch**.
