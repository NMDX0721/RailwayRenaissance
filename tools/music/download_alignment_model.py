"""Download pinned CTranslate2 model files with bounded, verified HTTP ranges."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import time
import urllib.request


def request(url, **headers):
    return urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=45)


def ranged_part(url, target, start, end):
    part = target / str(start)
    failures = 0
    while (offset := start + (part.stat().st_size if part.exists() else 0)) <= end:
        try:
            with request(url + f"?range_start={offset}", Range=f"bytes={offset}-{end}") as response:
                if response.status != 206 or not response.headers.get("Content-Range", "").startswith(f"bytes {offset}-"):
                    raise ValueError("Server did not honour the requested range")
                with part.open("ab") as output:
                    # read1 retains a short final network buffer before a dropped connection.
                    # read(n) can raise on a truncated response before returning those bytes.
                    while data := response.read1(64 * 1024):
                        output.write(data)
            failures = failures + 1 if start + part.stat().st_size == offset else 0
        except Exception:
            failures = failures + 1 if not part.exists() or start + part.stat().st_size == offset else 0
        if failures >= 8:
            raise RuntimeError(f"Download stalled at {offset}")
        if failures:
            time.sleep(1)
    if part.stat().st_size != end - start + 1:
        raise ValueError("Invalid range length")
    print(f"Range complete: {start}-{end}", flush=True)
    return part


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="base")
    args = parser.parse_args()
    repo = "Systran/faster-whisper-" + args.model
    with request(f"https://huggingface.co/api/models/{repo}?blobs=true") as response:
        info = json.load(response)
    target = Path(__file__).resolve().parents[2] / "Library/MusicAlignment/models" / args.model
    target.mkdir(parents=True, exist_ok=True)
    for entry in info["siblings"]:
        name = entry["rfilename"]
        if name not in ("model.bin", "config.json", "tokenizer.json", "vocabulary.txt", "vocabulary.json"):
            continue
        url = f"https://huggingface.co/{repo}/resolve/{info['sha']}/{name}"
        dest = target / name
        size = entry["size"]
        partial = dest.with_suffix(dest.suffix + ".partial")
        if dest.exists() and dest.stat().st_size == size:
            if not entry.get("lfs"):
                continue
            with dest.open("rb") as stream:
                if hashlib.file_digest(stream, "sha256").hexdigest() == entry["lfs"]["sha256"]:
                    continue
        if name != "model.bin":
            with request(url) as response:
                partial.write_bytes(response.read())
        else:
            start = partial.stat().st_size if partial.exists() else 0
            parts = target / "model.parts"
            parts.mkdir(exist_ok=True)
            ranges = [(offset, min(size - 1, offset + 4 * 1024 * 1024 - 1)) for offset in range(start, size, 4 * 1024 * 1024)]
            with ThreadPoolExecutor(max_workers=8) as pool:
                downloaded = list(pool.map(lambda pair: ranged_part(url, parts, *pair), ranges))
            with partial.open("ab") as output:
                for part in downloaded:
                    output.write(part.read_bytes())
            # The final SHA256 below verifies the complete file, including any resumed prefix.
        if partial.stat().st_size != size:
            raise ValueError(f"Invalid size: {name}")
        if entry.get("lfs"):
            with partial.open("rb") as stream:
                digest = hashlib.file_digest(stream, "sha256").hexdigest()
            if digest != entry["lfs"]["sha256"]:
                raise ValueError(f"SHA256 mismatch: {name}")
        partial.replace(dest)
    (target / "provenance.json").write_text(json.dumps(dict(repo=repo, revision=info["sha"]), indent=2), encoding="utf-8")
    print(target, flush=True)


if __name__ == "__main__":
    main()
