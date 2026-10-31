"""Download pinned local alignment/recognition assets; never send local media."""
from concurrent.futures import ThreadPoolExecutor
import argparse
import hashlib
import json
from pathlib import Path
from download_alignment_model import request, ranged_part

REPO = 'Qwen/Qwen3-ForcedAligner-0.6B'
REVISION = 'c7cbfc2048c462b0d63a45797104fc9db3ad62b7'


def sha256(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', choices=['huggingface', 'modelscope', 'hf-mirror'], default='huggingface')
    parser.add_argument('--model', choices=['aligner', 'asr', 'asr-1.7b', 'korean-ctc'], default='aligner')
    parser.add_argument('--workers',type=int,choices=range(1,17),default=6)
    args = parser.parse_args()
    repo, revision = ((REPO, REVISION) if args.model == 'aligner' else
                      ('Qwen/Qwen3-ASR-0.6B', '5eb144179a02acc5e5ba31e748d22b0cf3e303b0'))
    if args.model == 'korean-ctc':
        repo, revision = 'kresnik/wav2vec2-large-xlsr-korean', '629c9a3501c10ba128bf3fa1eebb12af3be03f61'
    elif args.model == 'asr-1.7b':
        repo, revision = 'Qwen/Qwen3-ASR-1.7B', '7278e1e70fe206f11671096ffdd38061171dd6e5'
    folder = args.model if args.model == 'korean-ctc' else 'qwen3-'+args.model
    target = Path(__file__).resolve().parents[2] / f'Library/MusicAlignment/models/{folder}'
    target.mkdir(parents=True, exist_ok=True)
    with request(f'https://huggingface.co/api/models/{repo}/revision/{revision}?blobs=true') as response:
        info = json.load(response)
    if info['sha'] != revision:
        raise ValueError('Revision mismatch')
    evidence = []
    for entry in info['siblings']:
        name = entry['rfilename']
        if '/' in name or name.startswith('.'):
            continue
        if args.model == 'korean-ctc' and name not in {'README.md','config.json','model.safetensors','preprocessor_config.json','tokenizer_config.json','special_tokens_map.config','vocab.json'}:
            continue  # Do not download optimizer/trainer/pickle checkpoints.
        dest = target / name
        expected = entry.get('lfs', {}).get('sha256')
        size = entry['size']
        def valid_existing():
            if not dest.exists() or dest.stat().st_size != size:
                return False
            if expected:
                return sha256(dest) == expected
            blob = dest.read_bytes()
            return bool(entry.get('blobId')) and hashlib.sha1(f'blob {len(blob)}\0'.encode()+blob).hexdigest() == entry['blobId']
        if not valid_existing():
            url = f'https://huggingface.co/{repo}/resolve/{revision}/{name}'
            if args.source == 'modelscope' and name != 'README.md':
                # Mirror is mutable: require the immutable HF LFS digest before accepting it.
                url = f'https://modelscope.cn/models/{repo}/resolve/master/{name}'
            elif args.source == 'hf-mirror':
                url = f'https://hf-mirror.com/{repo}/resolve/{revision}/{name}'
            partial = dest.with_suffix(dest.suffix + '.partial')
            if size > 8 * 1024 * 1024:
                parts = target / (name + '.parts')
                parts.mkdir(exist_ok=True)
                ranges = [(s, min(s + 4 * 1024 * 1024 - 1, size - 1)) for s in range(0, size, 4 * 1024 * 1024)]
                with ThreadPoolExecutor(max_workers=args.workers) as pool:
                    files = list(pool.map(lambda span: ranged_part(url, parts, *span), ranges))
                with partial.open('wb') as stream:
                    for part in files:
                        stream.write(part.read_bytes())
            else:
                with request(url) as response:
                    partial.write_bytes(response.read())
            if partial.stat().st_size != size or (expected and sha256(partial) != expected):
                raise ValueError('Asset validation failed: ' + name)
            if not expected and entry.get('blobId'):
                blob = partial.read_bytes()
                if hashlib.sha1(f'blob {len(blob)}\0'.encode() + blob).hexdigest() != entry['blobId']:
                    raise ValueError('Git blob hash mismatch: ' + name)
            partial.replace(dest)
        evidence.append(dict(file=name, size=size, sha256=sha256(dest), officialLfsSha256=expected, officialGitBlobId=entry.get('blobId')))
    (target / 'verification.json').write_text(json.dumps(dict(repo=repo, revision=revision, files=evidence), indent=2), encoding='utf-8')
    print('QWEN_ASSETS_VERIFIED ' + args.model, flush=True)


if __name__ == '__main__':
    main()
