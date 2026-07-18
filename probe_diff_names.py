"""List texture diffs by name patterns."""
from __future__ import annotations

import hashlib
import re
from pathlib import Path

import UnityPy

BASE = Path(r"c:\TCGCardShopModWork\asset-port\base-0703")
MOD_ASSETS = Path(r"c:\TCGCardShopModWork\asset-port\mod-062\sharedassets0.assets")


def load_paired_mod():
    folder = Path(r"c:\TCGCardShopModWork\asset-port\mod-paired")
    folder.mkdir(exist_ok=True)
    (folder / "sharedassets0.assets").write_bytes(MOD_ASSETS.read_bytes())
    for name in ("sharedassets0.assets.resS", "sharedassets0.resource"):
        (folder / name).write_bytes((BASE / name).read_bytes())
    return UnityPy.load(str(folder))


def main() -> None:
    base_env = UnityPy.load(str(BASE))
    mod_env = load_paired_mod()
    mod_by_id = {obj.path_id: obj for obj in mod_env.objects}

    diffs = []
    for b_obj in base_env.objects:
        if b_obj.type.name != "Texture2D":
            continue
        m_obj = mod_by_id.get(b_obj.path_id)
        if not m_obj or m_obj.type.name != "Texture2D":
            continue
        try:
            b_data = b_obj.read()
            m_data = m_obj.read()
            b_img, m_img = b_data.image, m_data.image
            if not b_img or not m_img:
                continue
            if hashlib.md5(b_img.tobytes()).hexdigest() == hashlib.md5(m_img.tobytes()).hexdigest():
                continue
            diffs.append(b_data.m_Name or f"pathid_{b_obj.path_id}")
        except Exception:
            pass

    print(f"total diffs: {len(diffs)}")
    cardish = [n for n in diffs if re.search(r"(Card|Pig|Pack|Tetra|Destiny|Ghost|Monster|Icon|Evo|Foil|Frame|Binder|Shop|Booster)", n, re.I)]
    print(f"cardish pattern matches: {len(cardish)}")
    for n in sorted(cardish)[:80]:
        print(" ", n)
    print("--- other samples ---")
    for n in sorted(set(diffs) - set(cardish))[:40]:
        print(" ", n)


if __name__ == "__main__":
    main()
