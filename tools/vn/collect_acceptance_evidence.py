"""Collect verified VN renderer evidence without marking the game release-ready."""
import hashlib
import json
import pathlib
import shutil

from PIL import Image, ImageStat

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/vn/acceptance_20261102'
REVIEW = ROOT / 'Library/VNCompiledReviewProject'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    log = ROOT / 'tools/vn/unity_compiled_review_20261102c.log'
    text = log.read_text(encoding='utf-8-sig')
    markers = ['VN_PLAYER_PORTRAITS_HIDDEN_OK scripts=10 dialogueConfigurations=1207',
               'VN_CG_LAYOUT_AND_CLICK_OK', 'VN_ASSET_COMPLETION_REVIEW_OK all seven CGs displayed']
    assert all(marker in text for marker in markers)
    assert 'VN_ASSET_COMPLETION_REVIEW_FAILED' not in text
    compilation = json.loads((ROOT / 'tools/vn/source_compile_review/result.json').read_text())
    post_compile_changes = []
    for part in ['runtime', 'vn_review_editor']:
        assert compilation[part]['exit_code'] == 0
        for path, expected in compilation[part]['source_sha256'].items():
            if sha(ROOT / path) != expected:
                assert not path.startswith('Assets/Scripts/VN/') and path != 'Assets/Editor/VNAssetCompletionReview.cs', 'VN source changed after compilation: ' + path
                post_compile_changes.append(path)
    resources = []
    manifest = json.loads((ROOT / 'tools/vn/generated_asset_manifest.json').read_text(encoding='utf-8-sig'))
    for entry in manifest['entries']:
        path = pathlib.Path(entry['destination'])
        source = ROOT / path
        assert sha(source) == sha(REVIEW / path), 'Reviewed resource differs from project: ' + str(path)
        with Image.open(source) as im:
            im.load()
            size = im.size
        resources.append(dict(path=path.as_posix(), sha256=sha(source), pixels=size))
    screenshots = []
    for path in sorted((REVIEW / 'tools/vn/unity_review').glob('*.png')):
        with Image.open(path) as im:
            im.load()
            assert im.size in [(1280, 720), (1024, 768)]
            assert max(ImageStat.Stat(im.convert('RGB')).stddev) > 5, 'Blank capture: ' + path.name
            pixels = im.size
        shutil.copy2(path, OUT / path.name)
        screenshots.append(dict(file=path.name, sha256=sha(path), pixels=pixels))
    assert len(screenshots) == 10
    shutil.copy2(log, OUT / log.name)
    shutil.copy2(REVIEW / 'source-evidence.json', OUT / 'source-evidence.json')
    result = dict(date='2026-11-02', decision='renderer checks passed; release acceptance incomplete',
        markers=markers, resources=resources, screenshots=screenshots, source_compile=compilation,
        post_compile_unrelated_source_changes=post_compile_changes,
        scope='Frozen compiled runtime snapshot; VN source unchanged; isolated Unity project; synthetic CG click; no full-game playthrough',
        log=dict(file=log.name, sha256=sha(log)),
        unresolved=['mentor daoshi placeholder', 'night/departure tablet hardware drawing consistency',
            'full branches, saves, backlog, auto/skip and transition to management not accepted',
            'standalone Windows Player not built/accepted', 'Japanese voice not generated'],
        harness_notes=['UnityEditor.Search.SearchDatabase startup index exception remains',
            'Default theme warning in minimal review panel; actual captures visually inspected',
            'No UPM server by design; unrelated package plugins removed from compiled review'])
    (OUT / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print('VN_ACCEPTANCE_EVIDENCE_COLLECTED resources=' + str(len(resources)) + ' screenshots=10; release incomplete')


if __name__ == '__main__':
    main()
