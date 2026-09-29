#!/usr/bin/env python3
"""低颗粒（复古）像素后处理。

源图保持不变（高颗粒生成），本脚本产出「低颗粒」变体：
最近邻缩小 ÷factor → 最近邻放大回原尺寸 = 像素块放大 factor 倍（复古感）。
输出到 Resources/bg_low/，游戏内经「设置 → 显示 → 复古像素」开关切换加载。

用法：
  python tools/pixelate_bg.py              # 默认 factor=3，处理 Assets/Resources/bg 下全部 png
  python tools/pixelate_bg.py --factor 2   # 轻微（块×2）
  python tools/pixelate_bg.py --only village_sunset
"""
import argparse
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets" / "Resources" / "bg"
DST = ROOT / "Assets" / "Resources" / "bg_low"


def pixelate(im: Image.Image, factor: int) -> Image.Image:
    w, h = im.size
    cw, ch = w // factor * factor, h // factor * factor  # 裁到整倍数，避免半像素
    if (cw, ch) != (w, h):
        im = im.crop((0, 0, cw, ch))
    small = im.resize((cw // factor, ch // factor), Image.NEAREST)
    return small.resize((cw, ch), Image.NEAREST)


def main() -> None:
    ap = argparse.ArgumentParser(description="生成低颗粒复古变体")
    ap.add_argument("--factor", type=int, default=3, help="像素块放大倍数（默认3）")
    ap.add_argument("--only", default="", help="只处理文件名含该子串的图（如 village_sunset）")
    args = ap.parse_args()

    DST.mkdir(parents=True, exist_ok=True)
    files = sorted(SRC.glob("*.png"))
    if args.only:
        files = [f for f in files if args.only in f.name]
    if not files:
        print("没有找到源图:", SRC)
        return

    for f in files:
        im = Image.open(f).convert("RGB")
        out = pixelate(im, args.factor)
        dst = DST / f.name
        out.save(dst, optimize=True)
        print(f"[x{args.factor}] {f.name}: {im.size[0]}x{im.size[1]} -> {dst.relative_to(ROOT)}")

    print(f"完成：{len(files)} 张 → {DST.relative_to(ROOT)}（Unity 下次聚焦自动导入）")


if __name__ == "__main__":
    main()
