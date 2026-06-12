"""Compare base vs mod textures by path_id."""
from __future__ import annotations

import hashlib
from pathlib import Path

import UnityPy

BASE = Path(r"c:\TCGCardShopModWork\asset-port\base-071")
MOD_ASSETS = Path(r"c:\TCGCardShopModWork\asset-port\mod-062\sharedassets0.assets")


def load_paired_mod() -> UnityPy.Environment:
    folder = Path(r"c:\TCGCardShopModWork\asset-port\mod-paired")
    folder.mkdir(exist_ok=True)
    (folder / "sharedassets0.assets").write_bytes(MOD_ASSETS.read_bytes())
    for name in ("sharedassets0.assets.resS", "sharedassets0.resource"):
        (folder / name).write_bytes((BASE / name).read_bytes())
    return UnityPy.load(str(folder))


def tex_by_path(env) -> dict[int, dict]:
    out = {}
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        try:
            data = obj.read()
            img = data.image
            md5 = hashlib.md5(img.tobytes()).hexdigest() if img else None
            out[obj.path_id] = {
                "name": data.m_Name,
                "md5": md5,
                "w": data.m_Width,
                "h": data.m_Height,
            }
        except Exception as exc:  # noqa: BLE001
            out[obj.path_id] = {"error": str(exc)}
    return out


def main() -> None:
    base_env = UnityPy.load(str(BASE))
    mod_env = load_paired_mod()
    base = tex_by_path(base_env)
    mod = tex_by_path(mod_env)

    shared_ids = set(base) & set(mod)
    diff = []
    readable_mod = 0
    for pid in sorted(shared_ids):
        b, m = base[pid], mod[pid]
        if "error" in m:
            continue
        if m.get("md5"):
            readable_mod += 1
        if b.get("md5") and m.get("md5") and b["md5"] != m["md5"]:
            diff.append((pid, b.get("name"), b.get("md5"), m.get("md5")))

    print(f"base textures: {len(base)}")
    print(f"mod textures: {len(mod)}")
    print(f"shared path_ids: {len(shared_ids)}")
    print(f"mod readable at shared ids: {readable_mod}")
    print(f"md5 diffs at shared path_ids: {len(diff)}")
    for row in diff[:30]:
        print(" ", row)


if __name__ == "__main__":
    main()
