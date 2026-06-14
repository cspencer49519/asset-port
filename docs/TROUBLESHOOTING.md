# Troubleshooting — 0.71 + Genobear + 071 patch

Primary log file: **`BepInEx/LogOutput.log`** (in the game folder).

Run the verifier first (pick your shell):

```powershell
.\scripts\Verify-TCG071Install.ps1 -GamePath "YOUR_GAME_PATH"
```

```bat
scripts\Verify-TCG071Install.bat "YOUR_GAME_PATH"
```

```bash
./scripts/Verify-TCG071Install.sh --game-path "YOUR_GAME_PATH"
```

## “Shelf data not loaded properly” / return to title

**Cause:** More Card Expansions 1.8.7 hits removed field `m_CardBorderList` on game 0.71.

**Fix:**

1. Confirm `BepInEx/plugins/TCGShopExpansionMod071Patch/TCGShopExpansionMod071Patch.dll` exists.
2. Log must show `TCGShopExpansionMod 0.71 Patch` and `Patched ExpansionMod for game 0.71`.
3. Re-run the install script (`Install-TCG071Mods.ps1`, `.bat`, or `.sh`) if the patch is missing or outdated.

See also [SHELF_ERROR_FIX.md](../SHELF_ERROR_FIX.md).

## Patch not loading

| Symptom | Fix |
|---------|-----|
| No “071 Patch” line in log | Install DLL to `BepInEx/plugins/TCGShopExpansionMod071Patch/` |
| BepInEx not loading plugins | Reinstall BepInEx mod 27; run game once |
| ExpansionMod missing | Install Nexus mod 48; patch has hard dependency |

## Pokemon cards show vanilla art / icons only

**Cause:** ArtExpander or `cardart.assets` missing.

**Fix:**

1. `BepInEx/plugins/ArtExpander/cardart.assets` must exist (~15 GB).
2. Log should contain `ArtExpander bridge ready`.
3. Install Genobear HD art bundle per pack README.

## Pack opening / display case visual bugs

Ensure patch version **1.0.49** or newer in log.

| Issue | Check |
|-------|--------|
| Wide stretched cards | Update to latest 071 patch |
| No pack backs | ExpansionMod + patch both loaded |
| Display case wrong faces | Load save after patch install |

## White screen / broken UI after asset swap

**Cause:** Wrong or incomplete `sharedassets0` trio (bad port or missing `.resS`).

**Fix:**

1. Restore backup from `Card Shop Simulator_Data/_backup_sharedassets_*`.
2. Re-install only the **ported trio** from a trusted release zip.
3. Do not mix 0.62 `.assets` with 0.71 `.resS` files.

## ArtExpander GhostCardPatch error

Harmony patch mismatch with 0.71 — **expected**. Full card art still works if `cardart.assets` is present.

## Getting help

Include:

1. Patch version line from log
2. Output of `Verify-TCG071Install.ps1`
3. Last 100 lines of `LogOutput.log` around first error
4. [VERSION_MATRIX.md](VERSION_MATRIX.md) versions you installed
