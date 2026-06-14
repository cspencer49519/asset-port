"""Port Genobear mod sharedassets0 texture changes onto game 0.71 base.

Only applies card/mod-related Texture2D assets. UI, font, loader, and input
textures are skipped to avoid white-screen / invisible UI regressions caused by
mis-paired .resS reads from the 0.62 mod file.
"""
from __future__ import annotations

import hashlib
import json
import re
import shutil
from datetime import datetime, timezone
from pathlib import Path

import UnityPy
from PIL import Image

ROOT = Path(__file__).resolve().parent
BASE = ROOT / "base-071"
MOD_ASSETS = ROOT / "mod-062" / "sharedassets0.assets"
MOD_PAIRED = ROOT / "mod-paired"
EXPORTED = ROOT / "exported-mod"
OUTPUT = ROOT / "output"
REPORT = ROOT / "port_report.json"

# Textures the Real TCG Overhaul mod is expected to change in sharedassets0.
PORT_NAME = re.compile(
    r"^(?:"
    r"Bat[A-D]"
    r"|Card"
    r"|Ghost_"
    r"|RainbowFoil"
    r"|Evo(?:BasicIcon|Border)"
    r"|T_Card"
    r"|GradedCard"
    r"|GradeCard"
    r")",
    re.IGNORECASE,
)


def ensure_mod_paired() -> None:
    MOD_PAIRED.mkdir(parents=True, exist_ok=True)
    shutil.copy2(MOD_ASSETS, MOD_PAIRED / "sharedassets0.assets")
    for name in ("sharedassets0.assets.resS", "sharedassets0.resource"):
        shutil.copy2(BASE / name, MOD_PAIRED / name)


def image_md5(img: Image.Image) -> str:
    return hashlib.md5(img.tobytes()).hexdigest()


def prepare_image(img: Image.Image) -> Image.Image:
    if img.mode not in ("RGB", "RGBA"):
        img = img.convert("RGBA")
    if img.mode == "RGBA":
        return img
    return img.convert("RGB")


def port_textures() -> dict:
    ensure_mod_paired()
    EXPORTED.mkdir(parents=True, exist_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)

    for name in ("sharedassets0.assets", "sharedassets0.assets.resS", "sharedassets0.resource"):
        shutil.copy2(BASE / name, OUTPUT / name)

    base_env = UnityPy.load(str(OUTPUT))
    mod_env = UnityPy.load(str(MOD_PAIRED))
    mod_by_id = {obj.path_id: obj for obj in mod_env.objects}

    report = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "note": "Selective card/mod Texture2D port; UI/font/loader textures excluded.",
        "applied": [],
        "skipped_same": [],
        "skipped_filtered": [],
        "skipped_no_mod_object": [],
        "skipped_unreadable": [],
        "skipped_same_image": [],
        "errors": [],
    }

    for b_obj in base_env.objects:
        if b_obj.type.name != "Texture2D":
            continue

        name = f"pathid_{b_obj.path_id}"
        try:
            b_raw = b_obj.get_raw_data()
        except Exception as exc:  # noqa: BLE001
            report["errors"].append({"path_id": b_obj.path_id, "stage": "base_raw", "error": str(exc)})
            continue

        m_obj = mod_by_id.get(b_obj.path_id)
        if not m_obj:
            report["skipped_no_mod_object"].append({"path_id": b_obj.path_id, "name": name})
            continue

        try:
            m_raw = m_obj.get_raw_data()
        except Exception as exc:  # noqa: BLE001
            report["errors"].append({"path_id": b_obj.path_id, "stage": "mod_raw", "error": str(exc)})
            continue

        if b_raw == m_raw:
            try:
                name = b_obj.read().m_Name or name
            except Exception:
                pass
            report["skipped_same"].append({"path_id": b_obj.path_id, "name": name})
            continue

        try:
            b_data = b_obj.read()
            m_data = m_obj.read()
            name = b_data.m_Name or name
            b_img = b_data.image
            m_img = m_data.image
        except Exception as exc:  # noqa: BLE001
            report["skipped_unreadable"].append({"path_id": b_obj.path_id, "name": name, "error": str(exc)})
            continue

        if not PORT_NAME.match(name):
            report["skipped_filtered"].append({"path_id": b_obj.path_id, "name": name})
            continue

        if m_img is None:
            report["skipped_unreadable"].append({"path_id": b_obj.path_id, "name": name, "error": "mod image None"})
            continue

        if b_img is not None and image_md5(b_img) == image_md5(m_img):
            report["skipped_same_image"].append({"path_id": b_obj.path_id, "name": name})
            continue

        m_img = prepare_image(m_img)
        safe_name = "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in name)
        png_path = EXPORTED / f"{safe_name}.png"
        m_img.save(png_path)

        try:
            b_data.image = m_img
            b_data.save()
        except Exception as exc:  # noqa: BLE001
            report["errors"].append({"path_id": b_obj.path_id, "name": name, "stage": "apply", "error": str(exc)})
            continue

        report["applied"].append(
            {
                "path_id": b_obj.path_id,
                "name": name,
                "png": str(png_path),
                "base_size": [b_data.m_Width, b_data.m_Height],
                "mod_size": [m_data.m_Width, m_data.m_Height],
                "applied_image_size": list(m_img.size),
            }
        )

    base_env.save(out_path=str(OUTPUT))

    report["counts"] = {k: len(v) for k, v in report.items() if isinstance(v, list)}
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")

    checklist = ROOT / "texture_checklist.txt"
    checklist.write_text("\n".join(sorted(x["name"] for x in report["applied"])), encoding="utf-8")
    return report


def main() -> None:
    report = port_textures()
    print(json.dumps(report["counts"], indent=2))
    print(f"Report: {REPORT}")
    print(f"Output: {OUTPUT}")


if __name__ == "__main__":
    main()
