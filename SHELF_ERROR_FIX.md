# "Shelf data not loaded correctly" — diagnosis and fix

## Root cause (confirmed from BepInEx log)

When loading a save, **TCGShopExpansionMod** crashes inside `SetCardExtrasImages()`:

```
MissingFieldException: Field not found:
  System.Collections.Generic.List`1<UnityEngine.Sprite>
  .MonsterData_ScriptableObject.m_CardBorderList
```

**Game 0.70.3 removed `m_CardBorderList`** (and several related list fields) from
`MonsterData_ScriptableObject`. The game now uses methods like `GetCardBorderSprite`
instead. **More Card Expansions v1.8.7 still reads the old field via reflection**, so
first world load fails every time.

This is **not** caused by the ported `sharedassets0.assets` work.

### Verified on your install (2026-06-12)

- Log shows `Loading [TCGShopExpansionMod 1.8.7]` — update from 1.8.5.2 was applied.
- Load still fails with the **same** `m_CardBorderList` error.
- **Conclusion:** 1.8.7 is **not** compatible with game **0.70.3** yet.

### Error chain in your log

1. `SetCardExtrasImages()` → MissingFieldException (`m_CardBorderList`)
2. `CardUI_SetCardUI_Postfix` → NullReferenceException (ExpansionMod not initialized)
3. `CardShelf_LoadCardCompartment` → shelf load fails
4. `Shelf data not loaded properly` → return to title
5. Follow-on NREs in `LightManager_Awake` / TextureReplacer are **side effects**, not the root cause.

## Fix options (pick one)

### Option A — Play now without ExpansionMod (recommended until author patches)

1. Rename:
   `BepInEx/plugins/TCGShopExpansionMod/TCGShopExpansionMod.dll`
   → `TCGShopExpansionMod.dll.off`
2. Launch and start/load a game — shelf error should disappear.
3. You keep: **NewCardsMod** (Pokémon cards), **ArtExpander** (HD `cardart.assets`),
   **TextureReplacer**, **Holographic Overhaul**.
4. You lose: per-card `.ini` overlays from **CustomExpansionPackImages** (needs ExpansionMod),
   F1 expansion toggles, custom border sprite swapping from ExpansionMod zip caches.

Genobear card **art** still comes mainly from ArtExpander + NewCardsMod; ExpansionMod is for
layout/config overlays, not the 15 GB art bundle.

### Option B — Wait for / request a 0.70.3-compatible ExpansionMod

- Watch [Nexus mod 48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) for a release
  **after 1.8.7** that mentions game 0.70.3 or `GetCardBorderSprite` / removed `m_CardBorderList`.
- File a bug report with the log excerpt above — the author closed older tickets assuming 1.8.7
  fixed remaining issues.

### Option C — Roll back game version (not ideal if you need 0.70.3)

Steam → game Properties → Betas → last version before the MonsterData refactor (if available).
Only use if you cannot play without ExpansionMod.

### Option E — 0.70.3 compatibility patch (installed in this workspace)

A small BepInEx plugin **`TCGShopExpansionMod0703Patch`** skips ExpansionMod calls that
mutate removed `MonsterData_ScriptableObject` sprite lists on game 0.70.3:

- `SetCardExtrasImages` / `ReplaceCardBorders` / `ReplaceCardBGs` / `ReplaceCardFronts`
- Adds null guards on ExpansionMod `CardUI_SetCardUI` and `LightManager_Awake` patches

**Location:** `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll`  
**Source:** `asset-port/TCGShopExpansionMod0703Patch/` (rebuild with `dotnet build -c Release`)

After installing, launch and confirm log contains:
`Patched ExpansionMod for game 0.70.3`. Load/start a game — `m_CardBorderList` error should be gone.

## F1 settings after ExpansionMod update (Genobear README)

- Access other card expansions = **true**
- Enable custom card images for new expansions = **true**
- Enable custom configs for new expansions = **true**
- Enable custom card images for original expansions = **true**
- Enable custom configs for original expansions = **true**
- Enable custom images for cards on play tables = **false** (Genobear default)

## Debug logging

| Source | How to enable |
|--------|----------------|
| **ExpansionMod** | `BepInEx/config/com.DarkDragoon.TCGShopExpansionMod.cfg` → `[DEBUG]` → `Toggle debugging mode = true` (already on) |
| **BepInEx file log** | `BepInEx/config/BepInEx.cfg` → `[Logging.Disk]` → `LogLevels` add `Debug` |
| **BepInEx console** | `BepInEx.cfg` → `[Logging.Console]` → `Enabled = true` |
| **Harmony patch detail** | `BepInEx.cfg` → `[Harmony.Logger]` → `LogChannels = Warn, Error, Debug` |
| **Holographic Overhaul** | `BepInEx/config/munch.holographicoverhaul.cfg` → `EnableDebugLogging = true` |

Primary log after a test run: `BepInEx/LogOutput.log`  
Search for: `MissingFieldException`, `Shelf data`, `TCGShopExpansionMod`.

## Log excerpt (your current failure)

```
TCGShopExpansionMod.Handlers.NewSwappingHandler.SetCardExtrasImages()
MissingFieldException: ... m_CardBorderList ...
TCGShopExpansionMod.Patches.PlayerPatches.CardUI_SetCardUI_Postfix
TCGShopNewCardsMod...CardShelf_LoadCardCompartment_Postfix
Shelf data not loaded properly
```
