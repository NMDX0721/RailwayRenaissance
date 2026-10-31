"""Independent local forced-alignment consensus. Never claims listening or publishes assets."""
import argparse
import hashlib
import json
from pathlib import Path
import statistics
import subprocess
import sys
from align_ost_lyrics import ROOT, normalized, write_json, stamp


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("id", choices=["south_wind", "starlit_rails", "chollima_ride", "sleepers"])
    parser.add_argument("--group-size", type=int, default=4)
    parser.add_argument("--context", type=int, default=2)
    parser.add_argument("--models", nargs="+", default=["base", "small"])
    args = parser.parse_args()
    if not 1 <= args.group_size <= 12 or not 0 <= args.context <= 4:
        raise ValueError("Invalid phrase group size")
    import numpy as np
    import stable_whisper
    import torch
    torch.set_num_threads(8)
    source = ROOT / f"Assets/Resources/Music/LyricSheets/{args.id}.json"
    sheet = json.loads(source.read_text(encoding="utf-8"))
    original = sheet["plainText"].splitlines()
    folder = ROOT / "tools/music/alignment_review"
    baseline_path = folder / "small" / f"{args.id}.authored-prompt.review.json"
    baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
    if [line["text"] for line in baseline["lines"]] != original:
        raise ValueError("Baseline is not the current authored transcript")
    audio_path = ROOT / f"Assets/Resources/bgm/{args.id}.mp3"
    audio_hash = hashlib.sha256(audio_path.read_bytes()).hexdigest()
    if baseline["audioSha256"] != audio_hash:
        raise ValueError("Baseline is for different audio")
    pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", str(audio_path), "-f", "f32le", "-ac", "1", "-ar", "16000", "-"], check=True, capture_output=True).stdout
    audio = np.frombuffer(pcm, dtype=np.float32).copy(); duration = len(audio) / 16000
    rounds = []
    for name in args.models:
        model_path = ROOT / "Library/MusicAlignment/models" / name
        model = stable_whisper.load_faster_whisper(str(model_path), device="cpu", compute_type="int8", cpu_threads=8, local_files_only=True)
        lines = []
        for first in range(0, len(original), args.group_size):
            group = baseline["lines"][first:first + args.group_size]
            low, high = max(0, first - args.context), min(len(original), first + len(group) + args.context)
            context = baseline["lines"][low:high]
            start = max(0, context[0]["start"] - 3)
            end = min(duration, max(line["end"] for line in context) + 3)
            text = "\n".join(original[low:high])
            result = model.align(audio[round(start * 16000):round(end * 16000)], text,
                                 language="en" if args.id == "sleepers" else "ko", original_split=True,
                                 regroup=False, suppress_silence=False, nonspeech_skip=None, verbose=False)
            if result is None or len(result.segments) != len(context):
                raise ValueError(f"Local aligner changed line count: {name}/{first + 1}")
            for index, segment in enumerate(result.segments[first - low:first - low + len(group)]):
                if normalized(segment.text) != normalized(original[first + index]):
                    raise ValueError("Local aligner changed source text")
                lines.append(dict(line=first + index + 1, text=original[first + index],
                                  start=start + segment.start, end=start + segment.end,
                                  window=[start, end], words=[dict(text=w.word, start=start + w.start, end=start + w.end) for w in segment.words or []]))
            print(f"{args.id}/{name}: source lines {first + 1}-{first + len(group)}", flush=True)
        rounds.append(dict(model=name, provenance=json.loads((model_path / "provenance.json").read_text()), lines=lines))
        del model
    consensus, previous = [], 0
    for index, text in enumerate(original):
        values = [run["lines"][index] for run in rounds]
        starts, ends = [v["start"] for v in values], [v["end"] for v in values]
        start, end = statistics.median(starts), statistics.median(ends)
        flags = []
        if len(values) < 2 or max(starts) - min(starts) > .3 or max(ends) - min(ends) > .4:
            flags.append("model_boundary_disagreement")
        if start < previous - .01 or end <= start or end > duration:
            flags.append("invalid_interval")
        if any(v["end"] <= v["start"] for v in values):
            flags.append("collapsed_model_line")
        consensus.append(dict(line=index + 1, text=text, start=start, end=end,
                              startSpread=max(starts) - min(starts), endSpread=max(ends) - min(ends), flags=flags))
        previous = end
    target = folder / "local_consensus"; target.mkdir(exist_ok=True)
    report = dict(id=args.id, audioSha256=audio_hash, sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                  sourceRevision=sheet.get("promptRevision"), duration=duration, groupSize=args.group_size,
                  contextLines=args.context, status="automatic_local_consensus_needs_listening", humanListeningVerified=False,
                  rounds=rounds, lines=consensus)
    key = f"{args.id}.g{args.group_size}.c{args.context}"
    write_json(target / f"{key}.json", report)
    lrc = ["[by:local two-model alignment candidate - not listening verified]"]
    for index, line in enumerate(consensus):
        lrc.append(f"[{stamp(line['start'])}]" + line["text"])
        following = consensus[index + 1]["start"] if index + 1 < len(consensus) else duration
        if following - line["end"] >= 1.5:
            lrc.append(f"[{stamp(line['end'])}]")
    (target / f"{key}.candidate.lrc").write_text("\n".join(lrc) + "\n", encoding="utf-8")
    print(f"{args.id}: {sum(bool(l['flags']) for l in consensus)}/{len(consensus)} disputed line boundaries", flush=True)


if __name__ == "__main__":
    main()
