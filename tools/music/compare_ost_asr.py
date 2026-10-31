"""Compare source sheets with independent ASR; intervals are evidence windows, not lyrics."""
import argparse
import difflib
import hashlib
import json
from align_ost_lyrics import ROOT, normalized, write_json


def compare(track_id, model):
    sheet_path = ROOT / f"Assets/Resources/Music/LyricSheets/{track_id}.json"
    source = json.loads(sheet_path.read_text(encoding="utf-8"))
    directory = ROOT / "tools/music/alignment_review" / model
    asr = json.loads((directory / f"{track_id}.asr.json").read_text(encoding="utf-8"))
    words = [word for segment in asr for word in segment["words"]]
    heard, positions = "", []
    for index, word in enumerate(words):
        chars = normalized(word["word"])
        heard += chars
        positions.extend([index] * len(chars))
    text = normalized(source["plainText"])
    mapping = {}
    for block in difflib.SequenceMatcher(None, text, heard, autojunk=False).get_matching_blocks():
        for offset in range(block.size):
            mapping[block.a + offset] = positions[block.b + offset]
    lines, cursor = [], 0
    for number, line in enumerate(source["plainText"].splitlines(), 1):
        chars = normalized(line)
        matched = [mapping[i] for i in range(cursor, cursor + len(chars)) if i in mapping]
        cursor += len(chars)
        recall = len(matched) / max(1, len(chars))
        evidence_words = words[matched[0]:matched[-1] + 1] if matched else []
        flags = []
        if recall < .8:
            flags.append("low_text_recall")
        if not evidence_words or evidence_words[-1]["end"] - evidence_words[0]["start"] < .15:
            flags.append("missing_or_compressed_timing")
        if any(word["end"] <= word["start"] for word in evidence_words):
            flags.append("zero_duration_asr_word")
        lines.append(dict(line=number, source=line, sourceRecall=round(recall, 3),
                          needsReview=bool(flags), flags=flags,
                          evidenceStart=words[matched[0]]["start"] if matched else None,
                          evidenceEnd=words[matched[-1]]["end"] if matched else None,
                          recognized="".join(w["word"] for w in words[matched[0]:matched[-1] + 1]) if matched else ""))
    output = directory / f"{track_id}.evidence.json"
    write_json(output, dict(id=track_id, model=model,
                           sourceSha256=hashlib.sha256(sheet_path.read_bytes()).hexdigest(),
                           asrSha256=hashlib.sha256((directory / f"{track_id}.asr.json").read_bytes()).hexdigest(),
                           purpose="automatic_evidence_not_timed_lyrics",
                           humanListeningVerified=False, lines=lines))
    flagged = sum(line["needsReview"] for line in lines)
    print(f"{track_id}/{model}: {flagged}/{len(lines)} source lines need review; {output}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("ids", nargs="+")
    parser.add_argument("--model", default="base")
    args = parser.parse_args()
    for track_id in args.ids:
        if track_id not in ("south_wind", "starlit_rails", "chollima_ride", "sleepers"):
            raise ValueError("Unknown OST ID")
        compare(track_id, args.model)


if __name__ == "__main__":
    main()
