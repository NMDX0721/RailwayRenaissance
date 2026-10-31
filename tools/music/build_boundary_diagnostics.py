"""Plot real source/vocal RMS around unresolved cues, without inferring sung words."""
import hashlib
import json
from pathlib import Path
import subprocess
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def envelope(path, start, end):
    raw = subprocess.check_output(["ffmpeg", "-v", "error", "-ss", str(start), "-i", str(path),
                                   "-t", str(end - start), "-f", "f32le", "-ac", "1", "-ar", "16000", "-"])
    samples = np.frombuffer(raw, dtype=np.float32)
    frame = 320
    data = samples[:len(samples) // frame * frame].reshape(-1, frame)
    return start + (np.arange(len(data)) + .5) * frame / 16000, np.sqrt(np.mean(data * data, axis=1))


def main():
    output = ROOT / "tools/music/alignment_review/boundary_diagnostics"
    output.mkdir(exist_ok=True)
    records = []
    for track, variant, windows in (
        ("chollima_ride", "spoken.local.sections.allsections", [(17, 65, 76), (42, 158, 176), (44, 186, 202)]),
        ("starlit_rails", "local.sections", [(21, 99, 112), (25, 127, 139), (46, 210, 228)]),
    ):
        report_path = ROOT / f"tools/music/alignment_review/phonetic/{track}.{variant}.json"
        report = json.loads(report_path.read_text(encoding="utf-8"))
        original = ROOT / f"Assets/Resources/bgm/{track}.mp3"
        vocals = ROOT / f"Library/MusicAlignment/vocals/{track}.wav"
        provenance = json.loads(vocals.with_suffix(".json").read_text(encoding="utf-8"))
        original_hash, vocal_hash = digest(original), digest(vocals)
        if (report["audioSha256"] != original_hash or provenance["originalAudioSha256"] != original_hash
                or provenance["derivedAudioSha256"] != vocal_hash or provenance["timeOffset"] != 0):
            raise ValueError("Source or derived audio provenance mismatch")
        for number, start, end in windows:
            fig, axes = plt.subplots(2, 1, figsize=(13, 5), sharex=True, layout="constrained")
            cues = []
            for ax, role, source in zip(axes, ("original", "vocals"), (original, vocals)):
                x, rms = envelope(source, start, end)
                ax.plot(x, rms, color="#ad8240", linewidth=.85)
                cue = next(run for run in report["rounds"] if run["role"] == role)["lines"][number - 1]
                for word in cue["words"]:
                    ax.axvspan(word["start"], word["end"], color="#457d7d", alpha=.2)
                ax.axvline(cue["start"], color="#387d4b", linestyle="--")
                ax.axvline(cue["end"], color="#a9524d", linestyle="--")
                ax.set(ylabel=f"{role} RMS", xlim=(start, end), ylim=(0, None))
                ax.grid(alpha=.18)
                cues.append(dict(role=role, text=cue["text"], start=cue["start"], end=cue["end"], flags=cue["flags"]))
            axes[-1].set_xlabel("Original recording seconds (20 ms RMS windows)")
            fig.suptitle(f"{track} / line {number} / candidate boundaries, not listening approved\n"
                         "Energy is not proof of a sung word; separated vocals can omit or retain instruments.", fontsize=11)
            name = f"{track}-line-{number:02d}.png"
            fig.savefig(output / name, dpi=140)
            plt.close(fig)
            records.append(dict(id=track, line=number, interval=[start, end], image=name,
                                originalAudioSha256=original_hash, vocalAudioSha256=vocal_hash,
                                candidateSha256=digest(report_path), cues=cues, humanListeningVerified=False))
    (output / "manifest.json").write_text(json.dumps(dict(
        status="diagnostic_only_not_word_timing", records=records), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"BOUNDARY_DIAGNOSTICS_OK {len(records)} original/vocal comparisons")


if __name__ == "__main__":
    main()
