# -*- coding: utf-8 -*-
import json, glob, os, sys

def analyze():
    files = sorted(glob.glob("Assets/Resources/Scripts/prologue_*.json"))
    report = []
    for fp in files:
        fname = os.path.basename(fp)
        with open(fp, "r", encoding="utf-8") as f:
            data = json.load(f)
        scenes = data.get("scenes", [])
        
        # 统计
        char_lines = {}
        narrations = []
        dialogues = []
        thoughts = []
        choices = []
        
        for sc_idx, sc in enumerate(scenes):
            bg = sc.get("bg", "")
            bgm = sc.get("bgm", "")
            for entry in sc.get("d", []):
                t = entry.get("t")
                text = entry.get("text", "")
                if t == "d":
                    s = entry.get("s", "未知")
                    char_lines[s] = char_lines.get(s, 0) + 1
                    dialogues.append((s, text))
                elif t == "n":
                    narrations.append(text)
                elif t == "thought":
                    s = entry.get("s", "内心")
                    thoughts.append((s, text))
                elif t == "c":
                    opts = [opt.get("text", "") for opt in entry.get("opts", [])]
                    choices.append(opts)
        
        total_words = sum(len(d[1]) for d in dialogues) + sum(len(n) for n in narrations) + sum(len(t[1]) for t in thoughts)
        report.append({
            "file": fname,
            "scenes_count": len(scenes),
            "dialogue_count": len(dialogues),
            "narration_count": len(narrations),
            "thought_count": len(thoughts),
            "choices_count": len(choices),
            "total_chars": total_words,
            "speakers": char_lines,
            "first_10_lines": (narrations + [f"{d[0]}: {d[1]}" for d in dialogues])[:10],
            "choices": choices
        })
    
    with open("docs/for-ai/vn_script_inspection_raw.json", "w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    print(f"Inspection raw report generated for {len(files)} files.")

if __name__ == "__main__":
    analyze()
