# Version matrix (tested combinations)

Pin these versions when reporting bugs or helping others install.

| Component | Version | Notes |
|-----------|---------|--------|
| **Game** | **0.71** | Steam build with `MonsterData` refactor (no `m_CardBorderList`) |
| **BepInEx** | 5.4.22+ | Must load before other plugins |
| **TCGShopNewCardsMod** | 1.6.0.0+ | Add New Cards Mod |
| **TCGShopExpansionMod** | **1.8.7** | More Card Expansions — **not** 0.71-native; requires patch |
| **TCGShopExpansionMod071Patch** | **1.0.49** | This repo |
| **TextureReplacer** | 1.6.1+ | Required by Genobear |
| **ArtExpander** | Genobear bundle | + `cardart.assets` (~15 GB) |
| **sharedassets0 trio** | Ported for 0.71 | From release `assets/` or local port output |

## ExpansionMod in-game settings (F1)

Under **com.DarkDragoon.TCGShopExpansionMod**:

| Setting | Value |
|---------|-------|
| Access other card expansions | **true** |
| Enable custom card images for new expansions | **true** |
| Enable custom configs for new expansions | **true** |
| Enable custom card images for original expansions | **true** |
| Enable custom configs for original expansions | **true** |
| Enable custom images for cards on play tables | **false** (Genobear default) |

## Known non-fatal log messages

- `ArtExpander ... GhostCardPatch` Harmony error — art still loads via `cardart.assets`
- `animated.assets not found` — optional ArtExpander file

## Success checklist

- [ ] Save loads (no “Shelf data not loaded properly”)
- [ ] Tetramon pack rip: stacked back → flip each card → fan row
- [ ] Shop display case: fronts toward customer, backs from behind
- [ ] Binder/album pages show correct card faces
