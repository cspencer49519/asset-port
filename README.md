# TCG Card Shop Simulator — 0.71 Mod Tooling

Tools and compatibility patches for running mod packs (e.g. Genobear Real TCG Overhaul) on **TCG Card Shop Simulator 0.71**.

This repo tracks **source and scripts only**. Game installs, mod packs, and Unity `.assets` binaries stay on your machine outside git.

## Contents

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod071Patch/` | BepInEx Harmony patch — makes More Card Expansions work on 0.71 |
| `port_with_vanilla05.py` | Selective `sharedassets0.assets` texture port (AssetsTools.NET) |
| `port_*.py`, `probe_*.py` | Asset inspection and port helpers |
| `GENOBEAR_REQUIREMENTS.md` | Mod compatibility notes |
| `SHELF_ERROR_FIX.md` | ExpansionMod shelf-load crash diagnosis |
| `ArtExpander-src/` | Optional ArtExpander reference source |

## Build the 0.71 patch

Requires the game installed at `../TCG Card Shop Simulator/` (sibling to this folder).

```powershell
dotnet build TCGShopExpansionMod071Patch\TCGShopExpansionMod071Patch.csproj -c Release
```

Copy `TCGShopExpansionMod071Patch\bin\Release\netstandard2.1\TCGShopExpansionMod071Patch.dll` to:

`../TCG Card Shop Simulator/BepInEx/plugins/TCGShopExpansionMod071Patch/`

## Local asset port workflow

1. Place reference files in ignored folders (`base-071/`, `vanilla-05/`, etc.) — see `.gitignore`.
2. Run `python port_with_vanilla05.py` (requires Python 3.11+, UnityPy, Pillow, AssetsTools.NET).

## Remote

GitLab: `git@192.168.0.50:tcg-cardshopmods/asset-port.git`
