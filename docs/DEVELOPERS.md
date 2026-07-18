# Developers — build and release

## Repository layout

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod0703Patch/` | Harmony patch source (C#) |
| `manifest.json` | Version pins and paths for scripts |
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

## Assemble release zip

```powershell
.\scripts\Build-Release.ps1
```

Creates `dist/TCG-0703-Genobear-{version}/` with:

- `patches/TCGShopExpansionMod0703Patch.dll`
- `manifest.json`, `docs/`, `scripts/` (six player scripts: `.ps1`, `.bat`, `.sh` install/verify + `read_manifest.py`)
- `assets/` (if `output/` or `assets/` contains ported sharedassets trio)

Zip `dist/TCG-0703-Genobear-*` for GitLab release upload.

## Port sharedassets (maintainers)

Players receive a **pre-ported** trio in release `assets/`. Maintainers regenerate it when Genobear sharedassets change.

1. Place vanilla 0.70.3 trio in `base-0703/` (from `Card Shop Simulator_Data/`)
2. Place Genobear 0.62 `sharedassets0.assets` in `mod-062/`
3. Set up venv: `python3 -m venv .venv-port && .venv-port/bin/pip install UnityPy Pillow pythonnet`
4. On macOS/Linux: `export DOTNET_ROOT=...` and `export PYTHONNET_RUNTIME=coreclr` (see `tools/atnet/`)
5. Run: `.venv-port/bin/python port_assets_tools.py` (UnityPy export + AssetsTools.NET write)
6. **Do not** use `port_sharedassets.py` UnityPy `save()` for 0.70.3 — it breaks `.resS` pairing (white screen)
7. Copy `output/*` to `assets/` before `Build-Release.ps1`

Expected output: ~16 card-frame textures applied, `sharedassets0.assets` ~160+ MB, object count unchanged (548).

## GitLab release

1. Bump `PluginVersion` in `Plugin.cs` and `manifest.json`
2. Tag: `git tag v1.0.50 && git push origin v1.0.50`
3. Run `Build-Release.ps1`, upload zip to GitLab Releases
4. Optionally attach pre-ported `assets` as separate download if zip is too large

## CI note

`.gitlab-ci.yml` builds the DLL when `lib/game/` contains reference assemblies copied from a local install (see `lib/game/README.md`). CI is optional; local `Build-Release.ps1` is the primary path.
