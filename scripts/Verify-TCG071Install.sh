#!/usr/bin/env bash
# Verifies a 0.71 Genobear + 071 patch install.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
MANIFEST="${RELEASE_ROOT}/manifest.json"

GAME_PATH=""
FAIL_COUNT=0
WARN_COUNT=0

usage() {
    cat <<'EOF'
Usage: Verify-TCG071Install.sh [--game-path PATH]

Verifies a 0.71 Genobear + 071 patch install.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --game-path)
            GAME_PATH="${2:-}"
            shift 2
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

write_pass() { echo "[PASS] $1"; }
write_fail() { echo "[FAIL] $1"; FAIL_COUNT=$((FAIL_COUNT + 1)); }
write_warn() { echo "[WARN] $1"; WARN_COUNT=$((WARN_COUNT + 1)); }

require_python() {
    if ! command -v python3 >/dev/null 2>&1; then
        echo "ERROR: python3 is required to read manifest.json." >&2
        exit 1
    fi
}

read_manifest() {
    python3 "${SCRIPT_DIR}/read_manifest.py" "${MANIFEST}" "$1"
}

resolve_game_path() {
    local explicit_path="$1"
    local game_exe="$2"

    if [[ -n "${explicit_path}" ]]; then
        if [[ -f "${explicit_path}/${game_exe}" ]]; then
            printf '%s\n' "$(cd "${explicit_path}" && pwd)"
            return 0
        fi
        echo "ERROR: Invalid game path: ${explicit_path}" >&2
        exit 1
    fi

    local sibling
    sibling="$(cd "${RELEASE_ROOT}/.." && pwd)/TCG Card Shop Simulator"
    if [[ -f "${sibling}/${game_exe}" ]]; then
        printf '%s\n' "${sibling}"
        return 0
    fi

    echo "ERROR: Pass --game-path to the folder containing ${game_exe}" >&2
    exit 1
}

check_file_exists() {
    local relative_path="$1"
    local required="$2"
    local full_path="${GAME_ROOT}/${relative_path}"

    if [[ -e "${full_path}" ]]; then
        write_pass "${relative_path}"
        return 0
    fi

    if [[ "${required}" -eq 1 ]]; then
        write_fail "Missing: ${relative_path}"
    else
        write_warn "Missing (optional): ${relative_path}"
    fi
    return 1
}

if [[ ! -f "${MANIFEST}" ]]; then
    echo "ERROR: Could not find manifest.json." >&2
    exit 1
fi

require_python

PATCH_VERSION="$(read_manifest patchVersion)"
GAME_EXE="$(read_manifest paths.gameExe)"
GAME_ROOT="$(resolve_game_path "${GAME_PATH}" "${GAME_EXE}")"

echo "Verifying install at: ${GAME_ROOT}"
echo "Expected patch version: ${PATCH_VERSION}"
echo

check_file_exists "${GAME_EXE}" 1 || true
check_file_exists "BepInEx" 1 || true
check_file_exists "$(read_manifest paths.patchDll)" 1 || true
check_file_exists "$(read_manifest paths.expansionModDll)" 1 || true
check_file_exists "$(read_manifest paths.newCardsModDll)" 1 || true
check_file_exists "$(read_manifest paths.cardArtAssets)" 0 || true
check_file_exists "$(read_manifest paths.sharedAssets)" 0 || true

SHARED_PATH="${GAME_ROOT}/$(read_manifest paths.sharedAssets)"
if [[ -f "${SHARED_PATH}" ]]; then
    SIZE="$(wc -c < "${SHARED_PATH}" | tr -d ' ')"
    VANILLA="$(read_manifest sharedAssets.vanillaBytes)"
    PORTED="$(read_manifest sharedAssets.portedMinBytes)"
    if [[ "${SIZE}" -le "${VANILLA}" ]]; then
        write_warn "sharedassets0.assets is vanilla size (${SIZE} bytes) — re-run install without --skip-assets"
    elif [[ "${SIZE}" -ge "${PORTED}" ]]; then
        write_pass "sharedassets0.assets looks ported (${SIZE} bytes)"
    fi
fi

LOG_FILE="$(read_manifest paths.logFile)"
LOG_PATH="${GAME_ROOT}/${LOG_FILE}"

if [[ -f "${LOG_PATH}" ]]; then
    write_pass "${LOG_FILE}"
    while IFS= read -r marker; do
        [[ -z "${marker}" ]] && continue
        if grep -Fq "${marker}" "${LOG_PATH}"; then
            write_pass "Log contains: ${marker}"
        else
            write_warn "Log missing (launch game once): ${marker}"
        fi
    done < <(read_manifest logSuccessMarkers)

    while IFS= read -r marker; do
        [[ -z "${marker}" ]] && continue
        if grep -Fq "${marker}" "${LOG_PATH}"; then
            write_fail "Log contains failure marker: ${marker}"
        fi
    done < <(read_manifest logFailureMarkers)
else
    write_warn "No log yet — launch the game once, then re-run this script."
fi

echo
if [[ "${FAIL_COUNT}" -eq 0 && "${WARN_COUNT}" -eq 0 ]]; then
    echo "All checks passed."
    exit 0
fi
if [[ "${FAIL_COUNT}" -eq 0 ]]; then
    echo "Passed with ${WARN_COUNT} warning(s). See docs/TROUBLESHOOTING.md"
    exit 0
fi

echo "${FAIL_COUNT} failure(s), ${WARN_COUNT} warning(s). See docs/TROUBLESHOOTING.md"
exit 1
