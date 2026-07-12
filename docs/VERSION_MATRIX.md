# Version matrix (tested combinations)

Pin these versions when reporting bugs or helping others install.

| Component | Version | Notes |
|-----------|---------|--------|
| **Game** | **0.71** | Steam build with `MonsterData` refactor (no `m_CardBorderList`) |
| **BepInEx** | 5.4.22+ | Must load before other plugins |
| **TCGShopNewCardsMod** | 1.6.0.0+ | Add New Cards Mod |
| **TCGShopExpansionMod** | **1.8.7** | More Card Expansions — **not** 0.71-native; requires patch |
| **TCGShopExpansionMod071Patch** | **1.1.038** | This repo — album/binder skips, HandleCards skip, graded-set clamp |
| **Imazen.WebP** | **10.0.1** | Place `deps/Imazen.WebP.dll` next to ExpansionMod DLL |
| **TextureReplacer** | 1.6.1+ | Required by Genobear |
| **ArtExpander** | **3.8.1** (ArtExpander-src rebuild) | SetCardUI-based; do **not** use Genobear 3.4.3 `GhostCardPatch` DLL |
| **sharedassets0 trio** | **Ported** (release `assets/`) | **Required** for Genobear card frames — install script copies these |
| **Configuration Manager** | Nexus mod 31 | Recommended — **F1** in-game config |

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

## Log triage (0.71)

### Must fix

- ExpansionMod album close-up / binder sort / `HandleCards` using removed `CardUI.m_GhostCard` — fixed by **071 Patch 1.1.038+** (skips those ExpansionMod methods)
- ArtExpander `GhostCardPatch` / `SetGhostCardUI` — fixed by deploying **ArtExpander 3.8.1** from `ArtExpander-src`
- Missing `Imazen.WebP` next to ExpansionMod — install from `deps/Imazen.WebP.dll`
- `CardOpeningSequence` pack refs `animator=False, mesh=False` after sharedassets port — UI fan may work; pack wrapper animation needs scene object restore (see TROUBLESHOOTING)

### Worth watching

- `GradedCardSetCheckStatusScreen` oversized set — clamped by 071 Patch 1.1.038+
- SteamAPI init failed — ignore unless launched outside Steam and Steam features are required

### Safe to ignore

- `ArtExpander ... animated.assets not found`
- RectTransform parent spam
- `SetLoadData` null-ref infos
- DontDestroyOnLoad warnings
- BoxCollider negative scale

## Success checklist

- [ ] Save loads (no “Shelf data not loaded properly”)
- [ ] Log shows `071 Patch` **1.1.038+** and no `GhostCardPatch` / `SetGhostCardUI` errors
- [ ] Album close-up and binder sort open without ExpansionMod `MissingFieldException` / NRE spam
- [ ] Tetramon pack rip: stacked back → flip each card → fan row
- [ ] Shop display case: fronts toward customer, backs from behind
- [ ] Binder/album pages show correct card faces
