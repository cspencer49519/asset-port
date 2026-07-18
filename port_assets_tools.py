"""Port mod textures using UnityPy export + AssetsTools.NET write.

UnityPy save corrupts 0.70.3 sharedassets/.resS pairing (white screen). UnityPy read
is reliable for decoding mod textures. AssetsTools.NET v3 preserves file structure
when writing patched Texture2D payloads.
"""
from __future__ import annotations

import json
import re
import shutil
from datetime import datetime, timezone
from pathlib import Path

import UnityPy
from PIL import Image
import clr

ATNET_DIR = Path(__file__).resolve().parent / "tools" / "atnet"
clr.AddReference(str(ATNET_DIR / "AssetsTools.NET.dll"))
clr.AddReference(str(ATNET_DIR / "AssetsTools.NET.Texture.dll"))

from AssetsTools.NET import AssetsFileWriter  # noqa: E402
from AssetsTools.NET.Extra import AssetsManager  # noqa: E402
from AssetsTools.NET.Texture import TextureFile, TextureFormat  # noqa: E402

ROOT = Path(__file__).resolve().parent
ATNET = ROOT / "tools" / "atnet"
BASE_DIR = ROOT / "base-0703"
MOD_ASSETS = ROOT / "mod-062" / "sharedassets0.assets"
MOD_PAIRED = ROOT / "mod-paired"
EXPORTED = ROOT / "exported-mod"
OUTPUT = ROOT / "output"
REPORT = ROOT / "port_report_atnet.json"
TPK = ATNET / "classdata.tpk"

PORT_NAME = re.compile(
    r"^(?:"
    r"Bat[A-D]"
    r"|Card"
    r"|Ghost_"
    r"|GradedCard"
    r"|GradeCard"
    r")",
    re.IGNORECASE,
)

# Foil/outline/font atlases must stay vanilla 0.70.3 — mod pixels break TMP glyph layout.
SKIP_TEXTURE = re.compile(
    r"(?:"
    r"^RainbowFoil$"
    r"|^Evo(?:BasicIcon|Border)$"
    r"|^T_Card"
    r"|Outline"
    r"|^CenterDot_.*Outline$"
    r"|^FredokaOne"
    r"|^Font Texture$"
    r")",
    re.IGNORECASE,
)


def should_port(name: str) -> bool:
    if SKIP_TEXTURE.search(name):
        return False
    return bool(PORT_NAME.match(name))


def ensure_mod_paired() -> None:
    MOD_PAIRED.mkdir(parents=True, exist_ok=True)
    shutil.copy2(MOD_ASSETS, MOD_PAIRED / "sharedassets0.assets")
    for name in ("sharedassets0.assets.resS", "sharedassets0.resource"):
        shutil.copy2(BASE_DIR / name, MOD_PAIRED / name)


def prepare_image(img: Image.Image) -> Image.Image:
    if img.mode not in ("RGB", "RGBA"):
        img = img.convert("RGBA")
    return img


def pad_size(width: int, height: int, block: int = 4) -> tuple[int, int]:
    return (
        width if width % block == 0 else width + (block - width % block),
        height if height % block == 0 else height + (block - height % block),
    )


def png_to_bgra(path: Path, target_w: int, target_h: int) -> tuple[bytes, int, int]:
    img = Image.open(path).convert("RGBA")
    tw, th = pad_size(target_w, target_h)
    if img.size != (tw, th):
        img = img.resize((tw, th), Image.Resampling.LANCZOS)
    img = img.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    width, height = img.size
    rgba = img.tobytes()
    bgra = bytearray(len(rgba))
    for i in range(0, len(rgba), 4):
        bgra[i] = rgba[i + 2]
        bgra[i + 1] = rgba[i + 1]
        bgra[i + 2] = rgba[i]
        bgra[i + 3] = rgba[i + 3]
    return bytes(bgra), width, height


def encode_bgra(bgra: bytes, fmt: TextureFormat, width: int, height: int) -> tuple[bytes, TextureFormat]:
    enc = TextureFile.EncodeManagedData(bgra, fmt, width, height, False)
    if enc is not None and len(enc) > 0:
        return bytes(enc), fmt
    enc = TextureFile.EncodeNativeData(bgra, fmt, width, height, 4, False)
    if enc is not None and len(enc) > 0:
        return bytes(enc), fmt
    fmt = TextureFormat.RGBA32
    enc = TextureFile.EncodeManagedData(bgra, fmt, width, height, False)
    if enc is None:
        raise RuntimeError(f"Encode failed for format {fmt}")
    return bytes(enc), fmt


def import_png_into_base(base_field, png_path: Path) -> None:
    tw = base_field["m_Width"].AsInt
    th = base_field["m_Height"].AsInt
    fmt = TextureFormat(base_field["m_TextureFormat"].AsInt)
    bgra, width, height = png_to_bgra(png_path, tw, th)
    enc, fmt = encode_bgra(bgra, fmt, width, height)

    stream = base_field["m_StreamData"]
    stream["offset"].AsInt = 0
    stream["size"].AsInt = 0
    stream["path"].AsString = ""

    if not base_field["m_MipCount"].IsDummy:
        base_field["m_MipCount"].AsInt = 1

    base_field["m_TextureFormat"].AsInt = int(fmt)
    base_field["m_CompleteImageSize"].AsInt = len(enc)
    base_field["m_Width"].AsInt = width
    base_field["m_Height"].AsInt = height
    base_field["image data"].AsByteArray = enc


