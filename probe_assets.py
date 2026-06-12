"""Probe sharedassets0 structure for port planning."""
from __future__ import annotations

import hashlib
from collections import defaultdict
from pathlib import Path

import UnityPy

BASE = Path(r"c:\TCGCardShopModWork\asset-port\base-071")
MOD = Path(r"c:\TCGCardShopModWork\asset-port\mod-062")


def texture_index(folder: Path, label: str) -> dict[str, dict]:
    env = UnityPy.load(str(folder))
    out: dict[str, dict] = {}
    errors = 0
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        try:
            data = obj.read()
            name = data.m_Name or f"pathid_{obj.path_id}"
            img = data.image
            digest = None
            if img is not None:
                digest = hashlib.md5(img.tobytes()).hexdigest()
            out[name] = {
                "path_id": obj.path_id,
                "width": getattr(data, "m_Width", None),
                "height": getattr(data, "m_Height", None),
                "md5": digest,
                "has_image": img is not None,
            }
        except Exception as exc:  # noqa: BLE001
            errors += 1
            out[f"pathid_{obj.path_id}"] = {"error": str(exc)}
    print(f"{label}: textures={len(out)} read_errors={errors}")
    return out


def main() -> None:
    print("Loading base-071 (full trio)...")
    base = texture_index(BASE, "base-071")

    print("Loading mod-062 (.assets only)...")
    mod_only = texture_index(MOD, "mod-062-assets-only")

    # Pair mod assets with base resS (broken pairing, diagnostic only)
    paired = MOD / "paired"
    paired.mkdir(exist_ok=True)
    for name in ("sharedassets0.assets", "sharedassets0.assets.resS", "sharedassets0.resource"):
        src = BASE / name
        dst = paired / name
        if name == "sharedassets0.assets":
            dst.write_bytes((MOD / "sharedassets0.assets").read_bytes())
        else:
            dst.write_bytes(src.read_bytes())
    print("Loading mod assets + base resS (diagnostic pairing)...")
    mod_paired = texture_index(paired, "mod-062-with-base-resS")

    names_base = set(base.keys())
    names_mod = set(mod_only.keys())
    names_paired = set(mod_paired.keys())

    common = names_base & names_mod
    diff_md5 = []
    for name in sorted(common):
        b = base.get(name, {})
        m = mod_only.get(name, {})
        if b.get("md5") and m.get("md5") and b["md5"] != m["md5"]:
            diff_md5.append(name)

    print(f"common names (base vs mod-assets-only): {len(common)}")
    print(f"different md5 (base vs mod-assets-only): {len(diff_md5)}")
    print(f"mod readable images (assets only): {sum(1 for v in mod_only.values() if v.get('has_image'))}")
    print(
        f"mod readable images (paired resS): {sum(1 for v in mod_paired.values() if v.get('has_image'))}"
    )
    print("sample diffs:", diff_md5[:20])

    only_mod = names_mod - names_base
    only_base = names_base - names_mod
    print(f"only in mod: {len(only_mod)} only in base: {len(only_base)}")

    type_counts = defaultdict(int)
    env = UnityPy.load(str(BASE))
    for obj in env.objects:
        type_counts[obj.type.name] += 1
    print("base object types:", dict(sorted(type_counts.items(), key=lambda x: -x[1])[:15]))


if __name__ == "__main__":
    main()
