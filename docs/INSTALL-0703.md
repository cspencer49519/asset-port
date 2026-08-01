# Install — Genobear / Real TCG Overhaul on game 0.70.3

Step-by-step guide for **players**. No coding required.

**Tested stack:** TCG Card Shop Simulator **0.70.3** + Genobear Real TCG Overhaul + **TCGShopExpansionMod0703Patch 1.2.6**.

**New installers:** start with **[START_HERE.md](START_HERE.md)** (short checklist), then return here if something fails.

---

## TL;DR — four phases

| Phase | What | Who installs it |
|-------|------|-----------------|
| **1** | Game + BepInEx | You (Steam + [Nexus mod 27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27)) |
| **2** | Core Nexus mods | You (mods 3, 26, 48) |
| **3** | Genobear pack + `cardart.assets` (~15 GB) | You (Genobear / MEGA) |
| **4** | **0.70.3 patch + ported sharedassets** | **Our release zip + install script** |

The install script copies the compatibility patch DLL and the **pre-ported `sharedassets0` trio** into your game. You do **not** need to run Python or port assets yourself.

### Already on an older 0703 patch?

If you already ran a **full** install once (ported sharedassets are in place), download the **patch-only** zip and run the installer with `-SkipAssets` / `--skip-assets`. That only refreshes the DLL.

---

## What the release zip contains

Download from GitLab Releases:

| Zip | Contents |
|-----|----------|
| **TCG-0703-Genobear-1.2.6.zip** | Full: patch + `assets/` + scripts + docs |
| **TCG-0703-Genobear-1.2.6-patch-only.zip** | Patch DLL + scripts + docs (no sharedassets) |

| Folder / file | Purpose |
|---------------|---------|
| `patches/TCGShopExpansionMod0703Patch.dll` | Makes ExpansionMod 1.8.7 work on game 0.70.3 (graded slabs, album, shelf, pack) |
| `assets/sharedassets0.*` | **Pre-ported** card-frame textures for 0.70.3 (required for Genobear frames) — full zip only |
| `scripts/Install-TCG0703Mods.*` | Copies patch + sharedassets into your game (backs up originals) |
| `scripts/Verify-TCG0703Install.*` | Checks files and log markers |
| `docs/` | This guide, START_HERE, troubleshooting, version pins, release notes |

If the zip has **no `assets/` folder**, use it only as a patch upgrade, or get the **full** zip for first-time frame install.

---

## What you install manually (before the script)

### Required Nexus mods

Install into `BepInEx/plugins/` per each mod’s README.

