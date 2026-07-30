#!/usr/bin/env bash
# Builds the patch DLL and assembles dist/TCG-0703-Genobear-{version}.zip for release upload.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"
SKIP_BUILD=0
PATCH_ONLY=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-build) SKIP_BUILD=1; shift ;;
    --patch-only) PATCH_ONLY=1; shift ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

VERSION="$(python3 -c "import json; print(json.load(open('$REPO_ROOT/manifest.json'))['patchVersion'])")"
DIST_NAME="TCG-0703-Genobear-$VERSION"
if [[ "$PATCH_ONLY" -eq 1 ]]; then
  DIST_NAME="${DIST_NAME}-patch-only"
fi
DIST_ROOT="$REPO_ROOT/dist/$DIST_NAME"
ZIP_PATH="$REPO_ROOT/dist/$DIST_NAME.zip"
CSPROJ="$REPO_ROOT/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.csproj"
BUILT_DLL="$REPO_ROOT/TCGShopExpansionMod0703Patch/bin/$CONFIGURATION/netstandard2.1/TCGShopExpansionMod0703Patch.dll"

if [[ "$SKIP_BUILD" -eq 0 ]]; then
  echo "Building TCGShopExpansionMod0703Patch..."
  dotnet build "$CSPROJ" -c "$CONFIGURATION" -v minimal
fi

if [[ ! -f "$BUILT_DLL" ]]; then
  echo "Built DLL not found: $BUILT_DLL" >&2
  exit 1
fi

rm -rf "$DIST_ROOT"
mkdir -p "$DIST_ROOT"/{patches,scripts,docs,assets}

cp "$BUILT_DLL" "$DIST_ROOT/patches/TCGShopExpansionMod0703Patch.dll"
cp "$REPO_ROOT/manifest.json" "$DIST_ROOT/manifest.json"
cp "$REPO_ROOT"/docs/* "$DIST_ROOT/docs/" 2>/dev/null || true

for script in Install-TCG0703Mods.ps1 Verify-TCG0703Install.ps1 Install-TCG0703Mods.bat \
  Verify-TCG0703Install.bat Install-TCG0703Mods.sh Verify-TCG0703Install.sh read_manifest.py; do
  if [[ -f "$REPO_ROOT/scripts/$script" ]]; then
    cp "$REPO_ROOT/scripts/$script" "$DIST_ROOT/scripts/"
  fi
done

COPIED_ASSETS=0
if [[ "$PATCH_ONLY" -eq 0 ]]; then
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
fi

rm -f "$ZIP_PATH"
(cd "$REPO_ROOT/dist" && zip -rq "$(basename "$ZIP_PATH")" "$DIST_NAME")

# Thunderstore package: flat zip root (independent of --patch-only).
TS_META_DIR="$REPO_ROOT/thunderstore"
TS_ICON="$TS_META_DIR/icon.png"
TS_README="$TS_META_DIR/README.md"
TS_MANIFEST_TEMPLATE="$TS_META_DIR/manifest.json"
for required in "$TS_ICON" "$TS_README" "$TS_MANIFEST_TEMPLATE"; do
  if [[ ! -f "$required" ]]; then
    echo "Thunderstore metadata missing: $required" >&2
    exit 1
  fi
done

ASSET_SOURCE_DIR=""
for src in "$REPO_ROOT/assets" "$REPO_ROOT/output"; do
  [[ -d "$src" ]] || continue
  if [[ -f "$src/sharedassets0.assets" && -f "$src/sharedassets0.assets.resS" && -f "$src/sharedassets0.resource" ]]; then
    ASSET_SOURCE_DIR="$src"
    break
  fi
done
if [[ -z "$ASSET_SOURCE_DIR" ]]; then
  echo "Thunderstore package requires sharedassets trio in assets/ or output/ (sharedassets0.assets, .assets.resS, .resource)." >&2
  exit 1
fi

TS_DIST_NAME="TCGPatch-TCGShopExpansionMod_0703_Patch-$VERSION"
TS_STAGE="$REPO_ROOT/dist/${TS_DIST_NAME}-stage"
TS_ZIP_PATH="$REPO_ROOT/dist/${TS_DIST_NAME}.zip"
rm -rf "$TS_STAGE"
rm -f "$TS_ZIP_PATH"
mkdir -p "$TS_STAGE/BepInEx/plugins/TCGShopExpansionMod0703Patch"
mkdir -p "$TS_STAGE/Card Shop Simulator_Data"

cp "$BUILT_DLL" "$TS_STAGE/BepInEx/plugins/TCGShopExpansionMod0703Patch/TCGShopExpansionMod0703Patch.dll"
cp "$TS_ICON" "$TS_STAGE/icon.png"
cp "$TS_README" "$TS_STAGE/README.md"
cp "$ASSET_SOURCE_DIR/sharedassets0.assets" "$TS_STAGE/Card Shop Simulator_Data/"
cp "$ASSET_SOURCE_DIR/sharedassets0.assets.resS" "$TS_STAGE/Card Shop Simulator_Data/"
cp "$ASSET_SOURCE_DIR/sharedassets0.resource" "$TS_STAGE/Card Shop Simulator_Data/"

python3 - "$TS_MANIFEST_TEMPLATE" "$TS_STAGE/manifest.json" "$VERSION" <<'PY'
import json, sys
template_path, out_path, version = sys.argv[1], sys.argv[2], sys.argv[3]
with open(template_path, encoding="utf-8") as f:
    manifest = json.load(f)
manifest["version_number"] = version
with open(out_path, "w", encoding="utf-8", newline="\n") as f:
    json.dump(manifest, f, indent=4)
    f.write("\n")
PY

(cd "$TS_STAGE" && zip -rq "$TS_ZIP_PATH" .)
rm -rf "$TS_STAGE"

echo ""
echo "Release folder: $DIST_ROOT"
echo "Release zip:    $ZIP_PATH"
echo "Thunderstore:   $TS_ZIP_PATH"
if [[ "$COPIED_ASSETS" -eq 0 ]]; then
  echo "Note: Installer zip has no assets/ folder; Thunderstore zip includes sharedassets from $ASSET_SOURCE_DIR."
fi
