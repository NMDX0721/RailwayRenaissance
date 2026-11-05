"""Collect professor rendering evidence and preserve any unresolved static branch issues."""
import hashlib
import json
from pathlib import Path
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / 'Library/VNCompiledReviewProject'
OUT = ROOT / 'tools/vn/professor_acceptance_20261102'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(exist_ok=True)
    log = ROOT / 'tools/vn/professor_expressions_review_20261102.log'
    text = log.read_text(encoding='utf-8-sig')
    assert 'VN_PROFESSOR_SIX_EXPRESSIONS_OK' in text and 'VN_ASSET_COMPLETION_REVIEW_FAILED' not in text
    compiled = json.loads((ROOT / 'tools/vn/source_compile_review/result.json').read_text())
    for path in ['Assets/Scripts/VN/CharacterSpriteManager.cs', 'Assets/Scripts/LoginManager.cs']:
        assert sha(ROOT / path) == compiled['runtime']['source_sha256'][path]
    shots = []
    for emotion in ['normal', 'serious', 'gentle', 'sad', 'smile', 'angry']:
        asset = ROOT / f'Assets/Resources/characters/daoshi_{emotion}.png'
        source = REVIEW / f'Assets/Resources/characters/daoshi_{emotion}.png'
        assert sha(asset) == sha(source)
        meta = asset.with_suffix('.png.meta')
        if not meta.exists():
            shutil.copy2(source.with_suffix('.png.meta'), meta)
        shot = REVIEW / f'tools/vn/unity_review/professor_{emotion}_2560x1440.png'
        assert Image.open(shot).size == (2560, 1440)
        shutil.copy2(shot, OUT / shot.name)
        shots.append(dict(expression=emotion, file=shot.name, sha256=sha(shot), asset_sha256=sha(asset)))
    for name in ['two_character_layout_2560x1440', 'three_character_layout_2560x1440']:
        source = REVIEW / f'tools/vn/unity_review/{name}.png'
        shutil.copy2(source, OUT / source.name)
    shutil.copy2(log, OUT / log.name)
    shutil.copy2(ROOT / 'tools/vn/source_compile_review/result.json', OUT / 'source_compile.json')
    shutil.copy2(ROOT / 'tools/vn/legacy_startup_review_20261102.log', OUT / 'legacy_startup_review_20261102.log')
    hashes, issues = {}, []
    choices = professor_lines = 0
    for path in sorted((ROOT / 'Assets/Resources/Scripts').glob('prologue_*.json')):
        script = json.loads(path.read_text(encoding='utf-8-sig'))
        hashes[path.name] = sha(path)
        for scene_index, scene in enumerate(script['scenes']):
            for entry_index, entry in enumerate(scene['d']):
                professor_lines += entry.get('s') == '佯教授'
                if entry.get('t') != 'c':
                    continue
                choices += 1
                for option_index, option in enumerate(entry.get('opts', [])):
                    target = option.get('next')
                    if not isinstance(target, int) or not 0 <= target < len(script['scenes']):
                        issues.append(dict(script=path.name, scene=scene_index, entry=entry_index,
                            option=option_index, next=target, scene_count=len(script['scenes'])))
    result = dict(character='佯明远', title='佯教授', resolution=[2560, 1440], shots=shots,
        professor_speaker_entries=professor_lines, choice_entries_inspected=choices,
        static_choice_target_issues=issues, script_sha256=hashes,
        login_manager_sha256=sha(ROOT / 'Assets/Scripts/LoginManager.cs'),
        scope='Isolated Unity renderer, six source-matched expressions, aliases and 2.5K layouts; separate LoginManager routing fixture',
        limitations='Not standalone Player/full-playthrough acceptance; existing UnityEditor.Search startup index exception remains; old exe requires rebuild.')
    (OUT / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'PROFESSOR_ACCEPTANCE_SAVED expressions=6 professor_lines={professor_lines} unresolved_choice_targets={len(issues)}')


if __name__ == '__main__':
    main()
