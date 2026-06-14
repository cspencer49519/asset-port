#!/usr/bin/env python3
"""Read values from manifest.json for install/verify scripts."""
import json
import sys


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: read_manifest.py <manifest.json> <dotted.key>", file=sys.stderr)
        return 2

    manifest_path, key = sys.argv[1], sys.argv[2]
    with open(manifest_path, encoding="utf-8") as handle:
        data = json.load(handle)

    value = data
    for part in key.split("."):
        value = value[part]

    if isinstance(value, list):
        for item in value:
            print(item)
    else:
        print(value)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
