# Nexus Mods page copy (paste into upload form)

Use with [Best Practices for Mod Authors](https://help.nexusmods.com/article/136-best-practices-for-mod-authors) and [File Submission Guidelines](https://help.nexusmods.com/article/28-file-submission-guidelines).

## Suggested title

`TCGShopExpansionMod 0.70.3 Patch - Genobear Compatibility`

## Short description (≤ ~250 chars if limited)

`Makes More Card Expansions 1.8.7 work on TCG Card Shop Simulator 0.70.3, with graded-slab and display fixes. Soft-requires Genobear Overhaul (not on Nexus). Optional ported sharedassets for card frames.`

## Category / tags (adjust to available site tags)

- Category: **Utilities** or **Bug Fixes** / **Patches** (whichever the game section offers)
- Tags ideas: `BepInEx`, `0.70.3`, `ExpansionMod`, `compatibility`, `patch`, `cards`

## Requirements (Nexus requirement picker)

Set as **required** (these exist on Nexus):

- BepInEx Pack — [mod 27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27)
- Add New Cards Mod — [mod 3](https://www.nexusmods.com/tcgcardshopsimulator/mods/3)
- TextureReplacer — [mod 26](https://www.nexusmods.com/tcgcardshopsimulator/mods/26)
- More Card Expansions — [mod 48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) (**1.8.7**)

Set as **optional / recommended**:

- Configuration Manager — [mod 31](https://www.nexusmods.com/tcgcardshopsimulator/mods/31)
- Holographic Overhaul — [mod 44](https://www.nexusmods.com/tcgcardshopsimulator/mods/44)

**Do not** add Genobear as a Nexus requirement (page removed / unavailable).  
**Do not** link Thunderstore or other external mod hosts in the description ([advertising / external services restrictions](https://help.nexusmods.com/article/28-file-submission-guidelines)).

## Files to upload

| Nexus file slot | Archive from `dist/` | Notes |
|-----------------|----------------------|-------|
| **Main files** | `TCGPatch-TCGShopExpansionMod0703Patch-Nexus-Main-{version}.zip` | Vortex-friendly `BepInEx/plugins/…` layout. Mark as latest main. |
| **Optional files** | `TCGPatch-TCGShopExpansionMod0703Patch-Nexus-SharedAssets-{version}.zip` | Ported frames. Description: *Manual install into Card Shop Simulator_Data. Required for Genobear-style card frames on 0.70.3.* |
| Optional (power users) | `TCG-0703-Genobear-{version}.zip` from GitHub release | Scripted installer; only if you want a third file. Prefer linking GitHub Releases for this rather than hosting a duplicate if space is a concern. |

## Thumbnail / images

- Use `thunderstore/icon.png` (256×256 Real TCG Overhaul Patch logo) as the mod image / thumbnail.
- Add 1–3 in-game screenshots (binder graded slab, shop display front/back, pack rip) when available.

## Description body (BBCode-friendly outline)

```
[b]TCGShopExpansionMod 0.70.3 Patch[/b]

Compatibility patch for TCG Card Shop Simulator [b]0.70.3[/b] so More Card Expansions 1.8.7 and the Genobear Real TCG Overhaul stack work on this game build.

[b]This is not Genobear.[/b] It does not include HD card art (~15 GB). Genobear Real TCG Overhaul is currently unavailable on Nexus Mods — obtain that pack from the author's own distribution (MEGA / Genobear README), then install this patch.

[b]Features[/b]
[list]
[*]ExpansionMod 1.8.7 compatibility on game 0.70.3 (album/binder/shelf fixes)
[*]Graded Destiny / Trainer / Ghost binder slabs
[*]Shop display graded card backs
[*]Optional ported sharedassets0 trio for Genobear-style card frames
[/list]

[b]Install[/b]
[list=1]
[*]Install BepInEx + required Nexus mods (see Requirements)
[*]Install Genobear ArtExpander 3.4.3 + cardart.assets from Genobear's pack
[*]Install this mod's MAIN file with Vortex (or extract BepInEx/plugins/... manually)
[*]Download OPTIONAL SharedAssets and extract into the game root so Card Shop Simulator_Data/sharedassets0.* are replaced (back up first)
[*]F1 → ExpansionMod settings → enable custom images/configs
[/list]

[b]Credits[/b]
DarkDragoon (ExpansionMod), Genobear (Real TCG Overhaul), shaklin (TextureReplacer), cklapperich (ArtExpander), TCGPatch maintainers (this patch).

Source: https://github.com/cspencer49519/asset-port
```

## Permissions / credits notes for the form

- Credit Genobear; do not claim ownership of Genobear art or pack content.
- This page ships only the compatibility patch + (optional) maintainers' ported sharedassets for 0.70.3.
- Mark as free to use / modify per your preference; keep credit requirement for Genobear and ExpansionMod authors.