def export_changes() -> tuple[list[dict], dict]:
    ensure_mod_paired()
    EXPORTED.mkdir(parents=True, exist_ok=True)

    base_env = UnityPy.load(str(BASE_DIR))
    mod_env = UnityPy.load(str(MOD_PAIRED))
    mod_by_id = {obj.path_id: obj for obj in mod_env.objects}

    report = {
        "skipped_same": [],
        "skipped_filtered": [],
        "skipped_no_mod": [],
        "skipped_unreadable": [],
        "export_errors": [],
    }
    changes: list[dict] = []

    for b_obj in base_env.objects:
        if b_obj.type.name != "Texture2D":
            continue

        path_id = b_obj.path_id
        name = f"pathid_{path_id}"
        m_obj = mod_by_id.get(path_id)
        if m_obj is None:
            report["skipped_no_mod"].append({"path_id": path_id, "name": name})
            continue

        try:
            if b_obj.get_raw_data() == m_obj.get_raw_data():
                try:
                    name = b_obj.read().m_Name or name
                except Exception:
                    pass
                report["skipped_same"].append({"path_id": path_id, "name": name})
                continue

            b_data = b_obj.read()
            m_data = m_obj.read()
            name = b_data.m_Name or name
            m_img = m_data.image
        except Exception as exc:  # noqa: BLE001
            report["skipped_unreadable"].append({"path_id": path_id, "name": name, "error": str(exc)})
            continue

        if not should_port(name):
            report["skipped_filtered"].append({"path_id": path_id, "name": name})
            continue

        if m_img is None:
            report["skipped_unreadable"].append(
                {"path_id": path_id, "name": name, "error": "mod image None"}
            )
            continue

        try:
            m_img = prepare_image(m_img)
            safe = "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in name)
            png_path = EXPORTED / f"{safe}.png"
            m_img.save(png_path)
            changes.append(
                {
                    "path_id": path_id,
                    "name": name,
                    "png": str(png_path),
                    "base_size": [b_data.m_Width, b_data.m_Height],
                    "mod_size": [m_data.m_Width, m_data.m_Height],
                }
            )
        except Exception as exc:  # noqa: BLE001
            report["export_errors"].append({"path_id": path_id, "name": name, "error": str(exc)})

    return changes, report


def apply_changes(changes: list[dict]) -> dict:
    work = ROOT / "work-atnet"
    if work.exists():
        shutil.rmtree(work)
    shutil.copytree(BASE_DIR, work)
    if OUTPUT.exists():
        shutil.rmtree(OUTPUT)
    OUTPUT.mkdir(parents=True)

    manager = AssetsManager()
    manager.LoadClassPackage(str(TPK))
    base_inst = manager.LoadAssetsFile(str(work / "sharedassets0.assets"), False)
    manager.LoadClassDatabaseFromPackage(base_inst.file.Metadata.UnityVersion)

    by_path = {info.PathId: info for info in base_inst.file.Metadata.AssetInfos}
    applied: list[dict] = []
    errors: list[dict] = []

    for change in changes:
        info = by_path.get(change["path_id"])
        if info is None:
            errors.append({"name": change["name"], "error": "path_id missing in base"})
            continue
        try:
            base_field = manager.GetBaseField(base_inst, info)
            import_png_into_base(base_field, Path(change["png"]))
            info.SetNewData(base_field)
            applied.append(change)
        except Exception as exc:  # noqa: BLE001
            errors.append({"path_id": change["path_id"], "name": change["name"], "error": str(exc)})

    out_assets = OUTPUT / "sharedassets0.assets"
    writer = AssetsFileWriter(str(out_assets))
    base_inst.file.Write(writer, 0)
    writer.Close()

    shutil.copy2(work / "sharedassets0.assets.resS", OUTPUT / "sharedassets0.assets.resS")
    shutil.copy2(work / "sharedassets0.resource", OUTPUT / "sharedassets0.resource")
    manager.UnloadAllAssetsFiles(True)

    return {
        "applied": applied,
        "errors": errors,
        "output_size": out_assets.stat().st_size,
        "input_size": (BASE_DIR / "sharedassets0.assets").stat().st_size,
    }


def main() -> None:
    changes, export_report = export_changes()
    result = apply_changes(changes)
    report = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "tool": "UnityPy export + AssetsTools.NET write",
        "method": "card frame textures only; foil/outline/font atlases stay vanilla 0.70.3",
        **export_report,
        **result,
    }
    report["counts"] = {
        "export_candidates": len(changes),
        "applied": len(result["applied"]),
        "errors": len(result["errors"]),
        "skipped_same": len(export_report["skipped_same"]),
        "skipped_filtered": len(export_report["skipped_filtered"]),
        "skipped_no_mod": len(export_report["skipped_no_mod"]),
        "skipped_unreadable": len(export_report["skipped_unreadable"]),
        "export_errors": len(export_report["export_errors"]),
    }
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    (ROOT / "texture_checklist.txt").write_text(
        "\n".join(sorted(x["name"] for x in result["applied"])), encoding="utf-8"
    )
    print(json.dumps(report["counts"], indent=2))
    print(f"input {report['input_size']} output {report['output_size']}")
    for item in result["applied"]:
        print(f"  {item['name']}")


if __name__ == "__main__":
    main()
