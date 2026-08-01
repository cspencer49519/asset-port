# Troubleshooting — 0.70.3 + Genobear + 0703 patch

Primary log: **`BepInEx/LogOutput.log`** (in the game folder).

Run the verifier first:

```powershell
.\scripts\Verify-TCG0703Install.ps1 -GamePath "YOUR_GAME_PATH"
```

```bat
scripts\Verify-TCG0703Install.bat "YOUR_GAME_PATH"
```

```bash
./scripts/Verify-TCG0703Install.sh --game-path "YOUR_GAME_PATH"
```

Full install guide: [INSTALL-0703.md](INSTALL-0703.md).

---

## Wrong or vanilla card frames

**Symptoms:** Game runs but cards use default borders/frames instead of Genobear style.

**Cause:** Ported `sharedassets0` trio not installed (used `-SkipAssets`, patch-only zip, or never ran install script).

**Fix:**

1. Re-run install script **without** `-SkipAssets` / `--skip-assets`.
2. Confirm release zip contains `assets/sharedassets0.assets` (~160+ MB, not ~47 MB).
3. Verify script should report `sharedassets0.assets looks ported`.
4. Restore from `_backup_sharedassets_*` only if a bad swap caused crashes — then re-run install.

---

## “Shelf data not loaded properly” / return to title

**Cause:** More Card Expansions 1.8.7 hits removed field `m_CardBorderList` on game 0.70.3.

**Fix:**

1. Confirm `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll` exists.
2. Log must show `TCGShopExpansionMod 0.70.3 Patch` and `Patched ExpansionMod for game 0.70.3`.
3. Re-run the install script if the patch is missing or outdated.

See also [SHELF_ERROR_FIX.md](../SHELF_ERROR_FIX.md).

---

## Patch not loading

| Symptom | Fix |
|---------|-----|
| No “0703 Patch” line in log | Install DLL to `BepInEx/plugins/TCGShopExpansionMod0703Patch/` |
| BepInEx not loading plugins | Reinstall BepInEx mod 27; run game once |
| ExpansionMod missing | Install Nexus mod 48 (v1.8.7); patch has hard dependency |
| `FileNotFoundException: MonoMod.Backports` | Copy `MonoMod.Backports.dll` and `MonoMod.ILHelpers.dll` from BepInEx pack into `BepInEx/core/` (some Wine/Mac installs) |

---

## Pokemon / Tetramon cards show vanilla art or icons only

**Cause:** ArtExpander or `cardart.assets` missing.

**Fix:**

1. `BepInEx/plugins/ArtExpander/cardart.assets` must exist (~15 GB).
2. Log should contain `ArtExpander bridge ready`.
3. Install Genobear HD art bundle per pack README.

---

## Game crash on launch after sharedassets change

**Cause:** Mixed game versions — e.g. Genobear 0.62 `.assets` with 0.70.3 `.resS`.

**Fix:**

1. Restore all three files from `Card Shop Simulator_Data/_backup_sharedassets_*`.
2. Re-install the **ported trio** from the release zip via install script.
3. Never copy only one file from a different game version.

---

## White screen / broken UI after asset swap

**Cause:** Corrupt or mis-paired `sharedassets0` trio (bad port, missing `.resS`, or UnityPy-broken port).

**Fix:**

1. Restore backup from `Card Shop Simulator_Data/_backup_sharedassets_*`.
2. Re-run install script using assets from a **trusted release zip** only.
3. Do not use homemade UnityPy `save()` ports for 0.70.3.

---

## Pack opening / display case visual bugs

Ensure patch version **1.2.8** or newer in log.

| Issue | Check |
|-------|--------|
| Wide stretched cards | Update to latest 0703 patch |
| No pack backs / blank white stack during rip | Patch **1.0.50+** resolves Tetramon back sprite via `GetCardBackSprite`; log should show `Pack back sprite resolved from:` |
| Cards invisible mid-flip, shadows over cards | Patch **1.0.50+** keeps already-revealed cards face-up during flip states |
| Display case wrong faces | Load save after patch install; check F1 ExpansionMod settings |
| Graded display back shows mirrored front art | Patch **1.2.6+**; log should show `Graded shelf back armed:` when a graded Destiny/Trainer/Ghost card is on a stand |
| Pack wrapper anim missing (`animator=False, mesh=False`) | Texture port can leave `CardOpeningSequence` pack mesh/animator refs null. UI card fan may still run. Log `Pack open readiness` and late-sync give-up lines. A/B: compare against `sharedassets0.assets.backup` (vanilla) — if animators only exist on vanilla, restore pack scene objects or re-port carefully. |

---

## Album close-up / binder sort spam (ExpansionMod)

**Symptoms:** `MissingFieldException: CardUI.m_GhostCard` on album zoom; `NullReferenceException` in `OpenSortAlbumScreen` postfix; hundreds of suppressed `SetCardUI` `MissingFieldException`s.

**Fix:** Install **0703 Patch 1.1.039+**. It unpatches ExpansionMod’s incompatible album/binder hooks and skips HandleCards on 0.70.3.

---

## Graded Destiny / Trainer / Ghost slabs wrong size or empty

**Symptoms:** Graded cards in the binder show empty slabs, tiny floating cases, fat/stretched art, or unreadable PSA grade text.

**Fix:**

1. Confirm log shows **`TCGShopExpansionMod 0.70.3 Patch 1.2.8`** (or newer).
2. Re-run the install script (patch-only is fine if sharedassets are already ported).
3. Keep Genobear **ArtExpander 3.4.3** — newer ArtExpander-src builds can wrong-size faces.
4. Optional: drop a real `GradedCardCase.png` into `BepInEx/plugins/TextureReplacer/objects_textures/` to replace the procedural slab chrome.

---

## Missing Imazen.WebP

**Symptoms:** `TypeLoadException` / Harmony reflection warning for `Imazen.WebP, Version=10.0.1.0`.

**Fix:** Copy `deps/Imazen.WebP.dll` to `BepInEx/plugins/TCGShopExpansionMod/` (next to `TCGShopExpansionMod.dll`). Native `libwebp` is only needed if you actually decode `.webp` custom art (Genobear PNGs do not require it).

---

## ArtExpander GhostCardPatch error in log

Genobear’s bundled ArtExpander **3.4.3** patches removed `CardUI.SetGhostCardUI` and fails Harmony load on 0.70.3 — **non-fatal**. Keep Genobear’s DLL; art still loads via `cardart.assets`.

Do **not** replace with ArtExpander-src 3.8.1 on this install — that build caused wrong card face sizes with Genobear `cardart.assets`.

`animated.assets not found` remains optional/safe to ignore.

---

## F1 / ExpansionMod settings not visible

Install [Configuration Manager](https://www.nexusmods.com/tcgcardshopsimulator/mods/31) (Nexus mod 31), then press **F1** in-game.

---

## Getting help

Include:

1. Patch version line from log
2. Output of verify script
3. Last ~100 lines of `LogOutput.log` around first error
4. [VERSION_MATRIX.md](VERSION_MATRIX.md) versions you installed
5. Whether verify reported ported vs vanilla `sharedassets0.assets` size
