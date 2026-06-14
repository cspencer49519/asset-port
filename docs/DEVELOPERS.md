# Developers — build and release

## Repository layout

| Path | Purpose |
|------|---------|
| `TCGShopExpansionMod071Patch/` | Harmony patch source (C#) |
| `manifest.json` | Version pins and paths for scripts |
| `scripts/` | Install, verify, release build |
| `port_*.py` | Asset port tooling (maintainers only) |
| `docs/` | Player-facing install docs |

Game install and large binaries stay **outside git** (see `.gitignore`).

## Build patch DLL

Requires game at `../TCG Card Shop Simulator/` (sibling to `asset-port`) for assembly references.

```powershell
dotnet build TCGShopExpansionMod071Patch\TCGShopExpansionMod071Patch.csproj -c Release
```

Output: `TCGShopExpansionMod071Patch\bin\Release\netstandard2.1\TCGShopExpansionMod071Patch.dll`

## Assemble release zip

```powershell
.\scripts\Build-Release.ps1
```

Creates `dist/TCG-071-Genobear-{version}/` with:

- `patches/TCGShopExpansionMod071Patch.dll`
- `manifest.json`, `docs/`, `scripts/` (six player scripts: `.ps1`, `.bat`, `.sh` install/verify + `read_manifest.py`)
- `assets/` (if `output/` or `assets/` contains ported sharedassets trio)

Zip `dist/TCG-071-Genobear-*` for GitLab release upload.

## Port sharedassets (maintainers)

1. Place 0.71 base trio in `base-071/`
2. Place 0.62 mod `sharedassets0.assets` in `mod-062/`
3. `pip install UnityPy Pillow`
4. `python port_sharedassets.py` or `python port_with_vanilla05.py`
5. Copy `output/*` to release `assets/` before `Build-Release.ps1`

## GitLab release

1. Bump `PluginVersion` in `Plugin.cs` and `manifest.json`
2. Tag: `git tag v1.0.50 && git push origin v1.0.50`
3. Run `Build-Release.ps1`, upload zip to GitLab Releases
4. Optionally attach pre-ported `assets` as separate download if zip is too large

## CI note

`.gitlab-ci.yml` builds the DLL when `lib/game/` contains reference assemblies copied from a local install (see `lib/game/README.md`). CI is optional; local `Build-Release.ps1` is the primary path.
