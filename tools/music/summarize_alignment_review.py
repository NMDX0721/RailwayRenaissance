"""Summarize local alignment evidence without promoting candidates or claiming listening."""
import collections
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "tools/music/alignment_review"


def main():
    rows = []
    for path in sorted(OUT.glob("*/*.review.json")):
        report = json.loads(path.read_text(encoding="utf-8"))
        counts = collections.Counter(flag for issue in report["issues"] for flag in issue["flags"])
        agreements = [line["asrAgreement"] for line in report["lines"]]
        rows.append(dict(
            file=str(path.relative_to(OUT)), id=report["id"], model=report["model"],
            lines=report["lineCount"], expectedLines=report["expectedLineCount"],
            meanSourceRecall=round(sum(agreements) / max(1, len(agreements)), 3),
            minimumSourceRecall=min(agreements, default=0), flags=dict(counts),
            humanListeningVerified=report.get("humanListeningVerified", False),
            audioSha256=report["audioSha256"],
        ))
    (OUT / "summary.json").write_text(json.dumps(rows, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    lines = ["# 原声自动对齐证据汇总", "", "自动指标用于定位疑点，不能代替听音核对；不修改游戏资源。", "",
             "|候选|行数|平均原文字符召回率|最低召回率|异常|", "|---|---:|---:|---:|---|"]
    for row in rows:
        flags = ", ".join(f"{key}: {value}" for key, value in row["flags"].items()) or "无自动异常"
        lines.append(f"|{row['file']}|{row['lines']}/{row['expectedLines']}|{row['meanSourceRecall']:.3f}|{row['minimumSourceRecall']:.3f}|{flags}|")
    lines += ["", "各候选的原音频哈希及独立识别/强制对齐详情见对应 review.json；不同模型的一致不构成人工听校。"]
    lines += ["", "## 原稿与独立识别差异", "", "以下为全曲单调匹配，区别于上表的局部时间窗口比较；包括低文字召回、压缩时间和零时长识别词。", "",
              "|曲目|模型|需核对行数|", "|---|---|---:|"]
    for path in sorted(OUT.glob("*/*.evidence.json")):
        evidence = json.loads(path.read_text(encoding="utf-8"))
        count = sum(line["needsReview"] for line in evidence["lines"])
        lines.append(f"|{evidence['id']}|{evidence['model']}|{count}/{len(evidence['lines'])}|")
    (OUT / "SUMMARY.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(OUT / "SUMMARY.md")


if __name__ == "__main__":
    main()
