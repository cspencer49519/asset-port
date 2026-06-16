#!/usr/bin/env bash
# Copy game reference DLLs into lib/game/ for CI and headless builds.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GAME_PATH="${GAME_PATH:-$(cd "$REPO_ROOT/../TCG Card Shop Simulator" 2>/dev/null && pwd || true)}"
DEST="$REPO_ROOT/lib/game"

if [[ -z "$GAME_PATH" || ! -d "$GAME_PATH" ]]; then
  echo "Set GAME_PATH to your TCG Card Shop Simulator install directory." >&2
  exit 1
fi

MANAGED="$GAME_PATH/Card Shop Simulator_Data/Managed"
EXPANSION="$GAME_PATH/BepInEx/plugins/TCGShopExpansionMod/TCGShopExpansionMod.dll"

for f in Assembly-CSharp.dll UnityEngine.dll UnityEngine.CoreModule.dll UnityEngine.UI.dll \
  UnityEngine.UIModule.dll UnityEngine.ImageConversionModule.dll UnityEngine.AnimationModule.dll \
  UnityEngine.ParticleSystemModule.dll; do
  if [[ ! -f "$MANAGED/$f" ]]; then
    echo "Missing: $MANAGED/$f" >&2
    exit 1
  fi
done

if [[ ! -f "$EXPANSION" ]]; then
  echo "Missing: $EXPANSION" >&2
  exit 1
fi

mkdir -p "$DEST"
cp "$MANAGED"/Assembly-CSharp.dll "$MANAGED"/UnityEngine.dll "$MANAGED"/UnityEngine.CoreModule.dll \
  "$MANAGED"/UnityEngine.UI.dll "$MANAGED"/UnityEngine.UIModule.dll \
  "$MANAGED"/UnityEngine.ImageConversionModule.dll "$MANAGED"/UnityEngine.AnimationModule.dll \
  "$MANAGED"/UnityEngine.ParticleSystemModule.dll "$DEST/"
cp "$EXPANSION" "$DEST/TCGShopExpansionMod.dll"

echo "Copied reference DLLs to $DEST"
ls -la "$DEST"
