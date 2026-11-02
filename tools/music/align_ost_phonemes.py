"""Independent MMS-FA phonetic alignment; source text remains intact, no listening claim."""
import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import re
import subprocess
import sys
from collections import Counter
from align_ost_lyrics import ROOT, write_json, stamp
from extract_ost_lyric_sheets import clean


def file_hash(path):
    with path.open("rb") as stream: return hashlib.file_digest(stream, "sha256").hexdigest()


def span_stays_ordered(characters, previous_end, following_start):
    return bool(characters) and characters[0].start >= previous_end and characters[-1].end <= following_start


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("id", choices=["south_wind", "starlit_rails", "chollima_ride", "sleepers"])
    parser.add_argument("--roles", nargs="+", choices=["original", "vocals"], default=["original", "vocals"])
    parser.add_argument("--spoken-normalization", action="store_true", help="Separate candidate using documented Korean pronunciation aliases")
    parser.add_argument("--local-context", action="store_true", help="Recheck weak lexical lines in bounded original-audio context")
    parser.add_argument("--section-context", action="store_true", help="Jointly align authored bridge sections; requires local-context")
    parser.add_argument("--all-sections", action="store_true", help="Include weak authored verse/chorus sections; requires section-context; separate candidate")
    parser.add_argument("--tag", default="", help="Separate evidence suffix; does not replace earlier reports")
    parser.add_argument("--nasal-assimilation", action="store_true", help="Separate 듣는 -> 든는 model-input candidate (NIKL pronunciation rule 18)")
    args = parser.parse_args()
    if args.tag and not re.fullmatch(r'[A-Za-z0-9_-]+', args.tag): raise ValueError('Invalid evidence tag')
    if args.section_context and not args.local_context: raise ValueError("Section refinement requires local context")
    if args.all_sections and not args.section_context: raise ValueError("All sections requires section context")
    import numpy as np
    import torch
    import torchaudio
    import uroman
    torch.set_num_threads(8)
    torch.hub.set_dir(str(ROOT / "Library/MusicAlignment/torch"))
    weights = ROOT / "Library/MusicAlignment/torch/checkpoints/model.pt"
    weight_hash = file_hash(weights)
    bundle = torchaudio.pipelines.MMS_FA
    model = bundle.get_model(with_star=False).eval()
    tokenizer, aligner = bundle.get_tokenizer(), bundle.get_aligner()
    romanizer = uroman.Uroman()
    source = ROOT / f"Assets/Resources/Music/LyricSheets/{args.id}.json"
    sheet = json.loads(source.read_text(encoding="utf-8"))
    original_lines = sheet["plainText"].splitlines()
    tokens, word_map = [], []
    for line_index, text in enumerate(original_lines):
        for word in text.split():
            spoken = word
            if args.id != "sleepers": spoken = spoken.replace("100", "백")
            if args.spoken_normalization and args.id != "sleepers":
                spoken = spoken.replace("AI", "에이아이").replace("천리마", "철리마")
            if args.nasal_assimilation and re.sub(r'[^가-힣]', '', word) == '듣는':
                spoken = spoken.replace('듣는', '든는')
            romanized = romanizer.romanize_string(spoken, lcode="eng" if args.id == "sleepers" else "kor").lower()
            romanized = re.sub("[^a-z]", "", romanized)
            if not romanized: continue
            plain_word = re.sub(r"[\W_]", "", word.lower())
            kind = "vocalization" if plain_word == "아" or re.fullmatch("m+", plain_word) else "lyric"
            tokens.append(romanized); word_map.append(dict(line=line_index, text=word, romanized=romanized, pronunciationInput=spoken, kind=kind))
    targets = tokenizer(tokens)
    audio_source = ROOT / f"Assets/Resources/bgm/{args.id}.mp3"
    audio_hash = file_hash(audio_source)
    word_frequency = Counter(re.sub(r"[^\w]", "", word["text"].lower()) for word in word_map)
    coarse_words = {}
    for key in (args.id, args.id + ".authored-vocals"):
        asr_file = ROOT / f"tools/music/alignment_review/small/{key}.asr.json"
        asr_meta = asr_file.with_name(asr_file.name.replace(".asr.json", ".asr.meta.json"))
        if not asr_file.exists() or not asr_meta.exists(): continue
        if json.loads(asr_meta.read_text())["audioSha256"] != audio_hash: continue
        for segment in json.loads(asr_file.read_text(encoding="utf-8")):
            for word in segment["words"]:
                token = re.sub(r"[^\w]", "", word["word"].lower())
                if word.get("probability", 0) >= .75 and word["end"] > word["start"]:
                    coarse_words.setdefault(token, []).append(dict(start=word["start"], end=word["end"], evidence=str(asr_file.relative_to(ROOT))))
    cache = ROOT / "Library/MusicAlignment/mms"; cache.mkdir(exist_ok=True)
    results = []
    for role in args.roles:
        path = audio_source if role == "original" else ROOT / f"Library/MusicAlignment/vocals/{args.id}.wav"
        if role == "vocals":
            provenance = json.loads(path.with_suffix(".json").read_text())
            if provenance["originalAudioSha256"] != audio_hash or provenance["timeOffset"] != 0 or provenance["derivedAudioSha256"] != file_hash(path):
                raise ValueError("Unverified derived waveform")
        identity = dict(audioSha256=file_hash(path), modelSha256=weight_hash, pipeline=1)
        cached = cache / f"{args.id}.{role}.pt"; metadata = cached.with_suffix(".json")
        if cached.exists() and metadata.exists() and json.loads(metadata.read_text()) == identity:
            values = torch.load(cached, weights_only=True); emission, times = values["emission"], values["times"]
        else:
            pcm = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-f", "f32le", "-ac", "1", "-ar", "16000", "-"], check=True, capture_output=True).stdout
            audio = torch.from_numpy(np.frombuffer(pcm, dtype=np.float32).copy())
            emissions, frame_times = [], []
            with torch.inference_mode():
                for start in range(0, len(audio), 8 * 16000):
                    end = min(len(audio), start + 8 * 16000)
                    left, right = max(0, start - 16000), min(len(audio), end + 16000)
                    if right - left < 400: continue
                    e, _ = model(audio[left:right].unsqueeze(0))
                    # Wav2Vec2 conv stride 320 samples, receptive field 400 samples.
                    centres = left / 16000 + (torch.arange(e.shape[1]) * 320 + 199.5) / 16000
                    keep = (centres >= start / 16000) & (centres < end / 16000)
                    emissions.append(e[0, keep].cpu()); frame_times.append(centres[keep])
                    print(f"{args.id}/{role}: phonetic emissions {end / 16000:.1f}/{len(audio) / 16000:.1f}s", flush=True)
            emission, times = torch.cat(emissions), torch.cat(frame_times)
            torch.save(dict(emission=emission, times=times), cached); write_json(metadata, identity)
        spans = aligner(emission, targets)
        local_refinements = []
        rejected_local = []
        if args.local_context:
            reference = json.loads((ROOT / f"tools/music/alignment_review/small/{args.id}.authored-prompt.review.json").read_text(encoding="utf-8"))
            if reference["audioSha256"] != audio_hash or [line["text"] for line in reference["lines"]] != original_lines:
                raise ValueError("Local context reference is stale")
            def quality(groups):
                fragmented, weak, total, mass = 0, 0, 0, 0
                for chars in groups:
                    gap = max([float(times[b.start] - times[a.end - 1]) for a, b in zip(chars, chars[1:])] or [0])
                    count = sum(c.end - c.start for c in chars)
                    score = sum(c.score * (c.end - c.start) for c in chars) / max(1, count)
                    fragmented += gap > .8; weak += score < .05
                    total += score * count; mass += count
                return (fragmented, weak, -total / max(1, mass))
            for line_index in range(len(original_lines)):
                indices = [i for i, word in enumerate(word_map) if word["line"] == line_index]
                if not any(word_map[i]["kind"] == "lyric" for i in indices): continue
                old_quality = quality([spans[i] for i in indices])
                if old_quality[0] == 0 and old_quality[1] == 0 and -old_quality[2] >= .15: continue
                low, high = line_index, line_index + 1
                if low > 0 and reference["lines"][low]["start"] - reference["lines"][low - 1]["end"] < 5: low -= 1
                if high < len(original_lines) and reference["lines"][high]["start"] - reference["lines"][high - 1]["end"] < 5: high += 1
                lower = max(0, reference["lines"][low]["start"] - 3)
                upper = reference["lines"][high - 1]["end"] + 3
                lo, hi = int(torch.searchsorted(times, lower)), min(len(times), int(torch.searchsorted(times, upper)))
                context_indices = [i for i, word in enumerate(word_map) if low <= word["line"] < high]
                local = aligner(emission[lo:hi], [targets[i] for i in context_indices])
                candidates = {i: [type(c)(c.token, c.start + lo, c.end + lo, c.score) for c in chars] for i, chars in zip(context_indices, local)}
                new_quality = quality([candidates[i] for i in indices])
                previous_end = spans[indices[0] - 1][-1].end if indices[0] > 0 else 0
                following_start = spans[indices[-1] + 1][0].start if indices[-1] + 1 < len(spans) else len(times)
                score_ok = -new_quality[2] >= max(.15, -.8 * old_quality[2])
                order_ok = candidates[indices[0]][0].start >= previous_end and candidates[indices[-1]][-1].end <= following_start
                if new_quality < old_quality and score_ok and order_ok:
                    local_refinements.append(dict(line=line_index + 1, window=[lower, upper], previousQuality=old_quality, selectedQuality=new_quality))
                    for i in indices: spans[i] = candidates[i]
                elif new_quality < old_quality:
                    rejected_local.append(dict(line=line_index + 1, window=[lower, upper], previousQuality=old_quality,
                                               candidateQuality=new_quality, scoreAccepted=score_ok, orderAccepted=order_ok))
            if args.section_context:
                authored, section_id, section = [], 0, ""
                for raw in sheet["promptText"].splitlines():
                    value = raw.strip()
                    if value.startswith("[") and value.endswith("]"):
                        section_id += 1; section = value[1:-1]
                    for text in clean(raw).splitlines(): authored.append((section_id, section, text))
                if [item[2] for item in authored] != original_lines: raise ValueError("Authored section mapping changed lyrics")
                for identifier in sorted({item[0] for item in authored if args.all_sections or item[1].lower().startswith("bridge")}):
                    line_ids = [i for i, item in enumerate(authored) if item[0] == identifier]
                    indices = [i for i, word in enumerate(word_map) if word["line"] in line_ids]
                    if not indices: continue
                    initial_quality = quality([spans[i] for i in indices])
                    if not authored[line_ids[0]][1].lower().startswith("bridge") and initial_quality[0] == 0 and initial_quality[1] == 0 and -initial_quality[2] >= .15:
                        continue
                    lower = max(0, reference["lines"][line_ids[0]]["start"] - 8)
                    upper = reference["lines"][line_ids[-1]]["end"] + 8
                    if indices[0] > 0: lower = max(lower, float(times[spans[indices[0] - 1][-1].end - 1]) + .02)
                    if indices[-1] + 1 < len(spans): upper = min(upper, float(times[spans[indices[-1] + 1][0].start]) - .02)
                    lo, hi = int(torch.searchsorted(times, lower)), min(len(times), int(torch.searchsorted(times, upper)))
                    if hi <= lo: continue
                    local = aligner(emission[lo:hi], [targets[i] for i in indices])
                    candidate = [[type(c)(c.token, c.start + lo, c.end + lo, c.score) for c in chars] for chars in local]
                    old_quality, new_quality = quality([spans[i] for i in indices]), quality(candidate)
                    accepted = new_quality < old_quality and -new_quality[2] >= max(.15, -.8 * old_quality[2])
                    record = dict(method="authored_section_joint_ctc", section=authored[line_ids[0]][1], lines=[line_ids[0] + 1, line_ids[-1] + 1], window=[lower, upper],
                                  previousQuality=old_quality, candidateQuality=new_quality)
                    if accepted:
                        local_refinements.append(record)
                        for i, chars in zip(indices, candidate): spans[i] = chars
                    else: rejected_local.append(record)
        words = []
        selected_frame_end = 0
        for word_index, (info, characters) in enumerate(zip(word_map, spans)):
            following_start = spans[word_index + 1][0].start if word_index + 1 < len(spans) else len(times)
            repair = None
            pronunciation_variant = None
            asr_constraint = None
            token = re.sub(r"[^\w]", "", info["text"].lower())
            anchors = coarse_words.get(token, [])
            initial_gap = max([float(times[b.start] - times[a.end - 1]) for a, b in zip(characters, characters[1:])] or [0])
            if initial_gap > .8 and word_frequency[token] == 1 and anchors and max(a["start"] for a in anchors) - min(a["start"] for a in anchors) < 4:
                lower = max(0, min(a["start"] for a in anchors) - .6)
                upper = max(a["end"] for a in anchors) + .6
                lo, hi = int(torch.searchsorted(times, lower)), min(len(times), int(torch.searchsorted(times, upper)))
                if hi - lo > len(targets[word_index]) * 2:
                    local = aligner(emission[lo:hi], [targets[word_index]])[0]
                    asr_constraint = dict(reason="unique_word_high_probability_independent_ASR_window", window=[lower, upper], evidence=anchors,
                                          originalStart=float(times[characters[0].start]), originalEnd=float(times[characters[-1].end - 1]))
                    candidate_chars = [type(c)(c.token, c.start + lo, c.end + lo, c.score) for c in local]
                    asr_constraint['orderAccepted'] = span_stays_ordered(candidate_chars, selected_frame_end, following_start)
                    if asr_constraint['orderAccepted']: characters = candidate_chars
            clusters = [[0]]
            for index in range(1, len(characters)):
                gap = float(times[characters[index].start] - times[characters[index - 1].end - 1])
                if gap > .8: clusters.append([])
                clusters[-1].append(index)
            if len(clusters) > 1:
                dense = max(clusters, key=len)
                if len(dense) >= max(2, len(characters) * .6):
                    lo = max(0, int(torch.searchsorted(times, times[characters[dense[0]].start] - .6)))
                    hi = min(len(times), int(torch.searchsorted(times, times[characters[dense[-1]].end - 1] + .6)))
                    lo, hi = max(lo, selected_frame_end), min(hi, following_start)
                    if hi - lo < len(targets[word_index]) * 2:
                        # Keep the original span when the bounded candidate cannot fit.
                        local = None
                    else:
                        local = aligner(emission[lo:hi], [targets[word_index]])[0]
                    repair = dict(reason="isolated_phoneme_across_long_gap", originalStart=float(times[characters[0].start]),
                                  originalEnd=float(times[characters[-1].end - 1]), retainedClusterCharacters=len(dense),
                                  totalCharacters=len(characters), localWindow=[float(times[lo]), float(times[hi - 1])]
                                  if hi > lo else None)
                    candidate_chars = [type(c)(c.token, c.start + lo, c.end + lo, c.score) for c in local] if local is not None else []
                    repair['orderAccepted'] = span_stays_ordered(candidate_chars, selected_frame_end, following_start)
                    if repair['orderAccepted']: characters = candidate_chars
            # 받침 ㅅ + ㅎ surfaces as aspirated ㅌ; preserve the written lyric.
            # Reference: NIKL standard pronunciation rule 12 (mcfaq_seq=8745).
            if re.sub(r"[^가-힣]", "", info["text"]) == "따뜻해":
                phonetic = re.sub("[^a-z]", "", romanizer.romanize_string("따뜨태", lcode="kor").lower())
                lo = max(0, int(torch.searchsorted(times, times[characters[0].start] - .6)))
                hi = min(len(times), int(torch.searchsorted(times, times[characters[-1].end - 1] + .6)))
                variant_chars = aligner(emission[lo:hi], tokenizer([phonetic]))[0]
                variant_chars = [type(c)(c.token, c.start + lo, c.end + lo, c.score) for c in variant_chars]
                def quality(chars):
                    gap = max([float(times[b.start] - times[a.end - 1]) for a, b in zip(chars, chars[1:])] or [0])
                    count = sum(c.end - c.start for c in chars)
                    return gap, sum(c.score * (c.end - c.start) for c in chars) / max(1, count)
                old_gap, old_score = quality(characters); new_gap, new_score = quality(variant_chars)
                if (new_gap <= .8 and (old_gap > .8 or new_score > old_score)
                        and span_stays_ordered(variant_chars, selected_frame_end, following_start)):
                    pronunciation_variant = dict(spoken="따뜨태", romanized=phonetic, originalRomanized=info["romanized"],
                                                 previousGap=old_gap, previousScore=old_score)
                    characters = variant_chars
            if not span_stays_ordered(characters, selected_frame_end, following_start):
                raise ValueError('Phonetic refinement broke word order')
            selected_frame_end = characters[-1].end
            count = sum(c.end - c.start for c in characters)
            score = sum(c.score * (c.end - c.start) for c in characters) / max(1, count)
            words.append(dict(**info, start=max(0, float(times[characters[0].start]) - .01),
                              end=float(times[characters[-1].end - 1]) + .01, score=score, cohesionRepair=repair, pronunciationVariant=pronunciation_variant, asrConstraint=asr_constraint,
                              maxInternalGap=max([float(times[b.start] - times[a.end - 1]) for a, b in zip(characters, characters[1:])] or [0])))
        lines = []
        previous = 0
        for index, text in enumerate(original_lines):
            line_words = [word for word in words if word["line"] == index]
            if not line_words: raise ValueError("Source line lacks phonetic tokens: " + text)
            start, end = line_words[0]["start"], line_words[-1]["end"]
            score = sum(w["score"] * (w["end"] - w["start"]) for w in line_words) / sum(w["end"] - w["start"] for w in line_words)
            flags = []
            if score < .15: flags.append("low_phonetic_score")
            lexical_words = [w for w in line_words if w["kind"] == "lyric"]
            if not lexical_words: flags.append("nonlexical_vocalization_needs_timing")
            if any(w["score"] < .05 for w in lexical_words): flags.append("weak_word_evidence")
            if any(w["maxInternalGap"] > .8 for w in line_words): flags.append("fragmented_word_alignment")
            if start < previous - .01 or end <= start: flags.append("invalid_interval")
            previous = end
            lines.append(dict(line=index + 1, text=text, start=start, end=end, phoneticScore=score, flags=flags, words=line_words))
        results.append(dict(role=role, lines=lines, localContextRefinements=local_refinements, rejectedLocalContext=rejected_local))
    directory = ROOT / "tools/music/alignment_review/phonetic"; directory.mkdir(exist_ok=True)
    report = dict(id=args.id, model="torchaudio MMS_FA", modelSha256=weight_hash, audioSha256=audio_hash,
                  sourceSha256=file_hash(source), sourceRevision=sheet.get("promptRevision"),
                  uromanVersion=importlib.metadata.version("uroman"), status="phonetic_candidates_needs_listening",
                  pronunciationPolicy=("documented_Korean_aliases" if args.spoken_normalization else "uroman_orthographic")
                    + ("+NIKL_rule18_nasal_candidate" if args.nasal_assimilation else ""),
                  alignmentCodeSha256=file_hash(Path(__file__)),
                  humanListeningVerified=False, rounds=results)
    key = args.id + (".spoken" if args.spoken_normalization else "")
    if args.local_context: key += ".local"
    if args.section_context: key += ".sections"
    if args.all_sections: key += ".allsections"
    if args.tag: key += "." + args.tag
    write_json(directory / f"{key}.json", report)
    for run in results:
        lines = run["lines"]; lrc = ["[by:phonetic alignment candidate - not listening verified]"]
        for index, line in enumerate(lines):
            lrc.append(f"[{stamp(line['start'])}]" + line["text"])
            following = lines[index + 1]["start"] if index + 1 < len(lines) else line["end"] + 2
            if following - line["end"] >= 1.5: lrc.append(f"[{stamp(line['end'])}]")
        (directory / f"{key}.{run['role']}.candidate.lrc").write_text("\n".join(lrc) + "\n", encoding="utf-8")
        print(f"{args.id}/{run['role']}: {sum(bool(l['flags']) for l in lines)}/{len(lines)} weak phonetic lines", flush=True)


if __name__ == "__main__": main()
