#!/usr/bin/env bash
# Builds the patch DLL and assembles dist/TCG-071-Genobear-{version}.zip for release upload.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"
SKIP_BUILD=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-build) SKIP_BUILD=1; shift ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

VERSION="$(python3 -c "import json; print(json.load(open('$REPO_ROOT/manifest.json'))['patchVersion'])")"
DIST_NAME="TCG-071-Genobear-$VERSION"
DIST_ROOT="$REPO_ROOT/dist/$DIST_NAME"
ZIP_PATH="$REPO_ROOT/dist/$DIST_NAME.zip"
CSPROJ="$REPO_ROOT/TCGShopExpansionMod071Patch/TCGShopExpansionMod071Patch.csproj"
BUILT_DLL="$REPO_ROOT/TCGShopExpansionMod071Patch/bin/$CONFIGURATION/netstandard2.1/TCGShopExpansionMod071Patch.dll"

if [[ "$SKIP_BUILD" -eq 0 ]]; then
  echo "Building TCGShopExpansionMod071Patch..."
  dotnet build "$CSPROJ" -c "$CONFIGURATION" -v minimal
fi

if [[ ! -f "$BUILT_DLL" ]]; then
  echo "Built DLL not found: $BUILT_DLL" >&2
  exit 1
fi

rm -rf "$DIST_ROOT"
mkdir -p "$DIST_ROOT"/{patches,scripts,docs,assets}

cp "$BUILT_DLL" "$DIST_ROOT/patches/TCGShopExpansionMod071Patch.dll"
cp "$REPO_ROOT/manifest.json" "$DIST_ROOT/manifest.json"
cp "$REPO_ROOT"/docs/* "$DIST_ROOT/docs/" 2>/dev/null || true

for script in Install-TCG071Mods.ps1 Verify-TCG071Install.ps1 Install-TCG071Mods.bat \
  Verify-TCG071Install.bat Install-TCG071Mods.sh Verify-TCG071Install.sh read_manifest.py; do
  if [[ -f "$REPO_ROOT/scripts/$script" ]]; then
    cp "$REPO_ROOT/scripts/$script" "$DIST_ROOT/scripts/"
  fi
done

COPIED_ASSETS=0
for src in "$REPO_ROOT/assets" "$REPO_ROOT/output"; do
  [[ -d "$src" ]] || continue
  for f in sharedassets0.assets sharedassets0.assets.resS sharedassets0.resource; do
    if [[ -f "$src/$f" ]]; then
      cp "$src/$f" "$DIST_ROOT/assets/"
      COPIED_ASSETS=1
    fi
  done
  [[ "$COPIED_ASSETS" -eq 1 ]] && break
done

rm -f "$ZIP_PATH"
(cd "$REPO_ROOT/dist" && zip -rq "$(basename "$ZIP_PATH")" "$DIST_NAME")

echo ""
echo "Release folder: $DIST_ROOT"
echo "Release zip:    $ZIP_PATH"
if [[ "$COPIED_ASSETS" -eq 0 ]]; then
  echo "Note: No sharedassets trio in assets/ or output/ — patch-only zip."
fi
