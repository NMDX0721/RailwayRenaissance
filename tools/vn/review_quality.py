# -*- coding: utf-8 -*-
import json, glob, os

files = sorted(glob.glob("Assets/Resources/Scripts/prologue_*.json"))

script_analysis = {}

for fp in files:
    fname = os.path.basename(fp)
    with open(fp, "r", encoding="utf-8") as f:
        data = json.load(f)
    scenes = data.get("scenes", [])
    
    scene_summaries = []
    for s_idx, sc in enumerate(scenes):
        entries = sc.get("d", [])
        d_list = []
        n_list = []
        t_list = []
        for e in entries:
            t = e.get("t")
            if t == "d":
                d_list.append((e.get("s"), e.get("text")))
            elif t == "n":
                n_list.append(e.get("text"))
            elif t == "thought":
                t_list.append((e.get("s"), e.get("text")))
        scene_summaries.append({
            "idx": s_idx,
            "bg": sc.get("bg"),
            "bgm": sc.get("bgm"),
            "dialogue_sample": d_list[:6],
            "dialogue_tail": d_list[-4:],
            "narration_sample": n_list[:4],
            "total_d": len(d_list),
            "total_n": len(n_list),
            "total_t": len(t_list)
        })
    script_analysis[fname] = scene_summaries

with open("docs/for-ai/vn_chapter_diagnostics.json", "w", encoding="utf-8") as f:
    json.dump(script_analysis, f, ensure_ascii=False, indent=2)

print("Diagnostics JSON exported successfully.")
