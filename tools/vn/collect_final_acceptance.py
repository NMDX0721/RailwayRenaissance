"""Freeze the final bookmark, text and viewport evidence without rewriting raw results."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--log', required=True)
    parser.add_argument('--compiled', default='tools/vn/source_compile_migration_20261103')
    args = parser.parse_args()
    isolate = ROOT / 'Library/VNCompiledReviewProject'
    review = isolate / 'tools/vn/final_review'
    compiled = ROOT / args.compiled
    output = ROOT / 'tools/vn/final_acceptance_20261103'
    result = json.loads((review / 'results.json').read_text())
    assert result['success'] and not result['errors']
    assert sum(c.startswith('VN_TEXT_FIT_OK') for c in result['checks']) == 5
    assert sum(c.startswith('VN_VIEWPORT_OK') for c in result['checks']) == 5
    assert any(c.startswith('VN_BOOKMARK_AUTO_RESUME_OK') for c in result['checks'])
    for marker in ['VN_AUDIO_CACHE_OK', 'VN_NEGATIVE_SPRITE_CACHE_OK', 'VN_TABLET_MIGRATION_RUNTIME_OK']:
        assert any(c.startswith(marker) for c in result['checks']), marker
    log = ROOT / args.log
    assert 'VN_FINAL_ACCEPTANCE_OK' in log.read_text(encoding='utf-8-sig')
    compilation = json.loads((compiled / 'result.json').read_text())
    manifest = dict(scope='Isolated Windows Editor runtime, synthetic touch and viewport tests; no Android device or standalone Player acceptance',
                    sources={}, scripts={}, assets={}, artifacts={}, configuration={})
    for group in ['runtime', 'vn_review_editor']:
        assert compilation[group]['exit_code'] == 0
        for relative, expected in compilation[group]['source_sha256'].items():
            assert sha(ROOT / relative) == expected, relative
            manifest['sources'][relative] = expected
    for path in (ROOT / 'Assets/Resources/Scripts').glob('prologue_*.json'):
        relative = path.relative_to(ROOT)
        assert sha(path) == sha(isolate / relative), str(relative)
        manifest['scripts'][relative.as_posix()] = sha(path)
    for folder in ['bg', 'cg', 'characters']:
        for path in (ROOT / 'Assets/Resources' / folder).rglob('*.png'):
            relative = path.relative_to(ROOT)
            assert sha(path) == sha(isolate / relative), str(relative)
            manifest['assets'][relative.as_posix()] = sha(path)
    for name, relative in [('Assembly-CSharp.dll', 'Assets/Plugins/Assembly-CSharp.dll'), ('Assembly-CSharp-Editor.dll', 'Assets/Editor/Assembly-CSharp-Editor.dll')]:
        assert sha(compiled / name) == sha(isolate / relative), name
        manifest['artifacts'][relative] = sha(compiled / name)
    manifest['configuration']['ProjectSettings/ProjectSettings.asset'] = sha(ROOT / 'ProjectSettings/ProjectSettings.asset')
    output.mkdir(exist_ok=True)
    screen_names = [entry.split()[1] for entry in result['checks'] if entry.startswith('VN_VIEWPORT_OK ')]
    pictures = [review / f'{kind}_{name}.png' for name in screen_names for kind in ['portraits', 'choices']]
    pictures += [review / 'bookmarks_android.png', review / 'tablet_shell_return_journey_bookmarks.png']
    for path in [log, review / 'results.json', compiled / 'result.json', ROOT / 'tools/vn/final_text_audit_20261103.json',
                 ROOT / 'tools/vn/prologue_asset_audit_final_20261103.json'] + pictures:
        shutil.copy2(path, output / path.name)
        manifest['artifacts'][path.name] = sha(path)
    (output / 'evidence.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('VN_FINAL_EVIDENCE_OK', len(manifest['sources']), 'sources;', len(manifest['scripts']), 'scripts;', len(manifest['assets']), 'PNGs')


if __name__ == '__main__':
    main()
