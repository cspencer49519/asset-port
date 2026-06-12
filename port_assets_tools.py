"""Port mod textures using AssetsTools.NET (UABEA-compatible write path).

UnityPy save corrupts font/UI references; this writer preserves the full
assets file structure and only replaces selected Texture2D byte payloads.
"""
from __future__ import annotations

import json
import re
import shutil
import struct
from datetime import datetime, timezone
from pathlib import Path

import clr

clr.AddReference(r"C:\uabea-windows\AssetsTools.NET.dll")
clr.AddReference(r"C:\uabea-windows\AssetsTools.NET.Texture.dll")

from AssetsTools.NET import AssetsFileWriter, AssetsReplacer  # noqa: E402
from AssetsTools.NET.Extra import AssetClassID, AssetsManager  # noqa: E402
from AssetsTools.NET.Texture import TextureFile, TextureFormat  # noqa: E402
from PIL import Image  # noqa: E402
from System.Collections.Generic import List  # noqa: E402

ROOT = Path(r"c:\TCGCardShopModWork\asset-port")
BASE_DIR = ROOT / "base-071"
MOD_ASSETS = ROOT / "mod-062" / "sharedassets0.assets"
MOD_PAIRED = ROOT / "mod-paired"
EXPORTED = ROOT / "exported-mod"
OUTPUT = ROOT / "output"
REPORT = ROOT / "port_report_atnet.json"
TPK = Path(r"C:\uabea-windows\classdata.tpk")

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
        shutil.copy2(BASE_DIR / name, MOD_PAIRED / name)


def bgra_to_png(bgra: bytes, width: int, height: int, path: Path) -> None:
    rgba = bytearray(len(bgra))
    for i in range(0, len(bgra), 4):
        rgba[i] = bgra[i + 2]
        rgba[i + 1] = bgra[i + 1]
        rgba[i + 2] = bgra[i]
        rgba[i + 3] = bgra[i + 3]
    img = Image.frombytes("RGBA", (width, height), bytes(rgba))
    img = img.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)


def png_to_bgra(path: Path) -> tuple[bytes, int, int]:
    img = Image.open(path).convert("RGBA")
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


def import_png_into_base(base_field, png_path: Path) -> None:
    fmt = TextureFormat(base_field["m_TextureFormat"].AsInt)
    bgra, width, height = png_to_bgra(png_path)
    enc = TextureFile.Encode(bgra, fmt, width, height)
    if enc is None:
        raise RuntimeError(f"Encode failed for format {fmt}")

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

    image_data = base_field["image data"]
    image_data.AsByteArray = enc


def port_textures() -> dict:
    ensure_mod_paired()
    EXPORTED.mkdir(parents=True, exist_ok=True)
    if OUTPUT.exists():
        shutil.rmtree(OUTPUT)
    OUTPUT.mkdir(parents=True)

    work = ROOT / "work-atnet"
    if work.exists():
        shutil.rmtree(work)
    shutil.copytree(BASE_DIR, work)

    manager = AssetsManager()
    manager.LoadClassPackage(str(TPK))
    base_inst = manager.LoadAssetsFile(str(work / "sharedassets0.assets"), False)
    mod_inst = manager.LoadAssetsFile(str(MOD_PAIRED / "sharedassets0.assets"), False)
    manager.LoadClassDatabaseFromPackage(base_inst.file.Metadata.UnityVersion)

    mod_infos = {
        info.PathId: info
        for info in mod_inst.file.Metadata.AssetInfos
        if info.TypeId == int(AssetClassID.Texture2D)
    }

    report = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "tool": "AssetsTools.NET",
        "applied": [],
        "skipped_same": [],
        "skipped_filtered": [],
        "skipped_no_mod": [],
        "skipped_unreadable": [],
        "errors": [],
    }

    replacers = List[AssetsReplacer]()

    for info in base_inst.file.Metadata.AssetInfos:
        if info.TypeId != int(AssetClassID.Texture2D):
            continue

        mod_info = mod_infos.get(info.PathId)
        if mod_info is None:
            report["skipped_no_mod"].append({"path_id": info.PathId})
            continue

        base_field = manager.GetBaseField(base_inst, info)
        mod_field = manager.GetBaseField(mod_inst, mod_info)
        name = base_field["m_Name"].AsString

        if base_field.WriteToByteArray() == mod_field.WriteToByteArray():
            report["skipped_same"].append({"path_id": info.PathId, "name": name})
            continue

        if not PORT_NAME.match(name):
            report["skipped_filtered"].append({"path_id": info.PathId, "name": name})
            continue

        try:
            mod_tex = TextureFile.ReadTextureFile(mod_field)
            mod_bgra = mod_tex.GetTextureData(mod_inst)
            if mod_bgra is None or len(mod_bgra) == 0:
                report["skipped_unreadable"].append(
                    {"path_id": info.PathId, "name": name, "error": "empty mod texture data"}
                )
                continue

            safe = "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in name)
            png_path = EXPORTED / f"{safe}.png"
            bgra_to_png(mod_bgra, mod_tex.m_Width, mod_tex.m_Height, png_path)
            import_png_into_base(base_field, png_path)
            info.SetNewData(base_field)
            report["applied"].append(
                {
                    "path_id": info.PathId,
                    "name": name,
                    "png": str(png_path),
                    "base_size": [base_field["m_Width"].AsInt, base_field["m_Height"].AsInt],
                }
            )
        except Exception as exc:  # noqa: BLE001
            report["errors"].append({"path_id": info.PathId, "name": name, "error": str(exc)})

    out_assets = OUTPUT / "sharedassets0.assets"
    OUTPUT.mkdir(parents=True, exist_ok=True)
    writer = AssetsFileWriter(str(out_assets))
    base_inst.file.Write(writer, 0, replacers, None)
    writer.Close()

    shutil.copy2(work / "sharedassets0.assets.resS", OUTPUT / "sharedassets0.assets.resS")
    shutil.copy2(work / "sharedassets0.resource", OUTPUT / "sharedassets0.resource")

    manager.UnloadAllAssetsFiles(True)

    report["counts"] = {k: len(v) for k, v in report.items() if isinstance(v, list)}
    report["output_size"] = out_assets.stat().st_size
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    (ROOT / "texture_checklist.txt").write_text(
        "\n".join(sorted(x["name"] for x in report["applied"])), encoding="utf-8"
    )
    return report


def main() -> None:
    report = port_textures()
    print(json.dumps({"counts": report["counts"], "output_size": report["output_size"]}, indent=2))


if __name__ == "__main__":
    main()
