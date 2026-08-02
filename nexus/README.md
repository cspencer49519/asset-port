# TCGShopExpansionMod 0.70.3 Patch (Nexus)

Compatibility patch for **TCG Card Shop Simulator 0.70.3** so **More Card Expansions** and the Genobear Real TCG Overhaul stack work on this game build.

This package is the **0.70.3 compatibility layer**. It does **not** replace Genobear, ExpansionMod, TextureReplacer, or BepInEx.

## What this mod does

- Makes **More Card Expansions 1.8.7** usable on game **0.70.3** (album/binder/shelf crash fixes)
- Fixes graded Destiny / Trainer / Ghost slabs in binders and on shop displays
- Includes install guidance for the **pre-ported `sharedassets0` trio** (Genobear-style card frames on 0.70.3)

## Requirements (Nexus)

Install these first (Vortex or manual):

| Mod | Nexus |
|-----|-------|
| BepInEx Pack | [tcgcardshopsimulator/mods/27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27) |
| Add New Cards Mod | [tcgcardshopsimulator/mods/3](https://www.nexusmods.com/tcgcardshopsimulator/mods/3) |
| TextureReplacer | [tcgcardshopsimulator/mods/26](https://www.nexusmods.com/tcgcardshopsimulator/mods/26) |
| More Card Expansions **1.8.7** | [tcgcardshopsimulator/mods/48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) |

Recommended:

| Mod | Nexus |
|-----|-------|
| Configuration Manager | [tcgcardshopsimulator/mods/31](https://www.nexusmods.com/tcgcardshopsimulator/mods/31) |
| Holographic Overhaul | [tcgcardshopsimulator/mods/44](https://www.nexusmods.com/tcgcardshopsimulator/mods/44) |

## Soft requirement: Genobear Real TCG Overhaul

Full Genobear card art and expansion content are **not** included here and are **not currently available on Nexus Mods**.

You must obtain the Genobear Real TCG Overhaul pack from the author's own distribution (commonly a MEGA pack linked from the Genobear README / community posts). At minimum you need:

- `ArtExpander.dll` **3.4.3** under `BepInEx/plugins/ArtExpander/`
- `cardart.assets` (~15 GB) in the same folder
- Other Genobear data/config files per that pack's README

**Do not** copy Genobear's raw 0.62 `sharedassets0` files into game 0.70.3. Use the **ported sharedassets** optional file from this Nexus page instead.

## Files on this Nexus page

| File | Purpose |
|------|---------|
| **Main file** (`…-Nexus-Main-*.zip`) | Patch DLL in Vortex-friendly `BepInEx/plugins/…` layout |
| **Optional file** (`…-Nexus-SharedAssets-*.zip`) | Ported `sharedassets0` trio for Genobear card frames (manual extract into game Data) |

## Install — main file (patch)

### Vortex / mod manager

1. Install BepInEx and the required Nexus mods above.
2. Install Genobear ArtExpander + `cardart.assets` manually (see soft requirement).
3. Download this mod's **main file** with your mod manager.
4. Enable the mod. It should deploy to `BepInEx/plugins/TCGShopExpansionMod0703Patch/`.

### Manual

1. Close the game.
2. Extract the main zip so that this path exists next to `Card Shop Simulator.exe`:

   `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll`

## Install — optional sharedassets (card frames)

Vortex does **not** reliably install files into `Card Shop Simulator_Data/`. Do this manually:

1. Download the **SharedAssets** optional file.
2. Back up your existing:

   - `Card Shop Simulator_Data/sharedassets0.assets`
   - `Card Shop Simulator_Data/sharedassets0.assets.resS`
   - `Card Shop Simulator_Data/sharedassets0.resource`

3. Extract the optional zip into the **game root** (where the `.exe` lives) so those three files replace the ones under `Card Shop Simulator_Data/`.

## Configure ExpansionMod (F1)

1. Launch the game and press **F1** (Configuration Manager recommended).
2. Open More Card Expansions / `com.DarkDragoon.TCGShopExpansionMod`.
3. Enable expansion / custom image / custom config options you need.
4. Leave play-table custom images off unless you know you want them.

## Verify

In `BepInEx/LogOutput.log` you should see:

```
TCGShopExpansionMod 0.70.3 Patch 1.2.10
Patched ExpansionMod for game 0.70.3
```

Smoke test: load a save → open a Tetramon pack → check a display stand from front and behind → open binder graded Destiny/Trainer/Ghost pages.

## Credits

- **DarkDragoon** — More Card Expansions
- **Genobear** — Real TCG Overhaul / ArtExpander / card art pipeline (obtain from Genobear's distribution; not hosted on this page)
- **shaklin** — TextureReplacer
- **cklapperich** — ArtExpander
- This patch — TCGPatch / asset-port maintainers

## Source / issues

GitHub: https://github.com/cspencer49519/asset-port
