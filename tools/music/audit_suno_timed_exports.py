"""Audit downloaded Suno subtitle exports against source text and local audio evidence.

This never publishes or rewrites game assets. Exported timing can itself be wrong.
"""
import difflib
import hashlib
import json
from pathlib import Path
import re
import subprocess
from extract_ost_lyric_sheets import ROOT, clean

DIRECTORY = ROOT / "tools/music/alignment_review/suno_exports"
SONGS = {"chollima_ride": "1faca03b-156a-41c1-8d4f-c7bc8cb2226a",
         "starlit_rails": "58153497-be67-48f3-b92a-7d490e2827b4"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def seconds(value):
    result = 0.
    for part in value.replace(",", ".").split(":"):
        result = result * 60 + float(part)
    return result


def main():
    summary = ["# Suno同步导出核对", "", "实际导出，不等于时间准确或人工听校通过。未修改Assets字幕。", ""]
    for track, song_id in SONGS.items():
        srt, lrc = DIRECTORY / f"{track}.srt", DIRECTORY / f"{track}.lrc"
        sheet = ROOT / f"Assets/Resources/Music/LyricSheets/{track}.json"
        source = json.loads(sheet.read_text(encoding="utf-8"))["plainText"].splitlines()
        audio = ROOT / f"Assets/Resources/bgm/{track}.mp3"
        duration = float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(audio)]))
        raw = srt.read_text(encoding="utf-8-sig").replace("\r\n", "\n")
        cues, removed, issues = [], [], []
        for block in re.split(r"\n\s*\n", raw.strip()):
            rows = block.strip().splitlines()
            timing_index = next((i for i, row in enumerate(rows) if "-->" in row), None)
            if timing_index is None:
                raise ValueError("Malformed subtitle block")
            start, end = map(seconds, rows[timing_index].split("-->"))
            text = "\n".join(rows[timing_index + 1:])
            plain = clean(text)
            if not plain:
                removed.append(dict(start=start, end=end, text=text))
                continue
            cue = dict(exportCue=rows[0], start=start, end=end, text=plain)
            cues.append(cue)
            flags = []
            if start < 0 or end <= start or end > duration: flags.append("invalid_interval")
            if end - start > 15: flags.append("long_interval_needs_review")
            if end - start < .15: flags.append("very_short_interval_needs_review")
            if len(cues) > 1 and start < cues[-2]["end"] - .001: flags.append("overlaps_previous")
            if flags: issues.append(dict(**cue, flags=flags))
        starts = {}
        for cue in cues: starts.setdefault(round(cue["start"], 3), []).append(cue["exportCue"])
        repeated_starts = [dict(start=start, cues=items) for start, items in starts.items() if len(items) > 1]
        lrc_rows = []
        for row in lrc.read_text(encoding="utf-8-sig").splitlines():
            match = re.fullmatch(r"\[(\d+:\d+\.\d+)\](.*)", row)
            if match and (plain := clean(match[2])): lrc_rows.append(dict(start=seconds(match[1]), text=plain))
        format_agreement = len(lrc_rows) == len(cues) and all(
            a["text"] == b["text"] and abs(a["start"] - b["start"]) <= .011 for a, b in zip(lrc_rows, cues))
        texts = [cue["text"] for cue in cues]
        matcher = difflib.SequenceMatcher(None, source, texts, autojunk=False)
        mapping = {}
        for block in matcher.get_matching_blocks():
            for offset in range(block.size): mapping[block.a + offset] = cues[block.b + offset]
        variant = "spoken.local.sections.allsections" if track == "chollima_ride" else "local.sections"
        phonetic_path = ROOT / f"tools/music/alignment_review/phonetic/{track}.{variant}.json"
        phonetic = json.loads(phonetic_path.read_text(encoding="utf-8"))
        if phonetic["audioSha256"] != digest(audio) or phonetic["sourceSha256"] != digest(sheet):
            raise ValueError("Local comparison evidence is stale")
        comparisons = []
        for index, text in enumerate(source):
            cue = mapping.get(index)
            alternatives = [dict(role=run["role"], start=run["lines"][index]["start"], end=run["lines"][index]["end"])
                            for run in phonetic["rounds"]]
            differences = [] if cue is None else [dict(role=a["role"], start=abs(a["start"] - cue["start"]), end=abs(a["end"] - cue["end"])) for a in alternatives]
            comparisons.append(dict(line=index + 1, text=text, exported=cue, local=alternatives, differences=differences))
        report = dict(id=track, songPage=f"https://suno.com/song/{song_id}",
                      acquisition="User-authorized Suno Lyric Downloader visible LRC/SRT buttons; no audio download",
                      sourceSha256=digest(sheet), audioSha256=digest(audio), srtSha256=digest(srt), lrcSha256=digest(lrc),
                      localEvidenceSha256=digest(phonetic_path), duration=duration, sourceLineCount=len(source),
                      cleanedCueCount=len(cues), sourceSequenceMatches=texts == source, lrcSrtAgreement=format_agreement,
                      removedDirections=removed, repeatedStarts=repeated_starts, intervalIssues=issues,
                      comparisons=comparisons, humanListeningVerified=False, status="exported_timing_needs_validation")
        (DIRECTORY / f"{track}.audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        # Keep the provider's actual export sequence, including discrepancies; never force it to the authored count.
        review_lines, review_issues = [], []
        for index, cue in enumerate(cues):
            line = dict(line=index + 1, text=cue["text"], start=cue["start"], end=cue["end"])
            review_lines.append(line)
            flags = [flag for issue in issues if issue["exportCue"] == cue["exportCue"] for flag in issue["flags"]]
            if len(starts[round(cue["start"], 3)]) > 1: flags.append("shared_export_start")
            if not report["sourceSequenceMatches"]: flags.append("export_differs_from_authored_sequence")
            if flags: review_issues.append(dict(line=index + 1, flags=flags))
        review = dict(id=track, model="Suno synchronized export / not listening approved", duration=duration,
                      audioSha256=report["audioSha256"], lineCount=len(review_lines), lines=review_lines,
                      issues=review_issues, status="exported_timing_needs_validation", humanListeningVerified=False,
                      sourceSequenceMatches=report["sourceSequenceMatches"], srtSha256=report["srtSha256"],
                      auditFile=f"suno_exports/{track}.audit.json")
        (DIRECTORY / f"{track}.suno-export.review.json").write_text(json.dumps(review, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        summary += [f"## {track}", "", f"原稿{len(source)}行；清理后导出{len(cues)}条；原稿顺序一致：{texts == source}；LRC/SRT一致：{format_agreement}。",
                    f"区间异常{len(issues)}条、重复起点{len(repeated_starts)}组。", "",
                    "|原稿句|原文|导出起止秒|本地原曲起止秒|", "|---|---|---|---|"]
        for row in comparisons:
            cue = row["exported"]
            if cue is None or any(d["start"] > .5 or d["end"] > 1 for d in row["differences"]):
                interval = "未可靠映射" if cue is None else f"{cue['start']:.3f}–{cue['end']:.3f}"
                a = row["local"][0]
                summary.append(f"|{row['line']}|{row['text']}|{interval}|{a['start']:.3f}–{a['end']:.3f}|")
        summary.append("")
        print(f"{track}: source={len(source)} export={len(cues)} sequence={texts == source} repeated={len(repeated_starts)} intervalIssues={len(issues)} formatsAgree={format_agreement}")
    (DIRECTORY / "SUMMARY.md").write_text("\n".join(summary) + "\n", encoding="utf-8")


if __name__ == "__main__": main()
