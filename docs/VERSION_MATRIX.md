# Version matrix (tested combinations)

Pin these versions when reporting bugs or helping others install.

| Component | Version | Notes |
|-----------|---------|--------|
| **Game** | **0.70.3** | Steam build with `MonsterData` refactor (no `m_CardBorderList`) |
| **BepInEx** | 5.4.22+ | Must load before other plugins |
| **TCGShopNewCardsMod** | 1.6.0.0+ | Add New Cards Mod |
| **TCGShopExpansionMod** | **1.8.7** | More Card Expansions — **not** 0.70.3-native; requires patch |
| **TCGShopExpansionMod0703Patch** | **1.2.7** | This repo — graded Destiny/Trainer/Ghost slabs, album/binder unpatch, HandleCards skip; Thunderstore package on release |
| **Imazen.WebP** | **10.0.1** | Place `deps/Imazen.WebP.dll` next to ExpansionMod DLL |
| **TextureReplacer** | 1.6.1+ | Required by Genobear |
| **ArtExpander** | Genobear **3.4.3** (+ `cardart.assets`) | Keep Genobear DLL on 0.70.3; `GhostCardPatch` Harmony error is non-fatal. Do not deploy ArtExpander-src 3.8.1 (wrong face sizing). |
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

## Log triage (0.70.3)

### Must fix

- ExpansionMod album close-up / binder sort / `HandleCards` using removed `CardUI.m_GhostCard` — fixed by **0703 Patch 1.1.039+** (Unpatch album/binder hooks; skip HandleCards)
- Graded Destiny/Trainer/Ghost album slabs empty / wrong size — fixed by **0703 Patch 1.2.4** (portrait GradedCardCase + GradedFace UI slab)
- Graded Destiny/Trainer/Ghost on shop display showing mirrored front instead of card back — fixed by **0703 Patch 1.2.6**
- ArtExpander: keep Genobear **3.4.3** — `GhostCardPatch` fails soft; ArtExpander-src 3.8.1 caused wrong card face sizes
- Missing `Imazen.WebP` next to ExpansionMod — install from `deps/Imazen.WebP.dll`
- `CardOpeningSequence` pack refs `animator=False, mesh=False` after sharedassets port — UI fan may work; pack wrapper animation needs scene object restore (see TROUBLESHOOTING)

### Worth watching

- `GradedCardSetCheckStatusScreen` oversized set — clamped by 0703 Patch 1.1.039+
- SteamAPI init failed — ignore unless launched outside Steam and Steam features are required

### Safe to ignore

- `ArtExpander ... animated.assets not found`
- RectTransform parent spam
- `SetLoadData` null-ref infos
- DontDestroyOnLoad warnings
- BoxCollider negative scale

## Success checklist

- [ ] Save loads (no “Shelf data not loaded properly”)
- [ ] Log shows `0703 Patch` **1.2.7**, album/binder unpatched, and Genobear ArtExpander 3.4.3 (GhostCardPatch warning OK)
- [ ] Album close-up and binder sort open without ExpansionMod `MissingFieldException` / NRE spam
- [ ] Tetramon pack rip: stacked back → flip each card → fan row
- [ ] Shop display case: fronts toward customer, backs from behind
- [ ] Binder/album pages show correct card faces
- [ ] Graded Destiny/Trainer/Ghost slabs fill binder pockets with readable grade text
