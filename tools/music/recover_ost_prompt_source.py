"""Recover the authored vocal prompts from Git; preserve embedded lyrics separately."""
import difflib
import hashlib
import json
from pathlib import Path
import re
import subprocess
from extract_ost_lyric_sheets import ROOT, clean

REVISION = "2318294a3fb98cccfe84eb3adc0ac1fe83a6fde0"
PATH = "docs/compose/specs/AI配乐提示词.md"
IDS = ("sleepers", "starlit_rails", "south_wind", "chollima_ride")


def main():
    raw = subprocess.check_output(["git", "show", f"{REVISION}:{PATH}"], cwd=ROOT)
    document = raw.decode("utf-8")
    archive = ROOT / "参考资料/资产/AI配乐提示词_历史原稿.md"
    archive.write_text("<!-- 历史原稿追溯恢复；来源 " + REVISION + ":" + PATH + "；正文未经改写。当前美术提词总集未保留全部歌词，不据此改写已归档美术提示。 -->\n\n" + document, encoding="utf-8")
    sections = re.split(r"(?m)^### 4\.[1-4] ", document)[1:]
    if len(sections) != 4:
        raise ValueError("Expected four authored vocal sections")
    reports = []
    for track, section in zip(IDS, sections):
        prompt = section.split("[Lyrics]", 1)[1].split("```", 1)[0].strip()
        plain = clean(prompt)
        path = ROOT / f"Assets/Resources/Music/LyricSheets/{track}.json"
        sheet = json.loads(path.read_text(encoding="utf-8"))
        embedded = clean(sheet["originalText"])
        sheet.update(embeddedPlainText=embedded, plainText=plain, promptText=prompt,
                     source="AI配乐提词原稿（历史版本追溯）", promptRevision=REVISION,
                     promptDocument=str(archive.relative_to(ROOT)), promptDocumentSha256=hashlib.sha256(raw).hexdigest())
        path.write_text(json.dumps(sheet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        reports.append(dict(id=track, embeddedLineCount=len(embedded.splitlines()), promptLineCount=len(plain.splitlines()),
                            diff=list(difflib.unified_diff(embedded.splitlines(), plain.splitlines(), fromfile="embedded", tofile="authored_prompt", lineterm=""))))
        print(f"{track}: authored {len(plain.splitlines())}, embedded {len(embedded.splitlines())}; embedded original preserved")
    target = ROOT / "tools/music/alignment_review/prompt-source-comparison.json"
    target.write_text(json.dumps(dict(revision=REVISION, document=PATH, comparisons=reports), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
