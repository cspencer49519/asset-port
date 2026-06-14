# Genobear Real TCG Overhaul on game 0.71 — quick reference

**Players:** use the full guide → **[docs/INSTALL-071.md](docs/INSTALL-071.md)**

This file is a maintainer checklist mirroring a working install.

## Required components

| Component | Version / source |
|-----------|------------------|
| Game | **0.71** (Steam) |
| BepInEx Pack | Nexus mod 27 |
| Add New Cards Mod | Nexus mod 3 |
| More Card Expansions | **1.8.7** + **TCGShopExpansionMod071Patch 1.0.49** |
| TextureReplacer | Nexus mod 26 |
| ArtExpander + `cardart.assets` | Genobear pack (~15 GB) |
| **Ported sharedassets0 trio** | Release `assets/` via install script — **not** raw Genobear 0.62 file |

## Install flow (player)

1. Nexus mods + Genobear (manual)
2. Extract **TCG-071-Genobear** release zip
3. Run `Install-TCG071Mods` script (patch + sharedassets)
4. Run `Verify-TCG071Install` script
5. F1 → ExpansionMod settings per [VERSION_MATRIX.md](docs/VERSION_MATRIX.md)

## Optional but common

- Configuration Manager (F1 menu)
- Holographic Overhaul (foils)
- Enhanced Prefab Loader

## Known non-fatal log messages

- ArtExpander GhostCardPatch Harmony error (Genobear DLL vs 0.71)
- `animated.assets not found`

## Shelf crash without patch

See [SHELF_ERROR_FIX.md](SHELF_ERROR_FIX.md).
