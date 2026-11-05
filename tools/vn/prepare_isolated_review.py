"""Snapshot the VN rendering review without opening the concurrently used project."""
import hashlib
import json
import pathlib
import re
import shutil

ROOT = pathlib.Path(__file__).resolve().parents[2]
TARGET = ROOT / 'Library/VNAssetReviewProject'


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    folders = ['Assets/Scripts', 'Assets/UI Toolkit', 'Assets/UI', 'Packages', 'ProjectSettings']
    folders += ['Assets/Resources/' + part for part in ['bg', 'cg', 'characters', 'Fonts', 'Scripts']]
    snapshot = {}
    for folder in folders:
        source = ROOT / folder
        if not source.exists():
            continue
        shutil.copytree(source, TARGET / folder, dirs_exist_ok=True)
        for path in source.rglob('*'):
            if path.is_file():
                snapshot[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    editor = TARGET / 'Assets/Editor'
    editor.mkdir(parents=True, exist_ok=True)
    for name in ['VNAssetCompletionReview.cs', 'VNAssetCompletionReview.cs.meta']:
        source = ROOT / 'Assets/Editor' / name
        if source.exists():
            shutil.copy2(source, editor / name)
            snapshot['Assets/Editor/' + name] = hashlib.sha256(source.read_bytes()).hexdigest()
    settings = TARGET / 'ProjectSettings/ProjectSettings.asset'
    text = settings.read_text(encoding='utf-8-sig')
    text = re.sub(r'(?m)^  companyName:.*$', '  companyName: RailwayRenaissanceReview', text)
    text = re.sub(r'(?m)^  productName:.*$', '  productName: VNAssetReview20261101', text)
    settings.write_text(text, encoding='utf-8')
    (TARGET / 'vn-source-snapshot.json').write_text(json.dumps(dict(source=str(ROOT),
        scope='Runtime scripts and VN resources; only VNAssetCompletionReview Editor entry; separate project identity',
        sha256=snapshot, deliberate_overrides=['ProjectSettings companyName/productName for review identity']), indent=2), encoding='utf-8')
    print(TARGET)


if __name__ == '__main__':
    main()
