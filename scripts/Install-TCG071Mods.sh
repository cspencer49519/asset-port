#!/usr/bin/env bash
# Installs TCGShopExpansionMod071Patch (and optional ported sharedassets) into a 0.71 game folder.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
MANIFEST="${RELEASE_ROOT}/manifest.json"

GAME_PATH=""
SKIP_ASSETS=0
FORCE=0
DRY_RUN=0

usage() {
    cat <<'EOF'
Usage: Install-TCG071Mods.sh [--game-path PATH] [--skip-assets] [--force] [--dry-run]

Installs TCGShopExpansionMod071Patch and optional sharedassets into a 0.71 game folder.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --game-path)
            GAME_PATH="${2:-}"
            shift 2
            ;;
        --skip-assets)
            SKIP_ASSETS=1
            shift
            ;;
        --force)
            FORCE=1
            shift
            ;;
        --dry-run)
            DRY_RUN=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            if [[ -z "${GAME_PATH}" ]]; then
                GAME_PATH="$1"
                shift
            else
                echo "ERROR: Unknown argument: $1" >&2
                usage >&2
                exit 1
            fi
            ;;
    esac
done

write_step() { echo "==> $1"; }
write_ok() { echo " OK  $1"; }
write_warn() { echo "WARN $1"; }

require_python() {
    if ! command -v python3 >/dev/null 2>&1; then
        echo "ERROR: python3 is required to read manifest.json." >&2
        exit 1
    fi
}

read_manifest() {
    python3 "${SCRIPT_DIR}/read_manifest.py" "${MANIFEST}" "$1"
}

ensure_directory() {
    local path="$1"
    if [[ ! -d "${path}" ]]; then
        if [[ "${DRY_RUN}" -eq 1 ]]; then
            echo "[dry-run] Create directory ${path}"
        else
            mkdir -p "${path}"
        fi
    fi
}

copy_with_backup() {
    local source="$1"
    local destination="$2"
    local backup_dir="$3"

    if [[ ! -f "${source}" ]]; then
        return 1
    fi

    ensure_directory "$(dirname "${destination}")"
    if [[ -f "${destination}" ]]; then
        ensure_directory "${backup_dir}"
        local backup_name="${backup_dir}/$(basename "${destination}")"
        if [[ "${DRY_RUN}" -eq 1 ]]; then
            echo "[dry-run] Backup ${destination} to ${backup_name}"
        else
            cp -f "${destination}" "${backup_name}"
            write_ok "Backed up $(basename "${destination}")"
        fi
    fi

    if [[ "${DRY_RUN}" -eq 1 ]]; then
        echo "[dry-run] Copy ${source} to ${destination}"
    else
        cp -f "${source}" "${destination}"
        write_ok "Installed $(basename "${destination}")"
    fi
    return 0
}

resolve_game_path() {
    local explicit_path="$1"
    local game_exe="$2"

    if [[ -n "${explicit_path}" ]]; then
        if [[ -f "${explicit_path}/${game_exe}" ]]; then
            printf '%s\n' "$(cd "${explicit_path}" && pwd)"
            return 0
        fi
        echo "ERROR: Game folder not found at ${explicit_path}" >&2
        exit 1
    fi

    local sibling
    sibling="$(cd "${RELEASE_ROOT}/.." && pwd)/TCG Card Shop Simulator"
    if [[ -f "${sibling}/${game_exe}" ]]; then
        write_warn "Using sibling game folder: ${sibling}"
        printf '%s\n' "${sibling}"
        return 0
    fi

    local steam_paths=(
        "${HOME}/.steam/steam/steamapps/common/TCG Card Shop Simulator"
        "${HOME}/.local/share/Steam/steamapps/common/TCG Card Shop Simulator"
        "${HOME}/Library/Application Support/Steam/steamapps/common/TCG Card Shop Simulator"
    )
    local root
    for root in "${steam_paths[@]}"; do
        if [[ -f "${root}/${game_exe}" ]]; then
            write_warn "Using Steam default path: ${root}"
            printf '%s\n' "${root}"
            return 0
        fi
    done

    echo "ERROR: Game folder not found. Pass --game-path to the folder containing ${game_exe}" >&2
    exit 1
}

resolve_patch_dll() {
    local release_dll="${RELEASE_ROOT}/patches/TCGShopExpansionMod071Patch.dll"
    if [[ -f "${release_dll}" ]]; then
        printf '%s\n' "${release_dll}"
        return 0
    fi

    local dev_dll="${RELEASE_ROOT}/TCGShopExpansionMod071Patch/bin/Release/netstandard2.1/TCGShopExpansionMod071Patch.dll"
    if [[ -f "${dev_dll}" ]]; then
        write_warn "Using dev build output for patch DLL."
        printf '%s\n' "${dev_dll}"
        return 0
    fi

    echo "ERROR: Patch DLL not found. Run scripts/Build-Release.ps1 or dotnet build first." >&2
    exit 1
}

