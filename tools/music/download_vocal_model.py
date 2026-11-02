"""Range-download the official torchaudio review model; check length and ZIP CRC."""
from concurrent.futures import ThreadPoolExecutor
import argparse
import hashlib
import json
from pathlib import Path
import zipfile
from align_ost_lyrics import ROOT
from download_alignment_model import ranged_part

URL = "https://download.pytorch.org/torchaudio/models/hdemucs_high_trained.pt"
SIZE = 334697255


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--kind", choices=["vocals", "mms"], default="vocals")
    args = parser.parse_args()
    url, size = URL, SIZE
    target = ROOT / "Library/MusicAlignment/torch/torchaudio/models/hdemucs_high_trained.pt"
    if args.kind == "mms":
        url, size = "https://dl.fbaipublicfiles.com/mms/torchaudio/ctc_alignment_mling_uroman/model.pt", 1262047414
        target = ROOT / "Library/MusicAlignment/torch/checkpoints/model.pt"
    provenance = target.with_suffix(".download.json")
    if target.exists() and provenance.exists() and target.stat().st_size == size:
        recorded = json.loads(provenance.read_text())
        with target.open("rb") as stream: cached_hash = hashlib.file_digest(stream, "sha256").hexdigest()
        if recorded.get("url") == url and recorded.get("sha256") == cached_hash:
            print(args.kind.upper() + "_MODEL_CACHE_OK: " + cached_hash, flush=True)
            return
    parts = target.parent / (args.kind + ".parts"); parts.mkdir(parents=True, exist_ok=True)
    intervals = [(start, min(size - 1, start + 4 * 1024 * 1024 - 1)) for start in range(0, size, 4 * 1024 * 1024)]
    with ThreadPoolExecutor(max_workers=8) as pool:
        files = list(pool.map(lambda pair: ranged_part(url, parts, *pair), intervals))
    partial = target.with_suffix(".verified-download")
    with partial.open("wb") as output:
        for part in files: output.write(part.read_bytes())
    if partial.stat().st_size != size:
        raise ValueError("Incorrect model length")
    with zipfile.ZipFile(partial) as archive:
        if archive.testzip() is not None: raise ValueError("Model ZIP CRC failed")
    digest = hashlib.sha256(partial.read_bytes()).hexdigest()
    partial.replace(target)
    target.with_suffix(".download.json").write_text(json.dumps(dict(url=url, size=size, sha256=digest,
        verification="HTTPS source, exact published object length, ZIP member CRC; no separately published SHA supplied"), indent=2), encoding="utf-8")
    print(args.kind.upper() + "_MODEL_DOWNLOAD_OK: " + digest, flush=True)


if __name__ == "__main__": main()
