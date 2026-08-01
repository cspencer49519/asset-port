# Start here — install in 10 minutes

This package makes **Genobear Real TCG Overhaul** work on **TCG Card Shop Simulator 0.70.3**.

**Current patch:** **1.2.7**

---

## Already have Nexus + Genobear installed?

Jump to **[Run the installer](#3-run-the-installer)** below.

---

## 1. Install the game stack (once)

| Step | What | Link |
|------|------|------|
| 1 | Steam game **0.70.3** | Launch once, quit |
| 2 | **BepInEx** | [Nexus mod 27](https://www.nexusmods.com/tcgcardshopsimulator/mods/27) |
| 3 | **Add New Cards** | [Nexus mod 3](https://www.nexusmods.com/tcgcardshopsimulator/mods/3) |
| 4 | **TextureReplacer** | [Nexus mod 26](https://www.nexusmods.com/tcgcardshopsimulator/mods/26) |
| 5 | **More Card Expansions 1.8.7** | [Nexus mod 48](https://www.nexusmods.com/tcgcardshopsimulator/mods/48) |
| 6 | **Genobear** (especially `ArtExpander` + ~15 GB `cardart.assets`) | Genobear / MEGA pack README |

Optional but recommended: [Configuration Manager](https://www.nexusmods.com/tcgcardshopsimulator/mods/31) (**F1** in-game settings).

**Do not** copy Genobear’s raw 0.62 `sharedassets0` into 0.70.3 — this zip’s installer provides the ported trio.

---

## 2. Extract this zip

Unzip **TCG-0703-Genobear-1.2.7** anywhere (e.g. Downloads). You should see:

```
TCG-0703-Genobear-1.2.7/
  patches/TCGShopExpansionMod0703Patch.dll
  assets/sharedassets0.*     ← full zip only
  scripts/Install-TCG0703Mods.*
  scripts/Verify-TCG0703Install.*
  docs/
  manifest.json
```

---

## 3. Run the installer

Replace the game path with yours.

### Windows (PowerShell — easiest)

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd "D:\Downloads\TCG-0703-Genobear-1.2.7"
.\scripts\Install-TCG0703Mods.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
.\scripts\Verify-TCG0703Install.ps1 -GamePath "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

### Windows (CMD)

```bat
cd /d D:\Downloads\TCG-0703-Genobear-1.2.7
scripts\Install-TCG0703Mods.bat "D:\Steam\steamapps\common\TCG Card Shop Simulator"
scripts\Verify-TCG0703Install.bat "D:\Steam\steamapps\common\TCG Card Shop Simulator"
```

### Linux / macOS

```bash
cd ~/Downloads/TCG-0703-Genobear-1.2.7
chmod +x scripts/*.sh
./scripts/Install-TCG0703Mods.sh --game-path "$HOME/.steam/steam/steamapps/common/TCG Card Shop Simulator"
./scripts/Verify-TCG0703Install.sh --game-path "$HOME/.steam/steam/steamapps/common/TCG Card Shop Simulator"
```

**Patch-only upgrade** (you already have ported sharedassets from an older full release):

```powershell
.\scripts\Install-TCG0703Mods.ps1 -GamePath "YOUR_GAME_PATH" -SkipAssets
```

---

## 4. Launch and check

1. Start the game from Steam.
2. Open `BepInEx/LogOutput.log` — look for:

```
TCGShopExpansionMod 0.70.3 Patch 1.2.7
Patched ExpansionMod for game 0.70.3
```

3. **F1** → **com.DarkDragoon.TCGShopExpansionMod** → turn on custom images/configs (see [VERSION_MATRIX.md](VERSION_MATRIX.md)).
4. Smoke test: load save → open a Tetramon pack → check display case → open binder graded Destiny/Trainer pages.

---

## Need more detail?

| Doc | When |
|-----|------|
| [INSTALL-0703.md](INSTALL-0703.md) | Full step-by-step + flags + manual install |
| [VERSION_MATRIX.md](VERSION_MATRIX.md) | Exact pinned versions + F1 settings |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Log errors and common fixes |
| [release-notes/v1.2.7.md](release-notes/v1.2.7.md) | What changed in this release |