if [[ ! -f "${MANIFEST}" ]]; then
    echo "ERROR: Could not find manifest.json at ${MANIFEST}" >&2
    exit 1
fi

require_python

PATCH_VERSION="$(read_manifest patchVersion)"
GAME_EXE="$(read_manifest paths.gameExe)"
DATA_FOLDER="$(read_manifest paths.dataFolder)"
SHARED_ASSETS="$(read_manifest paths.sharedAssets)"
SHARED_ASSETS_RESS="$(read_manifest paths.sharedAssetsResS)"
SHARED_ASSETS_RESOURCE="$(read_manifest paths.sharedAssetsResource)"

GAME_ROOT="$(resolve_game_path "${GAME_PATH}" "${GAME_EXE}")"
PATCH_DLL="$(resolve_patch_dll)"

write_step "Release root: ${RELEASE_ROOT}"
write_step "Game root:    ${GAME_ROOT}"
write_step "Patch DLL:    ${PATCH_DLL}"

if [[ ! -f "${GAME_ROOT}/${GAME_EXE}" ]]; then
    echo "ERROR: Invalid game folder (missing ${GAME_EXE}): ${GAME_ROOT}" >&2
    exit 1
fi

if [[ ! -d "${GAME_ROOT}/BepInEx" ]]; then
    write_warn "BepInEx folder not found. Install BepInEx (Nexus mod 27) before playing."
fi

PLUGIN_DIR="${GAME_ROOT}/BepInEx/plugins/TCGShopExpansionMod071Patch"
PLUGIN_DLL="${PLUGIN_DIR}/TCGShopExpansionMod071Patch.dll"
ensure_directory "${PLUGIN_DIR}"

if [[ -f "${PLUGIN_DLL}" && "${FORCE}" -eq 0 ]]; then
    existing_size="$(wc -c < "${PLUGIN_DLL}" | tr -d ' ')"
    incoming_size="$(wc -c < "${PATCH_DLL}" | tr -d ' ')"
    if [[ "${existing_size}" == "${incoming_size}" ]]; then
        write_ok "Patch DLL already installed (same size)."
    else
        write_warn "Patch DLL exists and differs. Re-run with --force to overwrite."
    fi
elif [[ "${DRY_RUN}" -eq 1 ]]; then
    echo "[dry-run] Copy ${PATCH_DLL} to ${PLUGIN_DLL}"
else
    cp -f "${PATCH_DLL}" "${PLUGIN_DLL}"
    write_ok "Installed patch DLL v${PATCH_VERSION}"
fi

if [[ "${SKIP_ASSETS}" -eq 0 ]]; then
    write_step "Ported sharedassets trio (Genobear card frames)"
    ASSETS_SOURCE="${RELEASE_ROOT}/assets"
    if [[ ! -d "${ASSETS_SOURCE}" ]]; then
        ASSETS_SOURCE="${RELEASE_ROOT}/output"
    fi

    if [[ ! -d "${ASSETS_SOURCE}" ]]; then
        write_warn "No assets/ or output/ folder in release — skipping sharedassets install."
        write_warn "Card frames will stay vanilla until you get a full release zip with assets/."
    else
        TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
        DATA_DIR="${GAME_ROOT}/${DATA_FOLDER}"
        BACKUP_DIR="${DATA_DIR}/_backup_sharedassets_${TIMESTAMP}"
        INSTALLED_ANY=0
        for rel_path in "${SHARED_ASSETS}" "${SHARED_ASSETS_RESS}" "${SHARED_ASSETS_RESOURCE}"; do
            name="$(basename "${rel_path}")"
            src="${ASSETS_SOURCE}/${name}"
            dst="${DATA_DIR}/${name}"
            if copy_with_backup "${src}" "${dst}" "${BACKUP_DIR}"; then
                INSTALLED_ANY=1
            else
                write_warn "Missing source asset: ${src}"
            fi
        done
        if [[ "${INSTALLED_ANY}" -eq 1 ]]; then
            write_ok "Sharedassets backup folder: ${BACKUP_DIR}"
        fi
    fi
else
    write_warn "Skipped sharedassets install (--skip-assets)."
fi

echo
write_step "Manual steps still required"
echo "  1. Install Nexus mods + Genobear (phases 1-3 in docs/INSTALL-071.md) if not done yet"
echo "  2. Run: ./scripts/Verify-TCG071Install.sh --game-path \"${GAME_ROOT}\""
echo "  3. Launch game, press F1, configure ExpansionMod (see docs/VERSION_MATRIX.md)"
echo "  4. Do not use --skip-assets on a normal install — card frames need the ported trio"
echo
write_ok "Install complete."
