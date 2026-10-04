#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""复古画风（低颗粒）预渲染 v3 —— 邻域推算（Scale2x/EPX）+ 调色板量化 + 离线预渲染。

算法链（对应「根据周围格子推算新渲染格」）：
    1) BOX 重采样缩到目标宽 target_width（区域平均＝邻域信息聚合）
    2) 中位切分调色板量化到 N 色（像素画调色板感，压缩噪声）
    3) Scale2x（EPX 邻域规则）放大若干轮 —— 每个新格由其 2×2 邻域推算
       （A/D/B/C 邻域比较决定衍生格取邻居色还是本色，边缘延伸平滑、非硬马赛克）
    4) NEAREST 微调回原尺寸（保持整数块结构）

输出到 Resources/bg_low/；运行时经「设置 → 复古画风」直接加载（零开销）。
用法：
    python tools/pixelate_bg.py                 # v3 默认 target_width=480, colors=64
    python tools/pixelate_bg.py --width 640 --colors 96
    python tools/pixelate_bg.py --only village_sunset
"""
import argparse
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets" / "Resources" / "bg"
DST = ROOT / "Assets" / "Resources" / "bg_low"


def scale2x(im: Image.Image) -> Image.Image:
    """Scale2x/EPX 单轮：2x 放大，新格颜色由邻域规则推算。"""
    w, h = im.size
    px = im.load()
    out = Image.new(im.mode, (w * 2, h * 2))
    op = out.load()
    for y in range(h):
        for x in range(w):
            a = px[x, y]
            b = px[x + 1, y] if x + 1 < w else a   # 右
            c = px[x - 1, y] if x > 0 else a       # 左
            d = px[x, y - 1] if y > 0 else a       # 上
            e = px[x, y + 1] if y + 1 < h else a   # 下
            # EPX 四规则（经典 Scale2x）
            a1 = d if (d == b and d != a and b != c) else a
            a2 = c if (d == a and d != c and a != b) else a
            a3 = e if (b == a and b != e and a != d) else a
            a4 = a if (e == c and e != a and c != d) else e
            X, Y = x * 2, y * 2
            op[X, Y] = a1
            op[X + 1, Y] = a2
            op[X, Y + 1] = a3
            op[X + 1, Y + 1] = a4
    return out


def retro_v3(im: Image.Image, target_width: int, colors: int) -> Image.Image:
    w, h = im.size
    tw = max(64, min(target_width, w))
    th = max(1, round(h * tw / w))
    # 1) 邻域平均缩图
    small = im.resize((tw, th), Image.BOX)
    # 2) 调色板量化
    q = small.quantize(colors=colors, method=Image.MEDIANCUT).convert("RGB")
    # 3) Scale2x 轮数：直到覆盖原短边（上限 4 轮）
    out = q
    rounds = 0
    while min(out.size) < min(w, h) and rounds < 4:
        out = scale2x(out)
        rounds += 1
    # 4) NEAREST 收尾到原尺寸
    if out.size != (w, h):
        out = out.resize((w, h), Image.NEAREST)
    return out


def main() -> None:
    ap = argparse.ArgumentParser(description="复古画风预渲染 v3（邻域推算+量化）")
    ap.add_argument("--width", type=int, default=480, help="目标宽度（默认480）")
    ap.add_argument("--colors", type=int, default=64, help="调色板色数（默认64）")
    ap.add_argument("--only", default="", help="只处理文件名含子串的图")
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
        out = retro_v3(im, args.width, args.colors)
        dst = DST / f.name
        out.save(dst, optimize=True)
        print(f"[v3 w{args.width} c{args.colors}] {f.name}: {im.size[0]}x{im.size[1]} -> bg_low/")

    print(f"完成：{len(files)} 张 → {DST.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
