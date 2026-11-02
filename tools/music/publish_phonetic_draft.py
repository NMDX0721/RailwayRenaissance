"""Publish line-only automatic drafts after explicit cross-method evidence checks."""
import argparse
import hashlib
import json
import subprocess
import shutil
import math
from pathlib import Path
from align_ost_lyrics import ROOT, normalized, stamp, write_json


def valid_word_intervals(line):
    if (not math.isfinite(line['start']) or not math.isfinite(line['end'])
            or not 0 <= line['start'] < line['end']):
        return False
    previous = line['start']
    for word in line['words']:
        start, end = word['start'], word['end']
        if (not math.isfinite(start) or not math.isfinite(end) or end <= start
                or start < previous - .001 or end > line['end'] + .001):
            return False
        previous = end
    return bool(line['words'])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("id", choices=["south_wind", "starlit_rails", "chollima_ride", "sleepers"])
    parser.add_argument("--variant", choices=["", "spoken", "local", "spoken.local", "spoken.local.sections", "local.sections", "spoken.local.sections.allsections", "local.sections.allsections", "local.sections.provider", "spoken.local.sections.allsections.provider", "local.sections.provider.boundaries", "local.sections.provider.boundaries.ordered", "spoken.local.sections.allsections.provider.boundaries"], default="")
    args = parser.parse_args()
    report_key = args.id + ("." + args.variant if args.variant else "")
    report_path = ROOT / f"tools/music/alignment_review/phonetic/{report_key}.json"
    report = json.loads(report_path.read_text(encoding="utf-8"))
    if report["id"] != args.id: raise ValueError("Wrong song report")
    sheet_path = ROOT / f"Assets/Resources/Music/LyricSheets/{args.id}.json"
    sheet = json.loads(sheet_path.read_text(encoding="utf-8"))
    if report["sourceSha256"] != hashlib.sha256(sheet_path.read_bytes()).hexdigest(): raise ValueError("Stale lyric source")
    audio_path = ROOT / f"Assets/Resources/bgm/{args.id}.mp3"
    if report["audioSha256"] != hashlib.sha256(audio_path.read_bytes()).hexdigest(): raise ValueError("Stale audio source")
    primary = next(run for run in report["rounds"] if run["role"] == "original")["lines"]
    alternate = next(run for run in report["rounds"] if run["role"] == "vocals")["lines"]
    if [line["text"] for line in primary] != sheet["plainText"].splitlines() or [line["text"] for line in alternate] != sheet["plainText"].splitlines(): raise ValueError("Changed source text")
    duration = float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(audio_path)]))
    asr = []
    for key in (args.id, args.id + ".authored-vocals"):
        path = ROOT / f"tools/music/alignment_review/small/{key}.asr.json"
        meta = path.with_name(path.name.replace(".asr.json", ".asr.meta.json"))
        if path.exists() and meta.exists() and json.loads(meta.read_text())["audioSha256"] == report["audioSha256"]:
            asr.append((str(path.relative_to(ROOT)), json.loads(path.read_text(encoding="utf-8"))))
    validations, failures, previous = [], [], 0
    for line, other in zip(primary, alternate):
        if not valid_word_intervals(line):
            failures.append(dict(line=line['line'], reason='invalid_original_word_intervals'))
        if line["flags"] or line["start"] < previous - .01 or line["end"] <= line["start"] or line["end"] > duration:
            failures.append(dict(line=line["line"], reason="weak_or_invalid_original_phonetics", flags=line["flags"]))
        previous = line["end"]
        difference = dict(start=abs(line["start"] - other["start"]), end=abs(line["end"] - other["end"]))
        support = []
        if (difference["start"] <= .25 and difference["end"] <= .35
                and not other['flags'] and valid_word_intervals(other)):
            support.append(dict(method="original_vs_vocals_phonetic_boundary", difference=difference))
        else:
            # Different recognizer, exact whole-line text and strong probability; keep all alternatives in provenance.
            for path, segments in asr:
                for segment in segments:
                    for word in segment["words"]:
                        if normalized(word["word"]) == normalized(line["text"]) and word.get("probability", 0) >= .75:
                            if abs(word["start"] - line["start"]) <= .25 and abs(word["end"] - line["end"]) <= .25:
                                support.append(dict(method="independent_exact_line_ASR", file=path, word=word, alternatePhoneticDifference=difference))
        if not support: failures.append(dict(line=line["line"], reason="no_cross_method_boundary_support", difference=difference))
        validations.append(dict(line=line["line"], support=support))
    validation_path = report_path.with_name(report_key + ".publication-check.json")
    write_json(validation_path, dict(id=args.id, automaticEvidenceOnly=True, humanListeningVerified=False, failures=failures, validations=validations))
    if failures: raise ValueError(f"Refusing publication: {len(failures)} unresolved checks; {validation_path}")
    target = ROOT / "Assets/Resources/Music/Lyrics"
    target.mkdir(exist_ok=True)
    lrc = ["[ti:" + sheet["title"] + "]", "[by:automatic phonetic calibration draft - pending listening]", "[00:00.00]"]
    reference = json.loads((ROOT / f"tools/music/alignment_review/small/{args.id}.authored-prompt.review.json").read_text(encoding="utf-8"))
    if [line["text"] for line in reference["lines"]] != sheet["plainText"].splitlines(): raise ValueError("Stale release-time reference")
    for index, line in enumerate(primary):
        lrc.append(f"[{stamp(line['start'])}]" + line["text"])
        following = primary[index + 1]["start"] if index + 1 < len(primary) else line["end"] + 2
        # CTC can end before a sustained sung vowel. Hold ordinary phrases until the next cue.
        if index + 1 == len(primary) or following - line["end"] >= 5:
            release = max(line["end"], reference["lines"][index]["end"])
            if index + 1 == len(primary) or release < following - 1:
                lrc.append(f"[{stamp(min(release, duration))}]")
    lrc_path = target / f"{args.id}.txt"
    lrc_path.write_text("\n".join(lrc) + "\n", encoding="utf-8")
    evidence_hash = hashlib.sha256(report_path.read_bytes()).hexdigest()
    publisher_hash = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    archive = ROOT / "tools/music/alignment_review/releases"; archive.mkdir(exist_ok=True)
    stem = f"{args.id}-{evidence_hash[:12]}-{publisher_hash[:8]}"
    archived_report, archived_checks = archive / (stem + ".json"), archive / (stem + ".checks.json")
    shutil.copy2(report_path, archived_report); shutil.copy2(validation_path, archived_checks)
    write_json(target / f"{args.id}_info.json", dict(id=args.id, source="原声字幕 · 音素自动校准草稿（待试听确认）",
        status="automatic_phonetic_calibration_draft", humanListeningVerified=False, lineCount=len(primary),
        audioSha256=report["audioSha256"], sourceSha256=report["sourceSha256"], promptRevision=sheet["promptRevision"],
        model=report["model"], modelSha256=report["modelSha256"], lineTimingOnly=True,
        gapRule="ordinary lines held until next cue; blank only for >=5s gaps or outro, release corroborated by original-audio alignment",
        evidenceReport=str(archived_report.relative_to(ROOT)), evidenceSha256=evidence_hash,
        publicationCheck=str(archived_checks.relative_to(ROOT)), publicationCheckSha256=hashlib.sha256(archived_checks.read_bytes()).hexdigest(),
        publisherSha256=publisher_hash, lrcSha256=hashlib.sha256(lrc_path.read_bytes()).hexdigest()))
    print(f"{args.id}: {len(primary)} source lines published as explicitly unlistened line-only calibration draft")


if __name__ == "__main__": main()
