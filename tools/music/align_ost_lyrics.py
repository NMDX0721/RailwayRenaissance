"""Local-only OST forced alignment and independent ASR checks. Outputs review candidates.

Install stable-ts in Library/MusicAlignment/python and download a model there first.
No generated file is promoted to Unity Resources by this command.
"""
import argparse
import difflib
import hashlib
import importlib.metadata
import json
from pathlib import Path
import re
import subprocess
import sys
import unicodedata

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/MusicAlignment/python"))


def normalized(text):
    return re.sub(r"[^\w]", "", text.casefold()).replace("twentythree", "23")


def stamp(seconds):
    cs = round(seconds * 100)
    return f"{cs // 6000:02d}:{cs // 100 % 60:02d}.{cs % 100:02d}"


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def asr_anchors(text, asr, duration, phonetic=False):
    units = (lambda value: unicodedata.normalize("NFKD", normalized(value))) if phonetic else normalized
    source = units(text)
    heard, positions = "", []
    for segment in asr:
        for word in segment["words"]:
            chars = units(word["word"])
            heard += chars
            positions.extend([(word["start"], word["end"])] * len(chars))
    mapping = {}
    for block in difflib.SequenceMatcher(None, source, heard, autojunk=False).get_matching_blocks():
        for offset in range(block.size):
            mapping[block.a + offset] = positions[block.b + offset]
    anchors, cursor = [], 0
    for line in text.splitlines():
        chars = units(line)
        matched = [mapping[i] for i in range(cursor, cursor + len(chars)) if i in mapping]
        cursor += len(chars)
        if not matched or len(matched) / max(1, len(chars)) < .5:
            raise ValueError("Cannot safely anchor this line to independent ASR: " + line)
        anchors.append(dict(start=max(0, matched[0][0] - .8), end=min(duration, matched[-1][1] + .8), text=line))
    for previous, following in zip(anchors, anchors[1:]):
        if previous["end"] > following["start"]:
            boundary = (previous["end"] + following["start"]) / 2
            previous["end"] = following["start"] = boundary
    return anchors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("ids", nargs="+", choices=["sleepers", "south_wind", "starlit_rails", "chollima_ride"])
    parser.add_argument("--model", default="base")
    parser.add_argument("--device", default="cpu")
    parser.add_argument("--threads", type=int, default=8)
    parser.add_argument("--transcript", type=Path, help="Performance transcript for a single song; listening review remains separate")
    parser.add_argument("--anchored", action="store_true", help="Constrain each line to independent-ASR evidence")
    parser.add_argument("--force-align", action="store_true", help="Recompute alignment while reusing independent ASR")
    parser.add_argument("--asr-only", action="store_true", help="Collect independent audio evidence without forcing source lyrics into the recording")
    parser.add_argument("--variant", default="", help="Separate candidate name to preserve prior source versions")
    parser.add_argument("--phonetic-anchors", action="store_true", help="Compare Korean jamo units for ASR anchors; never modifies source words")
    parser.add_argument("--audio", type=Path, help="Derived local review WAV, with provenance JSON beside it")
    args = parser.parse_args()
    if args.variant and not re.fullmatch(r"[A-Za-z0-9_-]+", args.variant):
        raise ValueError("Invalid candidate variant")
    import numpy as np
    import stable_whisper
    import torch

    torch.set_num_threads(args.threads)
    model_path = ROOT / "Library/MusicAlignment/models" / args.model
    model = stable_whisper.load_faster_whisper(str(model_path), device=args.device,
                                              compute_type="int8" if args.device == "cpu" else "float16",
                                              cpu_threads=args.threads, local_files_only=True)
    out = ROOT / "tools/music/alignment_review" / args.model
    out.mkdir(parents=True, exist_ok=True)
    for track_id in args.ids:
        sheet_file = ROOT / f"Assets/Resources/Music/LyricSheets/{track_id}.json"
        sheet = json.loads(sheet_file.read_text(encoding="utf-8"))
        original_lines = sheet["plainText"].splitlines()
        source_changes = []
        if args.transcript:
            if len(args.ids) != 1:
                raise ValueError("A transcript override requires exactly one song")
            if "withdrawn" in args.transcript.resolve().parts:
                raise ValueError("Withdrawn transcript cannot be used as alignment input")
            sheet["plainText"] = args.transcript.read_text(encoding="utf-8").strip()
            revised_lines = sheet["plainText"].splitlines()
            for kind, a, b, c, d in difflib.SequenceMatcher(None, original_lines, revised_lines, autojunk=False).get_opcodes():
                if kind != "equal":
                    source_changes.append(dict(kind=kind, originalLineStart=a + 1,
                                               original=original_lines[a:b], replacement=revised_lines[c:d]))
            if source_changes:
                print("TRANSCRIPT_OVERRIDE_DIFF: " + json.dumps(source_changes, ensure_ascii=True), flush=True)
        source = ROOT / f"Assets/Resources/bgm/{track_id}.mp3"
        language = "en" if track_id == "sleepers" else "ko"
        audio_hash = hashlib.sha256(source.read_bytes()).hexdigest()
        decode_source = source
        derived_provenance = None
        if args.audio:
            if not args.variant or len(args.ids) != 1:
                raise ValueError("Derived audio requires a separate variant and one track")
            derived_provenance = json.loads(args.audio.with_suffix(".json").read_text(encoding="utf-8"))
            if derived_provenance["originalAudioSha256"] != audio_hash or derived_provenance["timeOffset"] != 0:
                raise ValueError("Derived audio is not aligned to this original recording")
            if derived_provenance["derivedAudioSha256"] != hashlib.sha256(args.audio.read_bytes()).hexdigest():
                raise ValueError("Derived audio hash mismatch")
            decode_source = args.audio
        transcript_hash = hashlib.sha256(sheet["plainText"].encode("utf-8")).hexdigest()
        provenance = json.loads((model_path / "provenance.json").read_text(encoding="utf-8"))
        cache_identity = dict(audioSha256=audio_hash, modelProvenance=provenance, language=language)
        if derived_provenance:
            cache_identity["derivedAudioSha256"] = derived_provenance["derivedAudioSha256"]
        key = track_id + ("." + args.transcript.stem if args.transcript else "")
        if args.variant:
            key += "." + args.variant
        if args.anchored:
            key += ".anchored"
        if args.phonetic_anchors:
            if not args.anchored:
                raise ValueError("Phonetic anchors require --anchored")
            key += ".jamo"
        # Decode with ffmpeg, avoiding torchaudio codec/runtime dependencies.
        pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", str(decode_source), "-f", "f32le", "-ac", "1", "-ar", "16000", "-"],
                             check=True, capture_output=True).stdout
        audio = np.frombuffer(pcm, dtype=np.float32).copy()
        duration = len(audio) / 16000
        asr_key = track_id + ("." + args.variant if args.audio else "")
        transcription = out / f"{asr_key}.asr.json"
        asr_meta = out / f"{asr_key}.asr.meta.json"
        if transcription.exists() and asr_meta.exists() and json.loads(asr_meta.read_text(encoding="utf-8")) == cache_identity:
            asr = json.loads(transcription.read_text(encoding="utf-8"))
        else:
            print(f"Transcribing {track_id} ({duration:.1f}s)...", flush=True)
            segments, _ = model.transcribe_original(audio, language=language, beam_size=5,
                                                   word_timestamps=True, condition_on_previous_text=False)
            asr = [dict(start=s.start, end=s.end, text=s.text,
                        words=[dict(start=w.start, end=w.end, word=w.word, probability=w.probability) for w in s.words or []])
                   for s in segments]
            write_json(transcription, asr)
            write_json(asr_meta, cache_identity)
        if args.asr_only:
            print(f"{track_id}: independent ASR saved; no forced alignment or resource changes", flush=True)
            continue
        print(f"Aligning {track_id} to original lyric lines...", flush=True)
        alignment = out / f"{key}.alignment.json"
        alignment_meta = out / f"{key}.alignment.meta.json"
        alignment_identity = dict(**cache_identity, transcriptSha256=transcript_hash, anchored=args.anchored, phonetic=args.phonetic_anchors, pipelineVersion=2)
        if alignment.exists() and alignment_meta.exists() and json.loads(alignment_meta.read_text(encoding="utf-8")) == alignment_identity and not args.force_align:
            result = stable_whisper.WhisperResult(str(alignment))
        else:
            if args.anchored:
                anchors = asr_anchors(sheet["plainText"], asr, duration, args.phonetic_anchors)
                write_json(out / f"{key}.anchors.json", anchors)
                result = model.align_words(audio, anchors, language=language, normalize_text=False,
                                           suppress_silence=False, regroup=False, verbose=False)
            else:
                result = model.align(audio, sheet["plainText"], language=language, original_split=True,
                                     regroup=False, suppress_silence=False, nonspeech_skip=None, verbose=False)
            if result is None:
                raise RuntimeError("Aligner returned no result")
            result.save_as_json(str(alignment))
            write_json(alignment_meta, alignment_identity)
        asr_words = [word for segment in asr for word in segment["words"]]
        issues, lines = [], []
        previous = 0
        for index, segment in enumerate(result.segments):
            text = segment.text.strip()
            words = segment.words or []
            nearby = " ".join(w["word"] for w in asr_words if w["end"] >= segment.start - .5 and w["start"] <= segment.end + .5)
            source_chars = normalized(text)
            matcher = difflib.SequenceMatcher(None, source_chars, normalized(nearby), autojunk=False)
            # Nearby context may contain neighbouring words; measure source recall, not context precision.
            evidence = sum(block.size for block in matcher.get_matching_blocks()) / max(1, len(source_chars))
            flags = []
            if segment.start < previous - .01 or segment.end > duration + .1 or segment.end <= segment.start:
                flags.append("invalid_interval")
            if segment.end - segment.start > 15:
                flags.append("long_line")
            if not words or any(w.end <= w.start for w in words):
                flags.append("zero_duration_word")
            if evidence < .65:
                flags.append("low_asr_agreement")
            if flags:
                issues.append(dict(line=index + 1, flags=flags, start=segment.start, end=segment.end, text=text, heard=nearby))
            lines.append(dict(line=index + 1, start=segment.start, end=segment.end, text=text,
                              asrAgreement=round(evidence, 3), words=[dict(start=w.start, end=w.end, text=w.word) for w in words]))
            previous = segment.end
        expected = normalized(sheet["plainText"])
        actual = normalized("".join(line["text"] for line in lines))
        if actual != expected:
            issues.append(dict(flags=["source_text_mismatch"]))
        report = dict(id=track_id, language=language, audioSha256=audio_hash,
                      lyricSha256=hashlib.sha256(sheet_file.read_bytes()).hexdigest(), model=args.model,
                      transcriptOverride=str(args.transcript) if args.transcript else None,
                      transcriptSha256=transcript_hash,
                      originalLineCount=len(original_lines), sourceChanges=source_changes,
                      sourceRevision=sheet.get("promptRevision"),
                      derivedAudioProvenance=derived_provenance,
                      modelProvenance=provenance,
                      toolVersions={name: importlib.metadata.version(name) for name in ["stable-ts", "faster-whisper"]},
                      duration=duration, lineCount=len(lines), expectedLineCount=len(sheet["plainText"].splitlines()),
                      status="candidate_needs_review", humanListeningVerified=False, issues=issues, lines=lines)
        report["agreementMetric"] = "source_character_recall"
        write_json(out / f"{key}.review.json", report)
        lrc = [f"[ti:{sheet['title']}]", "[by:local forced alignment candidate - not listening verified]"]
        for index, line in enumerate(lines):
            # Explicit empty boundaries prevent carrying vocals across instrumental gaps.
            lrc.append(f"[{stamp(line['start'])}]" + line["text"])
            next_start = lines[index + 1]["start"] if index + 1 < len(lines) else duration
            if next_start - line["end"] >= 1.5:
                lrc.append(f"[{stamp(line['end'])}]")
        (out / f"{key}.candidate.lrc").write_text("\n".join(lrc) + "\n", encoding="utf-8")
        print(f"{track_id}: {len(lines)} lines; {len(issues)} flagged items. Review: {out / (key + '.review.json')}", flush=True)


if __name__ == "__main__":
    main()
