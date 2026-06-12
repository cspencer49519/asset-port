"""Count port candidates with dimension-safe filtering."""
from __future__ import annotations

from pathlib import Path

import UnityPy

BASE = Path(r"c:\TCGCardShopModWork\asset-port\base-071")
MOD = Path(r"c:\TCGCardShopModWork\asset-port\mod-062\sharedassets0.assets")


def load_paired_mod():
    folder = Path(r"c:\TCGCardShopModWork\asset-port\mod-paired")
    folder.mkdir(exist_ok=True)
    (folder / "sharedassets0.assets").write_bytes(MOD.read_bytes())
    for name in ("sharedassets0.assets.resS", "sharedassets0.resource"):
        (folder / name).write_bytes((BASE / name).read_bytes())
    return UnityPy.load(str(folder))


def main() -> None:
    base_env = UnityPy.load(str(BASE))
    mod_env = load_paired_mod()
    mod_by_id = {o.path_id: o for o in mod_env.objects}

    candidates = []
    skipped_dim = 0
    skipped_read = 0
    skipped_same = 0
    for b_obj in base_env.objects:
        if b_obj.type.name != "Texture2D":
            continue
        m_obj = mod_by_id.get(b_obj.path_id)
        if not m_obj:
            continue
        try:
            b_raw = b_obj.get_raw_data()
            m_raw = m_obj.get_raw_data()
            if b_raw == m_raw:
                skipped_same += 1
                continue
            b_data = b_obj.read()
            m_data = m_obj.read()
            if b_data.m_Width != m_data.m_Width or b_data.m_Height != m_data.m_Height:
                skipped_dim += 1
                continue
            if b_data.image is None or m_data.image is None:
                skipped_read += 1
                continue
            candidates.append(b_data.m_Name or f"pathid_{b_obj.path_id}")
        except Exception:
            skipped_read += 1

    print(f"skipped same raw: {skipped_same}")
    print(f"skipped dimension mismatch: {skipped_dim}")
    print(f"skipped read errors: {skipped_read}")
    print(f"candidates: {len(candidates)}")
    for n in sorted(candidates)[:50]:
        print(" ", n)


if __name__ == "__main__":
    main()
