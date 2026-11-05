# -*- coding: utf-8 -*-
import json, glob, os

def dump_dialogues():
    files = sorted(glob.glob("Assets/Resources/Scripts/prologue_*.json"))
    out_lines = []
    for fp in files:
        fname = os.path.basename(fp)
        with open(fp, "r", encoding="utf-8") as f:
            data = json.load(f)
        out_lines.append(f"\n========================================================")
        out_lines.append(f"FILE: {fname}")
        out_lines.append(f"========================================================")
        for s_idx, sc in enumerate(data.get("scenes", [])):
            out_lines.append(f"\n--- Scene {s_idx} [bg: {sc.get('bg')}, bgm: {sc.get('bgm')}] ---")
            for entry in sc.get("d", []):
                t = entry.get("t")
                if t == "d":
                    out_lines.append(f"{entry.get('s')}: {entry.get('text')}")
                elif t == "n":
                    out_lines.append(f"  [旁白] {entry.get('text')}")
                elif t == "thought":
                    out_lines.append(f"  [{entry.get('s')}内心] {entry.get('text')}")
                elif t == "c":
                    opts = [o.get('text') for o in entry.get('opts', [])]
                    out_lines.append(f"  [分支选择] {opts}")
                elif t == "scroll":
                    out_lines.append(f"  [滚动快讯] {entry.get('text')}")
                elif t == "loc":
                    out_lines.append(f"  [地点卡] {entry.get('text')}")

    with open("docs/for-ai/all_prologues_dump.txt", "w", encoding="utf-8") as f:
        f.write("\n".join(out_lines))
    print(f"Dumped {len(files)} files into docs/for-ai/all_prologues_dump.txt")

if __name__ == "__main__":
    dump_dialogues()
