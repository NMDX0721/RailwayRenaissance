"""Compile current source with Unity's cached references; this is not a Play test."""
import hashlib
import argparse
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'tools/vn/source_compile_review'
UNITY = pathlib.Path('D:/Unity Hub/Unity 6.5/Editor/Data')
EDITOR_SOURCES = ['Assets/Editor/VNAssetCompletionReview.cs']


def compile_one(name, editor=False):
    source_rsp = ROOT / 'Library/Bee/artifacts/1900b0aE.dag' / (name + '.rsp')
    lines = source_rsp.read_text(encoding='utf-8-sig').splitlines()
    revised = []
    sources = {}
    for line in lines:
        if line.startswith(('-out:', '-refout:', '/out:', '/refout:')):
            option = line.split(':', 1)[0]
            suffix = '.ref.dll' if 'refout' in option else '.dll'
            revised.append(f'{option}:"{(OUTPUT / (name + suffix)).as_posix()}"')
            continue
        if line.strip('"').endswith('.cs'):
            if editor:
                continue
            path = ROOT / line.strip('"')
            if not path.exists():
                if path.relative_to(ROOT).as_posix() in {'Assets/Scripts/TitleArchiveUI.cs', 'Assets/Scripts/MainStoryUI.cs'}:
                    continue
                raise FileNotFoundError(path)
            sources[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
        if editor and ('/Assembly-CSharp.dll"' in line or '/Assembly-CSharp.ref.dll"' in line):
            line = line.split(':', 1)[0] + ':"' + (OUTPUT / 'Assembly-CSharp.dll').as_posix() + '"'
        revised.append(line)
    if editor:
        for source in EDITOR_SOURCES:
            path = ROOT / source
            revised.append('"' + path.as_posix() + '"')
            sources[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    else:
        extra = list((ROOT / 'Assets/Scripts').rglob('*.cs'))
        for path in extra:
            source = path.relative_to(ROOT).as_posix()
            if path.exists() and source not in sources:
                revised.append('"' + path.as_posix() + '"')
                sources[source] = hashlib.sha256(path.read_bytes()).hexdigest()
    response = OUTPUT / (name + '.rsp')
    response.write_text('\n'.join(revised), encoding='utf-8')
    csc = next((UNITY / 'DotNetSdk/sdk').glob('*/Roslyn/bincore/csc.dll'))
    result = subprocess.run([str(UNITY / 'NetCoreRuntime/dotnet.exe'), str(csc), '@' + str(response)],
                            cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
    (OUTPUT / (name + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(name, 'exit=', result.returncode, 'sources=', len(sources))
    return dict(exit_code=result.returncode, source_sha256=sources, log=(OUTPUT / (name + '.log')).relative_to(ROOT).as_posix())


def main():
    global OUTPUT, EDITOR_SOURCES
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', default='tools/vn/source_compile_review')
    parser.add_argument('--editor-source', action='append')
    args = parser.parse_args()
    OUTPUT = ROOT / args.output
    if args.editor_source:
        EDITOR_SOURCES = args.editor_source
    OUTPUT.mkdir(parents=True, exist_ok=True)
    runtime = compile_one('Assembly-CSharp')
    editor = compile_one('Assembly-CSharp-Editor', editor=True) if runtime['exit_code'] == 0 else None
    report = dict(scope='Roslyn source compilation with cached Unity references, not Unity import/Play acceptance',
                  runtime=runtime, vn_review_editor=editor)
    (OUTPUT / 'result.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    if runtime['exit_code'] or editor is None or editor['exit_code']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
