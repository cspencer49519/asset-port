Genobear Real TCG Overhaul - install checklist (your game)
==========================================================

Required by Genobear README
---------------------------

| Requirement | Status | Notes |
|-------------|--------|-------|
| BepInEx Pack | OK | BepInEx 5.4.x in game root |
| TextureReplacer | OK | v1.6.1 in BepInEx/plugins |
| Add New Cards Mod | OK | TCGShopNewCardsMod 1.6.0.0 |
| More Card Expansions | **PATCHED for 0.71** | v1.8.7 + `TCGShopExpansionMod071Patch` (skips removed `m_CardBorderList` path). See `asset-port/SHELF_ERROR_FIX.md`. |
| ArtExpander (bundled) | OK | ArtExpander.dll + cardart.assets (~15 GB) |
| HD card art (MEGA) | OK | cardart.assets present |
| sharedassets0.assets (Genobear) | PORTED | Use ported file in Genobear pack / asset-port/output |

Also installed (not in Genobear readme but present)
-------------------------------------------------
- Enhanced Prefab Loader 6.0.1
- Holographic Overhaul 3.2.0
- Configuration Manager
- CustomExpansionPackImages configs/images (data only; normally loaded by TCGShopExpansionMod)

Known log warnings (non-fatal)
------------------------------
- ArtExpander GhostCardPatch Harmony error (version mismatch with current game)
- animated.assets not found (optional ArtExpander file)

Recommended next step
---------------------
Install More Card Expansions (TCGShopExpansionMod) from Nexus mod 48, then in-game
press F1 and confirm com.DarkDragoon.TCGShopExpansionMod settings match Genobear
README (enable expansion options except where noted).

After installing TCGShopExpansionMod, enable in its config:
- Access other card expansions = true
- Enable custom card images/configs for new and original expansions = true
