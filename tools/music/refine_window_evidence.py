"""Refine only unique exact-text matches in independent window ASR; no publication."""
import copy
from collections import Counter
import json
from pathlib import Path
import sys
from align_ost_lyrics import ROOT, normalized, stamp, write_json


def main(track_id):
    if track_id not in ("south_wind", "starlit_rails", "chollima_ride"):
        raise ValueError("Unknown OST")
    folder = ROOT / "tools/music/alignment_review/small"
    original = json.loads((folder / f"{track_id}.review.json").read_text(encoding="utf-8"))
    windows = json.loads((folder / f"{track_id}.window-check.json").read_text(encoding="utf-8"))
    if original["audioSha256"] != windows["audioSha256"]:
        raise ValueError("Audio versions differ")
    report = copy.deepcopy(original)
    occurrences = Counter(normalized(line["text"]) for line in report["lines"])
    changes = []
    proposals = {}
    for line in report["lines"]:
        source = normalized(line["text"])
        if not source or occurrences[source] != 1:
            continue
        matches = []
        for window in windows["windows"]:
            words = [word for segment in window["segments"] for word in segment["words"]]
            text, positions = "", []
            for index, word in enumerate(words):
                chars = normalized(word["word"])
                text += chars; positions.extend([index] * len(chars))
            cursor = 0
            while (offset := text.find(source, cursor)) >= 0:
                first, last = positions[offset], positions[offset + len(source) - 1]
                # A substring inside another word has no independent boundary.
                if (offset == 0 or positions[offset - 1] != first) and (offset + len(source) == len(positions) or positions[offset + len(source)] != last):
                    found = words[first:last + 1]
                    start, end = found[0]["start"], found[-1]["end"]
                    if end > start and all(w["end"] > w["start"] for w in found):
                        matches.append((start, end, found))
                cursor = offset + 1
        if not matches or max(m[0] for m in matches) - min(m[0] for m in matches) > .6 or max(m[1] for m in matches) - min(m[1] for m in matches) > .6:
            continue
        start, end, words = matches[0]
        proposals[line["line"]] = (start, end, words)
    # Do not trade an ASR match for broken sentence ordering.
    rejected = []
    while True:
        invalid = []
        for index, line in enumerate(report["lines"]):
            number = line["line"]
            if number not in proposals:
                continue
            start, end, _ = proposals[number]
            previous = report["lines"][index - 1] if index > 0 else None
            following = report["lines"][index + 1] if index + 1 < len(report["lines"]) else None
            previous_end = proposals.get(previous["line"], (previous["start"], previous["end"]))[1] if previous else 0
            next_start = proposals.get(following["line"], (following["start"], following["end"]))[0] if following else report["duration"]
            if start < previous_end - .01 or end > next_start + .01:
                invalid.append(number)
        if not invalid:
            break
        for number in invalid:
            rejected.append(dict(line=number, proposed=list(proposals[number][:2]), reason="conflicts_with_neighbouring_cue"))
            del proposals[number]
    for line in report["lines"]:
        if line["line"] not in proposals:
            continue
        start, end, words = proposals[line["line"]]
        changes.append(dict(line=line["line"], before=[line["start"], line["end"]], after=[start, end], evidence="unique exact-text window match"))
        line.update(start=start, end=end, asrAgreement=1, words=[dict(start=w["start"], end=w["end"], text=w["word"]) for w in words])
    changed = {item["line"] for item in changes}
    report["issues"] = [issue for issue in report["issues"] if issue.get("line") not in changed]
    previous = 0
    for line in report["lines"]:
        if line["start"] < previous - .01 or line["end"] <= line["start"] or line["end"] > report["duration"]:
            report["issues"].append(dict(line=line["line"], flags=["invalid_interval_after_refinement"], start=line["start"], end=line["end"], text=line["text"]))
        previous = line["end"]
    report.update(status="window_refined_candidate_needs_listening", humanListeningVerified=False, windowRefinements=changes, rejectedWindowProposals=rejected)
    write_json(folder / f"{track_id}.window-refined.review.json", report)
    lrc = ["[by:independent window candidate - not listening verified]"]
    for index, line in enumerate(report["lines"]):
        lrc.append(f"[{stamp(line['start'])}]" + line["text"])
        next_start = report["lines"][index + 1]["start"] if index + 1 < len(report["lines"]) else report["duration"]
        if next_start - line["end"] >= 1.5:
            lrc.append(f"[{stamp(line['end'])}]")
    (folder / f"{track_id}.window-refined.candidate.lrc").write_text("\n".join(lrc) + "\n", encoding="utf-8")
    print(f"{track_id}: {len(changes)} unique exact-text windows; {len(report['issues'])} remaining issues; all {len(report['lines'])} source lines preserved")


if __name__ == "__main__":
    for track in sys.argv[1:]:
        main(track)
