# Developers — build and release

## Repository layout

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod0703Patch/` | Harmony patch source (C#) |
| `manifest.json` | Version pins and paths for scripts |
| `thunderstore/` | Thunderstore package metadata (`icon.png`, `README.md`, `manifest.json` template) |
| `scripts/` | Install, verify, release build |
| `port_*.py` | Asset port tooling (maintainers only) |
| `docs/` | Player-facing install docs |

Game install and large binaries stay **outside git** (see `.gitignore`).

## Build patch DLL

Requires game at `../TCG Card Shop Simulator/` (sibling to `asset-port`) for assembly references.

```powershell
dotnet build TCGShopExpansionMod0703Patch\TCGShopExpansionMod0703Patch.csproj -c Release
```

Output: `TCGShopExpansionMod0703Patch\bin\Release\netstandard2.1\TCGShopExpansionMod0703Patch.dll`

## Assemble release zips

```powershell
.\scripts\Build-Release.ps1
```

Requires the ported sharedassets trio in `assets/` or `output/` (Thunderstore packaging fails without it).

Creates:

1. **Installer zip** — `dist/TCG-0703-Genobear-{version}.zip` with nested folder:
   - `patches/TCGShopExpansionMod0703Patch.dll`
   - `manifest.json`, `docs/`, `scripts/` (six player scripts: `.ps1`, `.bat`, `.sh` install/verify + `read_manifest.py`)
   - `assets/` (ported sharedassets trio)

2. **Thunderstore zip** — `dist/TCGPatch-TCGShopExpansionMod_0703_Patch-{version}.zip` (flat root, no wrapping folder):
   - `icon.png`, `README.md`, Thunderstore `manifest.json`
   - `BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll`
   - `Card Shop Simulator_Data/sharedassets0.assets` (+ `.resS`, `.resource`)

Thunderstore Mod Manager installs the DLL under `BepInEx/plugins` correctly, but does **not** place files into `Card Shop Simulator_Data/`. Players must copy the sharedassets trio manually (documented in `thunderstore/README.md`). Upload the Thunderstore zip manually to thunderstore.io under team **TCGPatch** / package **TCGShopExpansionMod_0703_Patch**.

## Port sharedassets (maintainers)

Players receive a **pre-ported** trio in release `assets/` and in the Thunderstore Data folder. Maintainers regenerate it when Genobear sharedassets change.

1. Place vanilla 0.70.3 trio in `base-0703/` (from `Card Shop Simulator_Data/`)
2. Place Genobear 0.62 `sharedassets0.assets` in `mod-062/`
3. Set up venv: `python3 -m venv .venv-port && .venv-port/bin/pip install UnityPy Pillow pythonnet`
4. On macOS/Linux: `export DOTNET_ROOT=...` and `export PYTHONNET_RUNTIME=coreclr` (see `tools/atnet/`)
5. Run: `.venv-port/bin/python port_assets_tools.py` (UnityPy export + AssetsTools.NET write)
6. **Do not** use `port_sharedassets.py` UnityPy `save()` for 0.70.3 — it breaks `.resS` pairing (white screen)
7. Copy `output/*` to `assets/` before `Build-Release.ps1`

Expected output: ~16 card-frame textures applied, `sharedassets0.assets` ~160+ MB, object count unchanged (548).

## GitLab release

1. Bump `PluginVersion` in `Plugin.cs` and `patchVersion` in `manifest.json` (keep them identical).
2. Update `docs/START_HERE.md`, `docs/INSTALL-0703.md`, `docs/VERSION_MATRIX.md`, and add `docs/release-notes/vX.Y.Z.md`.
3. Ensure ported sharedassets are in `assets/` or `output/`.
4. Commit and push to `main`.
5. Build and publish:

```powershell
.\scripts\Build-Release.ps1
$env:GITLAB_TOKEN = 'glpat-...'   # Personal Access Token with api scope
.\scripts\Publish-GitLabRelease.ps1
```

`Publish-GitLabRelease.ps1` creates annotated tag `vX.Y.Z`, GitLab release page, and uploads **full**, **patch-only**, and **Thunderstore** zips via the package registry.

Players should open **`docs/START_HERE.md`** inside the zip first.

## CI note

`.gitlab-ci.yml` builds the DLL when `lib/game/` contains reference assemblies (see `lib/game/README.md`). Tagged releases also package the patch-only installer zip and the Thunderstore zip; the runner must have the sharedassets trio under `assets/` or `output/`. The Thunderstore zip is uploaded via the generic package registry (large); the patch-only zip uses project uploads. CI is optional; local `Build-Release.ps1` is the primary path.
