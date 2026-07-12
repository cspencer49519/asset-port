"""Patch Grading Overhaul 3.4.2 for TCG Card Shop Simulator 0.62.3.

Grading Overhaul targets methods added in newer game builds. Map them to the
closest 0.62.3 equivalents so Harmony can apply patches.
"""
from __future__ import annotations

from pathlib import Path

TOOLS = Path(__file__).resolve().parent
WORKSPACE = TOOLS.parent.parent
SOURCE = (
    WORKSPACE
    / "TCG Card Shop Simulator-0.62.3"
    / "BepInEx"
    / "plugins"
    / "Grading Overhaul"
    / "Grading Overhaul.dll.original-0623"
)
OUTPUT = TOOLS / "Grading Overhaul.patched.dll"

# old_name -> new_name (must be same length or shorter; padded with null bytes)
REPLACEMENTS: tuple[tuple[str, str], ...] = (
    ("ShowSimplifiedCullingGradedCardCase", "ShowGradedCardCase"),
    ("SetSimplifyCardDistanceCull", "SetAlwaysCulling"),
    ("GradedCardOcclusionCull", "ShowGradedCardCase"),
)


def replace_padded(data: bytearray, old: str, new: str) -> int:
    old_b = old.encode("ascii")
    new_b = new.encode("ascii")
    if len(new_b) > len(old_b):
        raise ValueError(f"Replacement '{new}' is longer than '{old}'")
    padded = new_b + b"\x00" * (len(old_b) - len(new_b))
    count = data.count(old_b)
    if count == 0:
        raise ValueError(f"String not found in DLL: {old}")
    data[:] = bytes(data).replace(old_b, padded)
    return count


def main() -> None:
    if not SOURCE.exists():
        raise FileNotFoundError(
            f"Original backup missing: {SOURCE}\n"
            "Restore Grading Overhaul.dll.original-0623 from a clean mod install."
        )

    data = bytearray(SOURCE.read_bytes())
    for old, new in REPLACEMENTS:
        count = replace_padded(data, old, new)
        print(f"  {old} -> {new} ({count}x)")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_bytes(data)
    print(f"Wrote {OUTPUT} ({len(data)} bytes)")


if __name__ == "__main__":
    main()
