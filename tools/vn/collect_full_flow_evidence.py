"""Freeze runtime checks, preserving the runner result and known Editor failures."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--log', required=True)
    args = parser.parse_args()
    isolate = ROOT / 'Library/VNCompiledReviewProject'
    review = isolate / 'tools/vn/full_flow_review'
    output = ROOT / 'tools/vn/full_flow_acceptance_20261103'
    results = json.loads((review / 'results.json').read_text())
    assert results['cases'] == 25
    assert len(results['chapters']) == 10 and len(results['options']) == 25
    log = ROOT / args.log
    raw_log = log.read_text(encoding='utf-8-sig').replace('\r\n', '\n')
    for marker in ['VN_ALL_25_OPTIONS_RUNTIME_OK', 'VN_TEN_CHAPTERS_RUNTIME_OK', 'VN_GAMEPLAY_RECEIVER_OK']:
        assert marker in raw_log, marker
    known_editor_errors = []
    for error in results['errors']:
        normalized = error.replace('\r\n', '\n')
        assert normalized.startswith('ArgumentOutOfRangeException: Index was out of range.'), error
        position = raw_log.index(normalized)
        trace = raw_log[position:].split('\n\n', 1)[0]
        assert 'UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase' in trace, trace
        assert 'UnityEditor.Search.SearchInit.IndexationOnStartup' in trace, trace
        assert 'VNManager' not in trace and 'VNPrologueFlowReview' not in trace, trace
        known_editor_errors.append(trace)
    compiled = ROOT / 'tools/vn/source_compile_flow_20261103'
    compiler = json.loads((compiled / 'result.json').read_text())
    evidence = dict(scope='Accelerated isolated Unity traversal and minimal gameplay receiver; not full Player acceptance',
                    sources={}, scripts={}, artifacts={})
    for section in ['runtime', 'vn_review_editor']:
        assert compiler[section]['exit_code'] == 0
        for relative, expected in compiler[section]['source_sha256'].items():
            assert digest(ROOT / relative) == expected, relative
            evidence['sources'][relative] = expected
    for source in sorted((ROOT / 'Assets/Resources/Scripts').glob('prologue_*.json')):
        relative = source.relative_to(ROOT)
        assert digest(source) == digest(isolate / relative), str(relative)
        evidence['scripts'][relative.as_posix()] = digest(source)
    for name, relative in [('Assembly-CSharp.dll', 'Assets/Plugins/Assembly-CSharp.dll'),
                           ('Assembly-CSharp-Editor.dll', 'Assets/Editor/Assembly-CSharp-Editor.dll')]:
        assert digest(compiled / name) == digest(isolate / relative), name
        evidence['artifacts'][relative] = digest(compiled / name)
    output.mkdir(exist_ok=True)
    assessment = dict(runtime_checks_passed=True, overall_runner_success=results['success'],
        known_editor_errors=known_editor_errors,
        limitation='Raw runner remains failed when Unity Editor Search startup throws; no result or log was rewritten.')
    (output / 'assessment.json').write_text(json.dumps(assessment, indent=2), encoding='utf-8')
    evidence['artifacts']['assessment.json'] = digest(output / 'assessment.json')
    files = [log, compiled / 'result.json', review / 'results.json',
             isolate / 'Assets/Resources/vn_flow_cases.json'] + sorted(review.glob('*.png'))
    for source in files:
        shutil.copy2(source, output / source.name)
        evidence['artifacts'][source.name] = digest(source)
    (output / 'evidence.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
    print('VN_FULL_FLOW_EVIDENCE_OK', len(evidence['sources']), 'sources; 10 scripts; 25 options;',
          'overall_runner_success=' + str(results['success']), 'known_editor_errors=' + str(len(known_editor_errors)))


if __name__ == '__main__':
    main()
