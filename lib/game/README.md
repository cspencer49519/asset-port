# Reference assemblies for GitLab CI

Copy these from your game install into this folder to enable **CI builds and automated release packaging**.

## Quick setup (local or CI runner)

```bash
GAME_PATH="/path/to/TCG Card Shop Simulator" ./scripts/Populate-LibGame.sh
```

Default `GAME_PATH` is `../TCG Card Shop Simulator` relative to this repo (sibling folder).

## Required files

| File | Source |
|------|--------|
| `Assembly-CSharp.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.CoreModule.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.UI.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.UIModule.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.ImageConversionModule.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.AnimationModule.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.ParticleSystemModule.dll` | `Card Shop Simulator_Data/Managed/` |
| `TCGShopExpansionMod.dll` | `BepInEx/plugins/TCGShopExpansionMod/` |

These files are gitignored. The csproj prefers `lib/game/` when present; otherwise it uses the local game install path.

## CI behavior

| Pipeline | `lib/game/` present | Result |
|----------|---------------------|--------|
| `main` | Yes | `build-patch` compiles DLL |
| `main` | No | `ci-build-skipped` passes (no empty artifact upload) |
| `v*.*.*` tag | Yes | Full release pipeline: build → zip → upload → GitLab Release |
| `v*.*.*` tag | No | `build-patch` fails with setup instructions |

Populate `lib/game/` once on your GitLab runner host (or use a persistent runner volume) so tag pipelines can publish releases automatically.
