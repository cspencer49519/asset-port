"""Port Genobear sharedassets0 using 0.5 reference + AssetsTools.NET.

Ports card frame textures only. Foil textures are left to Holographic Overhaul;
outline/font atlases stay on vanilla 0.71 so TMP glyph mapping stays valid.
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

import clr

clr.AddReference(r"C:\uabea-windows\AssetsTools.NET.dll")
clr.AddReference(r"C:\uabea-windows\AssetsTools.NET.Texture.dll")

from AssetsTools.NET import AssetsFileWriter, AssetsReplacer, AssetsReplacerFromMemory  # noqa: E402
from AssetsTools.NET.Extra import AssetClassID, AssetsManager  # noqa: E402
from AssetsTools.NET.Texture import TextureFile, TextureFormat  # noqa: E402
from System.Collections.Generic import List  # noqa: E402

ROOT = Path(r"c:\TCGCardShopModWork\asset-port")
BASE = ROOT / "base-071"
VANILLA_05 = ROOT / "vanilla-05"
MOD_ASSETS = ROOT / "mod-062" / "sharedassets0.assets"
MOD_PAIRED = ROOT / "mod-paired-05"
EXPORTED = ROOT / "exported-mod"
OUTPUT = ROOT / "output"
REPORT = ROOT / "port_report.json"
TPK = Path(r"C:\uabea-windows\classdata.tpk")

PORT_TEXTURE = re.compile(
    r"(?:"
    r"^Bat[A-D]$"
    r"|^Card"
    r"|^Ghost_"
    r"|^GradedCard"
    r"|^GradeCard"
    r")",
    re.IGNORECASE,
)

# Mod foil layers fight Holographic Overhaul (rolling garbled backgrounds).
# Outline/font atlases must stay vanilla 0.71 — mod pixels break glyph UV layout.
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


def md5_img(img: Image.Image) -> str:
    return hashlib.md5(img.tobytes()).hexdigest()


def ensure_mod_paired() -> None:
    MOD_PAIRED.mkdir(parents=True, exist_ok=True)
    shutil.copy2(MOD_ASSETS, MOD_PAIRED / "sharedassets0.assets")
    shutil.copy2(VANILLA_05 / "sharedassets0.assets.resS", MOD_PAIRED / "sharedassets0.assets.resS")
    shutil.copy2(VANILLA_05 / "sharedassets0.resource", MOD_PAIRED / "sharedassets0.resource")


def tex_by_name(env) -> dict[str, object]:
    out: dict[str, object] = {}
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        try:
            name = obj.read().m_Name
            if name:
                out[name] = obj
        except Exception:
            pass
    return out


def should_port(name: str) -> bool:
    if SKIP_TEXTURE.search(name):
        return False
    return bool(PORT_TEXTURE.search(name))


def export_changes() -> tuple[list[dict], list[str]]:
    ensure_mod_paired()
    EXPORTED.mkdir(parents=True, exist_ok=True)
    for old in EXPORTED.glob("*.png"):
        old.unlink()

    vanilla = tex_by_name(UnityPy.load(str(VANILLA_05)))
    mod = tex_by_name(UnityPy.load(str(MOD_PAIRED)))
    base = tex_by_name(UnityPy.load(str(BASE)))

    changes: list[dict] = []
    skipped: list[str] = []

    for name in sorted(set(mod) & set(base)):
        if not should_port(name):
            continue
        mod_obj = mod[name]
        base_obj = base[name]
        vanilla_obj = vanilla.get(name)
        try:
            if mod_obj.get_raw_data() == base_obj.get_raw_data():
                skipped.append(f"{name}: same raw bytes as base")
                continue
            mod_img = mod_obj.read().image
            if mod_img is None:
                skipped.append(f"{name}: mod unreadable")
                continue
            if vanilla_obj is not None:
                vanilla_img = vanilla_obj.read().image
                if vanilla_img is not None and md5_img(mod_img) == md5_img(vanilla_img):
                    skipped.append(f"{name}: same pixels as vanilla 0.5")
                    continue
            base_data = base_obj.read()
            safe = "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in name)
            png_path = EXPORTED / f"{safe}.png"
            img = mod_img if mod_img.mode in ("RGB", "RGBA") else mod_img.convert("RGBA")
            img.save(png_path)
            changes.append(
                {
                    "name": name,
                    "path_id": base_obj.path_id,
                    "png": str(png_path),
                    "base_size": [base_data.m_Width, base_data.m_Height],
                }
            )
        except Exception as exc:  # noqa: BLE001
            skipped.append(f"{name}: {exc}")

    return changes, skipped


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


def import_png_into_field(base_field, png_path: Path) -> None:
    tw = base_field["m_Width"].AsInt
    th = base_field["m_Height"].AsInt
    fmt = TextureFormat(base_field["m_TextureFormat"].AsInt)
    bgra, width, height = png_to_bgra(png_path, tw, th)
    enc = TextureFile.Encode(bgra, fmt, width, height)
    if enc is None:
        fmt = TextureFormat.RGBA32
        enc = TextureFile.Encode(bgra, fmt, width, height)
    if enc is None:
        raise RuntimeError(f"encode failed for {fmt}")

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


def apply_changes(changes: list[dict]) -> dict:
    work = ROOT / "work-atnet"
    if work.exists():
        shutil.rmtree(work)
    shutil.copytree(BASE, work)
    if OUTPUT.exists():
        shutil.rmtree(OUTPUT)
    OUTPUT.mkdir(parents=True)

    manager = AssetsManager()
    manager.LoadClassPackage(str(TPK))
    inst = manager.LoadAssetsFile(str(work / "sharedassets0.assets"), False)
    manager.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion)

    by_path = {info.PathId: info for info in inst.file.Metadata.AssetInfos}
    replacers = List[AssetsReplacer]()
    applied: list[dict] = []
    errors: list[dict] = []

    for change in changes:
        info = by_path.get(change["path_id"])
        if info is None:
            errors.append({"name": change["name"], "error": "path_id missing in base"})
            continue
        try:
            base_field = manager.GetBaseField(inst, info)
            import_png_into_field(base_field, Path(change["png"]))
            data = bytes(base_field.WriteToByteArray())
            replacers.Add(AssetsReplacerFromMemory(info.PathId, info.TypeId, 0xFFFF, data))
            applied.append(change)
        except Exception as exc:  # noqa: BLE001
            errors.append({"name": change["name"], "error": str(exc)})

    out_assets = OUTPUT / "sharedassets0.assets"
    writer = AssetsFileWriter(str(out_assets))
    inst.file.Write(writer, 0, replacers, None)
    writer.Close()

    shutil.copy2(work / "sharedassets0.assets.resS", OUTPUT / "sharedassets0.assets.resS")
    shutil.copy2(work / "sharedassets0.resource", OUTPUT / "sharedassets0.resource")

    return {
        "applied": applied,
        "errors": errors,
        "output_size": out_assets.stat().st_size,
        "input_size": (BASE / "sharedassets0.assets").stat().st_size,
    }


def main() -> None:
    changes, skipped = export_changes()
    result = apply_changes(changes)
    report = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "reference": "TCG Card Shop Simulator-0.5 sharedassets0 trio",
        "method": "card frame textures only (no foil/outline atlases), AT.NET write",
        "mod_changes_found": len(changes),
        "skipped_export": skipped,
        "changes": changes,
        **result,
    }
    report["counts"] = {
        "mod_changes_found": len(changes),
        "applied": len(result["applied"]),
        "errors": len(result["errors"]),
        "skipped_export": len(skipped),
    }
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    (ROOT / "texture_checklist.txt").write_text(
        "\n".join(sorted(c["name"] for c in result["applied"])), encoding="utf-8"
    )
    print(json.dumps(report["counts"], indent=2))
    print("input", report["input_size"], "output", report["output_size"])
    print("applied:")
    for c in result["applied"]:
        print(" ", c["name"])
    if result["errors"]:
        print("errors:")
        for e in result["errors"]:
            print(" ", e)


if __name__ == "__main__":
    main()