| Mod | Nexus | Folder (typical) |
|-----|-------|------------------|
| BepInEx Pack | [mod 27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27) | Game root (`BepInEx/`) |
| Add New Cards Mod | [mod 3](https://www.nexusmods.com/tcgcardshopsimulator/mods/3) | `BepInEx/plugins/TCGShopNewCardsMod/` |
| More Card Expansions **1.8.7** | [mod 48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) | `BepInEx/plugins/TCGShopExpansionMod/` |
| TextureReplacer | [mod 26](https://www.nexusmods.com/tcgcardshopsimulator/mods/26) | `BepInEx/plugins/TextureReplacer/` |

### Genobear Real TCG Overhaul

Follow the Genobear pack README. At minimum you need:

| Item | Location |
|------|----------|
| `ArtExpander.dll` (**3.4.3** Genobear) | `BepInEx/plugins/ArtExpander/` |
| **`cardart.assets` (~15 GB)** | `BepInEx/plugins/ArtExpander/` |
| New Cards / expansion data | Per Genobear README |
| Config / image packs | Per Genobear README |

**Do not** copy Genobear’s raw `sharedassets0.assets` from the 0.62 pack into game 0.70.3. Use the **ported trio from our release** (phase 4).

### Recommended (not required)

| Mod | Why |
|-----|-----|
| [Configuration Manager](https://www.nexusmods.com/tcgcardshopsimulator/mods/31) | In-game **F1** menu for ExpansionMod settings |
| [Holographic Overhaul](https://www.nexusmods.com/tcgcardshopsimulator/mods/44) | Foil effects (Genobear foil textures are not ported into sharedassets) |

---

## Full install order

1. Install **TCG Card Shop Simulator 0.70.3** from Steam.
2. Launch the game **once**, then quit (creates `BepInEx` folders on first BepInEx install).
3. Install **BepInEx** ([mod 27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27)) into the game folder.
4. Install Nexus mods **3**, **26**, and **48** (ExpansionMod **1.8.7**).
5. Install **Genobear** — especially `cardart.assets` under `ArtExpander/`. Keep ArtExpander **3.4.3**.
6. (Recommended) Install **Configuration Manager** for F1 settings.
7. Download and extract **TCG-0703-Genobear-1.2.6.zip** anywhere (e.g. `Downloads/TCG-0703-Genobear-1.2.6`).
8. Run the **install script** (see platform section below). **Do not** use `--skip-assets` / `-SkipAssets` on a normal first install.
9. Run the **verify script** on the same game path.
10. Launch the game. Open config with **F1** → **com.DarkDragoon.TCGShopExpansionMod** → set values in [VERSION_MATRIX.md](VERSION_MATRIX.md).
11. **Smoke test:** load a save → open a **Tetramon** pack → check **display case** → open binder **graded Destiny/Trainer** pages.

---

## Run the install script

Pick **one** platform. Replace the path with your real Steam game folder.

### Windows — PowerShell (recommended)

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd "D:\Downloads\TCG-0703-Genobear-1.2.6"
.\scripts\Install-TCG0703Mods.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
.\scripts\Verify-TCG0703Install.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

### Windows — CMD

```bat
cd /d D:\Downloads\TCG-0703-Genobear-1.2.6
scripts\Install-TCG0703Mods.bat "D:\Steam\steamapps\common\TCG Card Shop Simulator"
scripts\Verify-TCG0703Install.bat "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

Requires `python` or `py -3` on PATH.

### Linux / macOS (Steam / Proton / CrossOver)

```bash
cd ~/Downloads/TCG-0703-Genobear-1.2.6
chmod +x scripts/*.sh
./scripts/Install-TCG0703Mods.sh --game-path "$HOME/.steam/steam/steamapps/common/TCG Card Shop Simulator"
./scripts/Verify-TCG0703Install.sh --game-path "$HOME/.steam/steam/steamapps/common/TCG Card Shop Simulator"
```

- Linux Steam may use `~/.local/share/Steam/steamapps/common/...` — the script auto-detects if you omit `--game-path`.
- macOS: `~/Library/Application Support/Steam/steamapps/common/TCG Card Shop Simulator`

### Patch-only upgrade

```powershell
.\scripts\Install-TCG0703Mods.ps1 -GamePath "YOUR_GAME_PATH" -SkipAssets -Force
.\scripts\Verify-TCG0703Install.ps1 -GamePath "YOUR_GAME_PATH"
```

---

## What the installer does

| Action | Details |
|--------|---------|
| Installs patch DLL | `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll` |
| Installs sharedassets trio | `Card Shop Simulator_Data/sharedassets0.assets`, `.assets.resS`, `.resource` |
| Backs up originals | `Card Shop Simulator_Data/_backup_sharedassets_YYYYMMDD-HHMMSS/` |
| Does **not** install | Nexus mods, Genobear art, BepInEx itself |

### Install flags

| PowerShell | CMD | bash | Effect |
|------------|-----|------|--------|
| `-GamePath` | first arg or `/GamePath` | `--game-path` or first arg | Path to game root |
| `-SkipAssets` | `/SkipAssets` | `--skip-assets` | Patch DLL only — **card frames stay as-is** |
| `-WhatIf` | `/WhatIf` | `--dry-run` | Show actions without copying |
| `-Force` | `/Force` | `--force` | Overwrite patch DLL without prompt |

---

## Manual install (no script)

1. Create `BepInEx/plugins/TCGShopExpansionMod0703Patch/`.
2. Copy `patches/TCGShopExpansionMod0703Patch.dll` from the release zip into that folder.
3. Back up then replace all **three** files in `Card Shop Simulator_Data/` from release `assets/`:
   - `sharedassets0.assets`
   - `sharedassets0.assets.resS`
   - `sharedassets0.resource`

All three must come from the **same ported release**. Never mix 0.62 `.assets` with 0.70.3 `.resS`.

---

## After install — log checks

Open `BepInEx/LogOutput.log`. You should see:

```
TCGShopExpansionMod 0.70.3 Patch 1.2.6
Patched ExpansionMod for game 0.70.3
ArtExpander bridge ready
```

### ExpansionMod settings (F1)

Under **com.DarkDragoon.TCGShopExpansionMod** (requires Configuration Manager or BepInEx config UI):

| Setting | Value |
|---------|-------|
| Access other card expansions | **true** |
| Enable custom card images for new expansions | **true** |
| Enable custom configs for new expansions | **true** |
| Enable custom card images for original expansions | **true** |
| Enable custom configs for original expansions | **true** |
| Enable custom images for cards on play tables | **false** |

Full matrix: [VERSION_MATRIX.md](VERSION_MATRIX.md).

### Success checklist

- [ ] Save loads (no “Shelf data not loaded properly”)
- [ ] Tetramon pack: correct card art and Genobear-style frames
- [ ] Display case: correct fronts/backs
- [ ] Binder: graded Destiny/Trainer/Ghost slabs fill pockets; grade text readable; art centered
- [ ] Verify script passes (warnings only for optional items you skipped)

---

## Common mistakes

| Mistake | Result |
|---------|--------|
| Skipping our install script / using `-SkipAssets` on first install | Vanilla card frames, wrong borders |
| Copying Genobear 0.62 `sharedassets0` into 0.70.3 | Crash or white screen |
| Missing `cardart.assets` | Pokémon/Tetramon art missing or icons only |
| ExpansionMod not 1.8.7 | Shelf load crash without patch |
| Only replacing `.assets`, not `.resS` + `.resource` | White screen or broken UI |
| Replacing Genobear ArtExpander 3.4.3 with ArtExpander-src builds | Wrong card face sizes |

Fixes: [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

---

## Developers

Build from source and asset porting: [DEVELOPERS.md](DEVELOPERS.md) (maintainers only).
Release notes: [release-notes/v1.2.6.md](release-notes/v1.2.6.md).
