"""Local official torchaudio HDemucs stems for timing review, never game audio replacement."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from align_ost_lyrics import ROOT


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("id", choices=["south_wind", "starlit_rails", "chollima_ride", "sleepers"])
    parser.add_argument("--normalize", action="store_true", help="Use official tutorial's mono-reference normalization; save separate candidate")
    args = parser.parse_args()
    import numpy as np
    import torch
    import torchaudio
    torch.set_num_threads(8)
    torch.hub.set_dir(str(ROOT / "Library/MusicAlignment/torch"))
    bundle = torchaudio.pipelines.HDEMUCS_HIGH_MUSDB_PLUS
    model = bundle.get_model().eval()
    source = ROOT / f"Assets/Resources/bgm/{args.id}.mp3"
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(source), "-f", "f32le", "-ac", "2", "-ar", "44100", "-"], check=True, capture_output=True).stdout
    audio = torch.from_numpy(np.frombuffer(raw, dtype=np.float32).copy().reshape(-1, 2).T.copy())
    original_samples = audio.shape[1]
    reference_mean, reference_std = 0., 1.
    if args.normalize:
        # https://github.com/pytorch/audio/blob/main/examples/tutorials/hybrid_demucs_tutorial.py
        reference = audio.mean(0)
        reference_mean, reference_std = float(reference.mean()), float(reference.std())
        if not np.isfinite(reference_std) or reference_std < 1e-8:
            raise ValueError("Cannot normalize silent or nonfinite audio")
        audio = (audio - reference_mean) / reference_std
    output = torch.zeros_like(audio); total_weight = torch.zeros(audio.shape[1])
    chunk, hop = 8 * 44100, 6 * 44100
    with torch.inference_mode():
        for start in range(0, audio.shape[1], hop):
            end = min(start + chunk, audio.shape[1])
            part = audio[:, start:end]
            separated = model(part.unsqueeze(0))[0, model.sources.index("vocals")]
            weight = torch.ones(end - start)
            fade = min(44100, (end - start) // 2)
            if start > 0: weight[:fade] = torch.linspace(0, 1, fade)
            if end < audio.shape[1]: weight[-fade:] = torch.linspace(1, 0, fade)
            output[:, start:end] += separated * weight
            total_weight[start:end] += weight
            print(f"{args.id}: vocals {end / 44100:.1f}/{audio.shape[1] / 44100:.1f}s", flush=True)
    output /= total_weight.clamp_min(1e-8)
    if args.normalize: output = output * reference_std + reference_mean
    if output.shape[1] != original_samples or not torch.isfinite(output).all():
        raise ValueError("Separation changed sample count or produced nonfinite samples")
    directory = ROOT / "Library/MusicAlignment/vocals"; directory.mkdir(exist_ok=True)
    key = args.id + (".normalized" if args.normalize else "")
    target = directory / f"{key}.wav"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", "44100", "-ac", "2", "-i", "-", "-c:a", "pcm_f32le", str(target)],
                   input=output.T.contiguous().numpy().tobytes(), check=True)
    weights = Path(torch.hub.get_dir()) / "torchaudio/models/hdemucs_high_trained.pt"
    meta = dict(id=args.id, originalAudioSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                derivedAudioSha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                model="torchaudio HDEMUCS_HIGH_MUSDB_PLUS", modelSha256=hashlib.sha256(weights.read_bytes()).hexdigest(),
                purpose="local timing evidence only; possible separation artifacts", sampleRate=44100, timeOffset=0,
                sourceSamples=original_samples, outputSamples=output.shape[1],
                normalization="official_tutorial_mono_reference" if args.normalize else "none",
                referenceMean=reference_mean, referenceStd=reference_std,
                separationCodeSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    target.with_suffix(".json").write_text(json.dumps(meta, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
