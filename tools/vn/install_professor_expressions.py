"""Validate generated professor cutouts and copy exact imagegen outputs into Resources."""
import hashlib
import json
from pathlib import Path
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / 'tools/vn/mentor_expression_work'
EMOTIONS = ['normal', 'serious', 'gentle', 'sad', 'smile', 'angry']


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    entries = []
    for emotion in EMOTIONS:
        entry = json.loads((WORK / ('final_' + emotion + '.json')).read_text(encoding='utf-8'))
        source = Path(entry['source'])
        with Image.open(source) as image:
            assert image.mode == 'RGBA' and image.size == (1024, 1536), emotion
            alpha = image.getchannel('A')
            regions = [(0, 0, 64, 64), (960, 0, 1024, 64), (0, 0, 16, 1536), (1008, 0, 1024, 1536)]
            maxima = [alpha.crop(region).getextrema()[1] for region in regions]
            assert max(maxima) <= 1, f'{emotion}: visible background residue {maxima}'
            body_bbox = alpha.crop((0, 600, 1024, 1536)).point(lambda value: 255 if value > 200 else 0).getbbox()
        entry.update(resource='characters/daoshi_' + emotion,
            destination='Assets/Resources/characters/daoshi_' + emotion + '.png',
            sha256=sha(source), pixels=[1024, 1536], background_alpha_maxima=maxima,
            alpha_tolerance='max 1/255 in corner/outer-edge empty regions', body_bbox_below_y600=body_bbox)
        entries.append(entry)
    for entry in entries:
        destination = ROOT / entry['destination']
        if entry['emotion'] == 'normal' and destination.exists():
            archive = WORK / 'archived_normal_before_cleanup.png'
            if not archive.exists():
                shutil.copy2(destination, archive)
        shutil.copy2(entry['source'], destination)
        assert sha(destination) == entry['sha256']
    report = dict(character='佯明远', title='佯教授', internal_id='daoshi',
        base_provenance='tools/vn/mentor_portrait_provenance_20261102.json', entries=entries,
        notes=['Clean full portrait variants, not chibi.',
            'No CPU pixel editing; production PNGs are exact copies of built-in imagegen outputs.',
            'Rejected black-corner images and magenta intermediate are not shipped.'])
    (ROOT / 'tools/vn/professor_expressions_provenance_20261102.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    manifest_path = ROOT / 'tools/vn/generated_asset_manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    owned = {entry['resource'] for entry in entries}
    manifest['entries'] = [entry for entry in manifest['entries'] if entry['resource'] not in owned] + entries
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('PROFESSOR_SIX_EXPRESSIONS_INSTALLED alpha regions max<=1/255; original image bytes preserved')


if __name__ == '__main__':
    main()
