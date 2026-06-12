"""List card-like texture names in base sharedassets0."""
from __future__ import annotations

import re
from pathlib import Path

import UnityPy

BASE = Path(r"c:\TCGCardShopModWork\asset-port\base-071")


def main() -> None:
    env = UnityPy.load(str(BASE))
    names = []
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        try:
            n = obj.read().m_Name
            if n:
                names.append(n)
        except Exception:
            pass

    print(f"total named textures: {len(names)}")
    patterns = [
        r"^[A-Z][a-z]+[A-D]$",
        r"Pig",
        r"Bat",
        r"Card",
        r"Pack",
        r"Ghost",
        r"Tetra",
        r"Foil",
        r"Frame",
    ]
    for pat in patterns:
        m = [n for n in names if re.search(pat, n)]
        print(f"pattern {pat}: {len(m)}")
        for x in sorted(m)[:15]:
            print(" ", x)


if __name__ == "__main__":
    main()
