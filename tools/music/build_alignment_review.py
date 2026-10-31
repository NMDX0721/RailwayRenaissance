"""Create a local audio/cue review page. No server, accounts or uploads."""
import hashlib
import json
import numpy as np
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "tools/music/alignment_review"


def main():
    data = [dict(json.loads(path.read_text(encoding="utf-8")), reviewFile=str(path.relative_to(OUT))) for path in sorted(OUT.glob("*/*.review.json"))]
    phonetic_paths = list(sorted((OUT / "phonetic").glob("*.json")))
    published = set()
    for info_path in (ROOT / "Assets/Resources/Music/Lyrics").glob("*_info.json"):
        info = json.loads(info_path.read_text(encoding="utf-8"))
        if info.get("evidenceReport"):
            evidence = ROOT / info["evidenceReport"]
            if evidence.exists(): phonetic_paths.append(evidence); published.add(evidence.resolve())
    durations = {}
    for path in phonetic_paths:
        report = json.loads(path.read_text(encoding="utf-8"))
        if not report.get("rounds") or not str(report.get("model", "")).startswith("torchaudio MMS_FA"): continue
        track = report["id"]
        if track not in durations:
            durations[track] = float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(ROOT / f"Assets/Resources/bgm/{track}.mp3")]))
        for run in report["rounds"]:
            data.append(dict(id=track, model=report["model"] + " / " + run["role"], reviewFile=str(path.relative_to(OUT)),
                             publishedDraft=path.resolve() in published and run["role"] == "original",
                             audioSha256=report["audioSha256"], duration=durations[track], lineCount=len(run["lines"]),
                             status=report["status"], humanListeningVerified=False, lines=run["lines"],
                             issues=[dict(line=line["line"], flags=line["flags"]) for line in run["lines"] if line["flags"]]))
    waveforms = {}
    cache = ROOT / "Library/MusicAlignment/review_waveforms"
    cache.mkdir(parents=True, exist_ok=True)
    for report in data:
        report["reviewKey"] = hashlib.sha256(json.dumps(report, sort_keys=True, ensure_ascii=False).encode()).hexdigest()
        track = report["id"]
        if track in waveforms:
            continue
        audio = ROOT / f"Assets/Resources/bgm/{track}.mp3"
        digest = hashlib.sha256(audio.read_bytes()).hexdigest()
        if report["audioSha256"] != digest:
            raise ValueError(f"Stale candidate audio: {track}")
        cached = cache / (digest + ".json")
        if cached.exists():
            waveforms[track] = json.loads(cached.read_text(encoding="utf-8"))
            continue
        pcm = np.frombuffer(subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(audio), "-ac", "1", "-ar", "8000", "-f", "f32le", "pipe:1"]), dtype="<f4")
        bins = [pcm[i:i+160] for i in range(0, len(pcm), 160)]
        peaks = [round(float(value), 5) for block in bins for value in (block.min(), block.max())]
        waveforms[track] = dict(duration=len(pcm)/8000, peaks=peaks, peak=float(np.max(np.abs(pcm))), audioSha256=digest)
        cached.write_text(json.dumps(waveforms[track]), encoding="utf-8")
    page = Path(__file__).with_name("review_ui.html").read_text(encoding="utf-8")
    for token, value in [("__REPORTS__", data), ("__WAVEFORMS__", waveforms)]:
        page = page.replace(token, json.dumps(value, ensure_ascii=False).replace("<", "\\u003c"))
    (OUT / "index.html").write_text(page, encoding="utf-8")
    print(OUT / "index.html")


if __name__ == "__main__":
    main()
