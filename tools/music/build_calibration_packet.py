"""Build source-preserving review reports and original-audio clips for disputed boundaries."""
import hashlib
import json
from pathlib import Path
import subprocess
from align_ost_lyrics import ROOT, write_json


def main():
    folder = ROOT / "tools/music/alignment_review"
    clips = folder / "calibration"; clips.mkdir(exist_ok=True)
    summary = ["# 歌词时间校准复核包", "", "所有时间仍为自动证据；模型一致不等于听音通过。原稿不删改；南风39句已接入明确标注待听校的自动校准稿。", "", "## Whisper 局部交叉诊断（历史轮次）", "",
               "|曲目|原稿行数|局部双模型边界争议|", "|---|---:|---:|"]
    notes = []
    for track in ("south_wind", "starlit_rails", "chollima_ride", "sleepers"):
        path = folder / "local_consensus" / f"{track}.g4.c2.json"
        report = json.loads(path.read_text(encoding="utf-8"))
        source = json.loads((ROOT / f"Assets/Resources/Music/LyricSheets/{track}.json").read_text(encoding="utf-8"))
        if [line["text"] for line in report["lines"]] != source["plainText"].splitlines():
            raise ValueError("Review source changed: " + track)
        audio = ROOT / f"Assets/Resources/bgm/{track}.mp3"
        if hashlib.sha256(audio.read_bytes()).hexdigest() != report["audioSha256"]:
            raise ValueError("Review recording changed: " + track)
        report.update(model="base+small / local context", lineCount=len(report["lines"]), expectedLineCount=len(report["lines"]),
                      issues=[dict(line=line["line"], flags=line["flags"], text=line["text"], start=line["start"], end=line["end"]) for line in report["lines"] if line["flags"]])
        write_json(folder / "local_consensus" / f"{track}.local-review.review.json", report)
        summary.append(f"|{track}|{report['lineCount']}|{len(report['issues'])}|")
        if track == "south_wind":
            notes += ["", "南风疑点片段（原曲裁取，未变速；时间列是原曲区间）：", ""]
            for issue in report["issues"]:
                number = issue["line"]
                candidates = [run["lines"][number - 1] for run in report["rounds"]]
                start = max(0, min(line["start"] for line in candidates) - 2)
                end = min(report["duration"], max(line["end"] for line in candidates) + 2)
                clip = clips / f"south_wind-line-{number:02d}.mp3"
                subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", str(start), "-i", str(audio), "-t", str(end - start), "-c:a", "libmp3lame", "-q:a", "3", str(clip)], check=True)
                notes.append(f"- 第{number}句，原曲 {start:.2f}–{end:.2f}s：{issue['text']}；[试听]({clip.name})")
    summary += ["", "## 最新音素复核与自动发布门槛", "", "失败数为检查项数，不等于句数；没有人工听校通过声明。", "", "|曲目|自动门槛未通过项|处理|", "|---|---:|---|"]
    for track in ("south_wind", "starlit_rails", "chollima_ride", "sleepers"):
        latest = {"chollima_ride": "chollima_ride.spoken.local.sections.allsections.provider.boundaries",
                  "starlit_rails": "starlit_rails.local.sections.provider.boundaries"}
        variant = latest.get(track, track)
        check = folder / "phonetic" / f"{variant}.publication-check.json"
        if not check.exists(): continue
        validation = json.loads(check.read_text(encoding="utf-8"))
        failures = len(validation["failures"])
        state = "39句自动校准逐行稿已接入，待听校" if track == "south_wind" and failures == 0 else "新音素候选未发布，保留既有阅读/草稿"
        summary.append(f"|{track}|{failures}|{state}|")
        candidate = folder / "phonetic" / f"{variant}.json"
        if track in ("starlit_rails", "chollima_ride") and candidate.exists():
            evidence = json.loads(candidate.read_text(encoding="utf-8"))
            audio = ROOT / f"Assets/Resources/bgm/{track}.mp3"
            if hashlib.sha256(audio.read_bytes()).hexdigest() != evidence["audioSha256"]:
                raise ValueError("Candidate recording changed: " + track)
            source = json.loads((ROOT / f"Assets/Resources/Music/LyricSheets/{track}.json").read_text(encoding="utf-8"))
            for run in evidence["rounds"]:
                if [line["text"] for line in run["lines"]] != source["plainText"].splitlines():
                    raise ValueError("Candidate source changed: " + track)
            notes += ["", f"{track} 最新未通过检查的原曲片段（{variant}，不是已听校结论）：", ""]
            manifest = []
            for number in sorted({failure["line"] for failure in validation["failures"]}):
                rows = [run["lines"][number - 1] for run in evidence["rounds"]]
                start = max(0, min(row["start"] for row in rows) - 2)
                end = max(row["end"] for row in rows) + 2
                clip = clips / f"{variant}-line-{number:02d}.mp3"
                subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", str(start), "-i", str(audio), "-t", str(end - start), "-c:a", "libmp3lame", "-q:a", "3", str(clip)], check=True)
                manifest.append(dict(line=number, text=rows[0]["text"], originalAudioStart=start, originalAudioEnd=end,
                                     file=clip.name, sha256=hashlib.sha256(clip.read_bytes()).hexdigest(), humanListeningVerified=False))
                notes.append(f"- 第{number}句，原曲 {start:.2f}–{end:.2f}s：{rows[0]['text']}；[试听]({clip.name})")
            write_json(clips / f"{variant}.clips.json", dict(audioSha256=evidence["audioSha256"],
                       reportSha256=hashlib.sha256(candidate.read_bytes()).hexdigest(), clips=manifest))
    summary += notes + ["", "以上试听片段仍保留历史疑点，南风最新时轴见Assets/Resources/Music/Lyrics/south_wind.txt及其info证据索引。", ""]
    (clips / "SUMMARY.md").write_text("\n".join(summary) + "\n", encoding="utf-8")
    print(clips / "SUMMARY.md")


if __name__ == "__main__": main()
