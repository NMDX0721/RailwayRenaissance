"""Independent overlapping-window ASR. Never rewrites source sheets or publishes cues."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import re

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/MusicAlignment/python"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("id", choices=["south_wind", "starlit_rails", "chollima_ride", "sleepers"])
    parser.add_argument("--window", action="append", required=True, help="start:end in seconds; repeat for overlapping windows")
    parser.add_argument("--model", default="small")
    parser.add_argument("--device", choices=["cpu", "cuda"], default="cpu")
    parser.add_argument("--tag", default="", help="Keep separate evidence rounds")
    parser.add_argument("--role", choices=["original", "vocals"], default="original")
    parser.add_argument("--vocals-variant", choices=["", "normalized"], default="")
    parser.add_argument("--language", choices=["expected", "auto", "ko", "en"], default="expected")
    args = parser.parse_args()
    if args.tag and not re.fullmatch(r"[A-Za-z0-9_-]+", args.tag):
        raise ValueError("Invalid evidence tag")
    if args.language != "expected" and not args.tag:
        raise ValueError("Language diagnostics require a separate evidence tag")
    if args.vocals_variant and args.role != "vocals":
        raise ValueError("Vocal variants require --role vocals")
    import numpy as np
    from faster_whisper import WhisperModel
    model_path = ROOT / "Library/MusicAlignment/models" / args.model
    compute_type = "float16" if args.device == "cuda" else "int8"
    model = WhisperModel(str(model_path), device=args.device, compute_type=compute_type, cpu_threads=8, local_files_only=True)
    source = ROOT / f"Assets/Resources/bgm/{args.id}.mp3"
    waveform = source
    if args.role == "vocals":
        if not args.tag: raise ValueError("Derived audio requires a separate evidence tag")
        vocal_key = args.id + ("." + args.vocals_variant if args.vocals_variant else "")
        waveform = ROOT / f"Library/MusicAlignment/vocals/{vocal_key}.wav"
        provenance = json.loads(waveform.with_suffix(".json").read_text(encoding="utf-8"))
        if (provenance["originalAudioSha256"] != hashlib.sha256(source.read_bytes()).hexdigest()
                or provenance["timeOffset"] != 0
                or provenance["derivedAudioSha256"] != hashlib.sha256(waveform.read_bytes()).hexdigest()):
            raise ValueError("Derived waveform provenance does not match original audio")
    pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", str(waveform), "-f", "f32le", "-ac", "1", "-ar", "16000", "-"], check=True, capture_output=True).stdout
    audio = np.frombuffer(pcm, dtype=np.float32).copy()
    windows = []
    for spec in args.window:
        start, end = map(float, spec.split(":"))
        if not 0 <= start < end <= len(audio) / 16000:
            raise ValueError("Invalid audio window: " + spec)
        language = ("en" if args.id == "sleepers" else "ko") if args.language == "expected" else args.language
        segments, transcription_info = model.transcribe(audio[round(start * 16000):round(end * 16000)],
                                      language=None if language == "auto" else language,
                                      beam_size=5, word_timestamps=True, condition_on_previous_text=False,
                                      vad_filter=False)
        result = [dict(start=start + s.start, end=start + s.end, text=s.text,
                       words=[dict(start=start + w.start, end=start + w.end, word=w.word, probability=w.probability) for w in s.words or []]) for s in segments]
        windows.append(dict(start=start, end=end, segments=result, detectedLanguage=transcription_info.language,
                            languageProbability=transcription_info.language_probability))
        print(f"{args.id}: {spec} done", flush=True)
    key = args.id + ("." + args.tag if args.tag else "")
    if args.role != "original": key += "." + args.role
    if args.vocals_variant: key += "." + args.vocals_variant
    target = ROOT / f"tools/music/alignment_review/{args.model}/{key}.window-check.json"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(dict(id=args.id, audioSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                                     audioRole=args.role, waveformSha256=hashlib.sha256(waveform.read_bytes()).hexdigest(), timeOffset=0,
                                     waveformVariant=args.vocals_variant,
                                     requestedLanguage=args.language,
                                     device=args.device, computeType=compute_type,
                                     modelProvenance=json.loads((model_path / "provenance.json").read_text()),
                                     humanListeningVerified=False, status="independent_window_evidence", windows=windows), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
