"""Find Texture2D objects whose serialized .assets bytes differ between mod and base."""
from __future__ import annotations

from pathlib import Path

import UnityPy

BASE = Path(r"c:\TCGCardShopModWork\asset-port\base-0703")
MOD = Path(r"c:\TCGCardShopModWork\asset-port\mod-062\sharedassets0.assets")


def raw_map(path: Path) -> dict[tuple[int, str], bytes]:
    env = UnityPy.load(str(path))
    out = {}
    for obj in env.objects:
        try:
            out[(obj.path_id, obj.type.name)] = obj.get_raw_data()
        except Exception:
            pass
    return out


def main() -> None:
    base = raw_map(BASE / "sharedassets0.assets")
    mod = raw_map(MOD)

    shared = set(base) & set(mod)
    diffs = []
    for key in shared:
        if base[key] != mod[key]:
            diffs.append(key)

    tex_diffs = [k for k in diffs if k[1] == "Texture2D"]
    print(f"shared objects: {len(shared)}")
    print(f"raw byte diffs: {len(diffs)}")
    print(f"texture raw diffs: {len(tex_diffs)}")

    env = UnityPy.load(str(BASE / "sharedassets0.assets"))
    by_id = {o.path_id: o for o in env.objects}
    names = []
    for pid, _ in tex_diffs:
        try:
            names.append(by_id[pid].read().m_Name)
        except Exception:
            names.append(f"pathid_{pid}")
    print("texture diff names sample:")
    for n in sorted(names)[:60]:
        print(" ", n)
    print(f"... total named: {len(names)}")


if __name__ == "__main__":
    main()
