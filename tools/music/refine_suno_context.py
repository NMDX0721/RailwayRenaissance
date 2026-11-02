"""Re-align weak phonetics only inside independently corroborated exported line windows.

Keeps source lyrics intact, rejects malformed/shared/long provider intervals, never publishes.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys
from align_ost_lyrics import ROOT, write_json
sys.path.insert(0, str(ROOT / "Library/MusicAlignment/python"))
import torch
import torchaudio


def digest(path):
    with path.open("rb") as stream: return hashlib.file_digest(stream, "sha256").hexdigest()


def quality(words):
    mass = sum(max(.001, w["end"] - w["start"]) for w in words)
    return (sum(w["maxInternalGap"] > .8 for w in words),
            sum(w["score"] < .05 for w in words if w["kind"] == "lyric"),
            -sum(w["score"] * max(.001, w["end"] - w["start"]) for w in words) / mass)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("id", choices=["starlit_rails", "chollima_ride"])
    parser.add_argument("--include-boundary-conflicts", action="store_true", help="Also recheck lines whose two acoustic sources disagree; separate output")
    parser.add_argument("--provider", choices=["suno", "lyricsmith", "lyrictimestamps"], default="suno")
    args = parser.parse_args()
    torch.set_num_threads(8)
    variant = "local.sections" if args.id == "starlit_rails" else "spoken.local.sections.allsections"
    folder = ROOT / "tools/music/alignment_review"
    source = folder / f"phonetic/{args.id}.{variant}.json"
    if args.provider != "suno":
        if (args.provider == "lyricsmith" and args.id != "starlit_rails"
                or args.provider == "lyrictimestamps" and args.id != "chollima_ride"):
            raise ValueError("No audited native export for this track")
        variant += ".provider.boundaries"
        source = folder / f"phonetic/{args.id}.{variant}.json"
    report = json.loads(source.read_text(encoding="utf-8"))
    audit_path = folder / f"suno_exports/{args.id}.audit.json"
    if args.provider == "lyricsmith": audit_path = folder / f"lyricsmith/{args.id}.native-audit.json"
    if args.provider == "lyrictimestamps": audit_path = folder / f"lyrictimestamps/{args.id}.native-audit.json"
    audit = json.loads(audit_path.read_text(encoding="utf-8"))
    if args.provider == "lyrictimestamps":
        if digest(ROOT / audit["exportPath"]) != audit["exportSha256"]:
            raise ValueError("Native JSON export changed")
        audit.update(comparisons=[dict(line=line["line"], exported=dict(
            text=line["text"], start=line["start"], end=line["end"])) for line in audit["lines"]],
            repeatedStarts=[])
    if args.provider == "lyricsmith":
        from audit_lyricsmith_exports import parse as parse_native
        native_srt = []
        for entry in audit["nativeFiles"]:
            if digest(ROOT / entry["file"]) != entry["sha256"]: raise ValueError("Native export changed")
            if entry["file"].endswith(".srt"):
                native_srt.extend(parse_native(ROOT / entry["file"]))
        comparisons = []
        for line in audit["lines"]:
            # Only original downloaded word data plus SRT, never reconstructed CSS-only rows.
            if not line["nativeWordFiles"] or not any(name.endswith(".srt") for name in line["nativeFiles"]): continue
            if any(flag in line["flags"] for flag in ["invalid_word_interval", "word_order_or_overlap"]): continue
            matching = [cue for cue in native_srt if cue["text"] == line["text"]
                        and abs(cue["start"] - line["start"]) <= .011 and abs(cue["end"] - line["end"]) <= .011]
            if not matching: raise ValueError("Audited native cue cannot be recovered")
            comparisons.append(dict(line=line["line"], exported=matching[0]))
        if (audit["audioSha256"] != digest(ROOT / f"Assets/Resources/bgm/{args.id}.mp3")
                or audit["sourceSha256"] != digest(ROOT / f"Assets/Resources/Music/LyricSheets/{args.id}.json")):
            raise ValueError("Stale native source")
        audit.update(comparisons=comparisons, repeatedStarts=[])
    if (report["audioSha256"] != audit["audioSha256"] or report["sourceSha256"] != audit["sourceSha256"]
            or audit["audioSha256"] != digest(ROOT / f"Assets/Resources/bgm/{args.id}.mp3")
            or audit["sourceSha256"] != digest(ROOT / f"Assets/Resources/Music/LyricSheets/{args.id}.json")):
        raise ValueError("Stale or inconsistent exported evidence")
    if args.provider == "suno" and (audit["srtSha256"] != digest(folder / f"suno_exports/{args.id}.srt")
                                   or not audit["lrcSrtAgreement"]):
        raise ValueError("Stale or inconsistent Suno exports")
    original = copy.deepcopy(report)
    aligner, tokenizer = torchaudio.pipelines.MMS_FA.get_aligner(), torchaudio.pipelines.MMS_FA.get_tokenizer()
    shared = {round(x["start"], 3) for x in audit["repeatedStarts"]}
    accepted, rejected = [], []
    for run in report["rounds"]:
        cache = ROOT / f"Library/MusicAlignment/mms/{args.id}.{run['role']}.pt"
        metadata = json.loads(cache.with_suffix(".json").read_text())
        if metadata["modelSha256"] != report["modelSha256"]: raise ValueError("Wrong acoustic model cache")
        audio_path = ROOT / (f"Assets/Resources/bgm/{args.id}.mp3" if run["role"] == "original" else f"Library/MusicAlignment/vocals/{args.id}.wav")
        if metadata["audioSha256"] != digest(audio_path): raise ValueError("Wrong waveform cache")
        values = torch.load(cache, weights_only=True)
        emission, times = values["emission"], values["times"]
        for comparison in audit["comparisons"]:
            index = comparison["line"] - 1
            line = run["lines"][index]
            cue = comparison["exported"]
            acoustic_lines = [other["lines"][index] for other in original["rounds"]]
            conflict = (max(x["start"] for x in acoustic_lines) - min(x["start"] for x in acoustic_lines) > .25
                        or max(x["end"] for x in acoustic_lines) - min(x["end"] for x in acoustic_lines) > .35)
            if not line["flags"] and not (args.include_boundary_conflicts and conflict): continue
            if cue is None or cue["text"] != line["text"]: continue
            if (round(cue["start"], 3) in shared or not .15 <= cue["end"] - cue["start"] <= 15
                    or not any(abs(cue["start"] - other["lines"][index]["start"]) <= .5 for other in original["rounds"])):
                rejected.append(dict(role=run["role"], line=index + 1, reason="export_window_not_corroborated"))
                continue
            if not any(w["kind"] == "lyric" for w in line["words"]): continue
            lower = max(0, cue["start"] - .35, run["lines"][index - 1]["end"] if index else 0)
            upper = min(audit["duration"], cue["end"] + .15,
                        run["lines"][index + 1]["start"] if index + 1 < len(run["lines"]) else audit["duration"])
            lo, hi = int(torch.searchsorted(times, lower)), int(torch.searchsorted(times, upper))
            if hi <= lo: continue
            spans = aligner(emission[lo:hi], tokenizer([w["romanized"] for w in line["words"]]))
            words = []
            for previous, chars in zip(line["words"], spans):
                chars = [type(c)(c.token, c.start + lo, c.end + lo, c.score) for c in chars]
                word = copy.deepcopy(previous)
                count = sum(c.end - c.start for c in chars)
                word.update(start=max(lower, float(times[chars[0].start]) - .01),
                            end=min(upper, float(times[chars[-1].end - 1]) + .01),
                            score=sum(c.score * (c.end - c.start) for c in chars) / max(1, count),
                            maxInternalGap=max([float(times[b.start] - times[a.end - 1]) for a, b in zip(chars, chars[1:])] or [0]),
                            providerConstraint=dict(auditSha256=digest(audit_path), window=[lower, upper]))
                words.append(word)
            before, after = quality(line["words"]), quality(words)
            if (after >= before or -after[2] < max(.15, -.8 * before[2])
                    or words[-1]["end"] <= words[0]["start"]):
                rejected.append(dict(role=run["role"], line=index + 1, reason="quality_or_confidence_guard_rejected",
                                     qualityImproved=after < before, scoreAccepted=-after[2] >= max(.15, -.8 * before[2]),
                                     before=before, candidate=after))
                continue
            flags = []
            if after[0]: flags.append("fragmented_word_alignment")
            if after[1]: flags.append("weak_word_evidence")
            score = -after[2]
            if score < .15: flags.append("low_phonetic_score")
            if any(w["end"] <= w["start"] for w in words): flags.append("invalid_interval")
            if flags:
                rejected.append(dict(role=run["role"], line=index + 1, reason="candidate_still_flagged", flags=flags))
                continue
            accepted.append(dict(role=run["role"], line=index + 1, before=before, selected=after, window=[lower, upper]))
            line.update(start=words[0]["start"], end=words[-1]["end"], words=words, flags=flags, phoneticScore=score)
    report.update(providerAuditSha256=digest(audit_path), parentReportSha256=digest(source),
                  refinementCodeSha256=digest(Path(__file__)), providerRefinements=accepted,
                  rejectedProviderRefinements=rejected, humanListeningVerified=False)
    key = (f"{args.id}.{variant}.{args.provider}" if args.provider != "suno" else
           f"{args.id}.{variant}.provider" + (".boundaries" if args.include_boundary_conflicts else ""))
    destination = folder / f"phonetic/{key}.json"
    write_json(destination, report)
    for run in report["rounds"]:
        assert [x["text"] for x in run["lines"]] == [x["text"] for x in original["rounds"][0]["lines"]]
        print(args.id, run["role"], "flagged", [(x["line"], x["flags"]) for x in run["lines"] if x["flags"]])
    print("Provider refinements accepted:", len(accepted), "; rejected:", len(rejected))


if __name__ == "__main__": main()
