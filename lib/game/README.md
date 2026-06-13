# Reference assemblies for GitLab CI (optional)

Copy these from your game install into this folder to enable remote builds:

| File | Source |
|------|--------|
| `Assembly-CSharp.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.CoreModule.dll` | `Card Shop Simulator_Data/Managed/` |
| `UnityEngine.UI.dll` | `Card Shop Simulator_Data/Managed/` |
| `TCGShopExpansionMod.dll` | `BepInEx/plugins/TCGShopExpansionMod/` |

These files are gitignored. CI falls back to skipping the build job if they are absent.
