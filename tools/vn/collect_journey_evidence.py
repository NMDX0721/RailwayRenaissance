"""Freeze the reviewed journey artifacts; assert source and resource identity."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/vn/journey_flow_acceptance_20261102'
ISOLATE = ROOT / 'Library/VNCompiledReviewProject'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(exist_ok=True)
    result = json.loads((ROOT / 'tools/vn/source_compile_review/result.json').read_text())
    evidence = {'scope': 'Compiled isolated Unity review, not full Player acceptance',
                'sources': {}, 'scripts': {}, 'artifacts': {}}
    for group in ['runtime', 'vn_review_editor']:
        assert result[group]['exit_code'] == 0
        for name, expected in result[group]['source_sha256'].items():
            if name.startswith('Assets/Scripts/VN/') or name == 'Assets/Editor/VNAssetCompletionReview.cs':
                assert sha(ROOT / name) == expected, name
                evidence['sources'][name] = expected
    audit = json.loads((ROOT / 'tools/vn/prologue_flow_audit_20261102.json').read_text())
    for script in audit['scripts']:
        relative = 'Assets/Resources/Scripts/' + script['script'] + '.json'
        expected = script['sha256']
        assert sha(ROOT / relative) == sha(ISOLATE / relative) == expected, relative
        evidence['scripts'][relative] = expected
    files = [ROOT / 'tools/vn/journey_flow_review_20261102_verified.log',
             ROOT / 'tools/vn/source_compile_review/result.json',
             ROOT / 'tools/vn/prologue_flow_audit_20261102.json',
             ROOT / 'tools/vn/prologue_asset_audit_20261102_flow.json']
    files += [ISOLATE / 'tools/vn/unity_review' / name for name in
              ['journey_choice_2560x1440.png', 'journey_story_merge_2560x1440.png']]
    for source in files:
        shutil.copy2(source, OUT / source.name)
        evidence['artifacts'][source.name] = sha(source)
    for relative in ['Assets/Plugins/Assembly-CSharp.dll', 'Assets/Editor/Assembly-CSharp-Editor.dll']:
        compiled = ROOT / 'tools/vn/source_compile_review' / Path(relative).name
        assert sha(compiled) == sha(ISOLATE / relative)
        evidence['artifacts'][relative] = sha(compiled)
    (OUT / 'evidence.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
    print('VN_JOURNEY_EVIDENCE_OK source hashes, ten scripts and compiled assemblies match')


if __name__ == '__main__':
    main()
