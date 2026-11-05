"""Prepare an isolated Unity renderer check using the freshly compiled source."""
import json
import pathlib
import shutil
import subprocess
import argparse

ROOT = pathlib.Path(__file__).resolve().parents[2]
TARGET = ROOT / 'Library/VNCompiledReviewProject'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--compiled', default='tools/vn/source_compile_review')
    args = parser.parse_args()
    compiled = ROOT / args.compiled
    result = json.loads((compiled / 'result.json').read_text())
    assert result['runtime']['exit_code'] == result['vn_review_editor']['exit_code'] == 0
    plugins = TARGET / 'Assets/Plugins'
    editor = TARGET / 'Assets/Editor'
    plugins.mkdir(parents=True, exist_ok=True)
    editor.mkdir(parents=True, exist_ok=True)
    for name, folder in [('Assembly-CSharp.dll', plugins), ('Assembly-CSharp-Editor.dll', editor)]:
        shutil.copy2(compiled / name, folder / name)
    references = []
    needed = {'Assembly-CSharp.dll'}
    pending = [plugins / 'Assembly-CSharp.dll', editor / 'Assembly-CSharp-Editor.dll']
    while pending:
        assembly = pending.pop()
        quoted_path = str(assembly).replace("'", "''")
        command = "[Reflection.Assembly]::LoadFile('" + quoted_path + "').GetReferencedAssemblies() | ForEach-Object { $_.Name } | ConvertTo-Json -Compress"
        metadata = subprocess.run(['C:/Users/Oe_Lee/AppData/Local/Programs/PowerShell/7/pwsh.exe',
            '-NoProfile', '-Command', command], capture_output=True, text=True, check=True)
        names = json.loads(metadata.stdout)
        for name in ([names] if isinstance(names, str) else names):
            implementation = ROOT / 'Library/ScriptAssemblies' / (name + '.dll')
            if implementation.exists() and implementation.name not in needed:
                needed.add(implementation.name)
                shutil.copy2(implementation, plugins / implementation.name)
                references.append(str(implementation))
                pending.append(plugins / implementation.name)
    # Remove only stale dependencies from this dedicated generated review directory.
    for unused in plugins.glob('*.dll'):
        if unused.name not in needed:
            assert unused.resolve().parent == plugins.resolve()
            unused.unlink()
    for part in ['bg', 'cg', 'characters', 'Fonts', 'Scripts', 'UI', 'Localization']:
        shutil.copytree(ROOT / 'Assets/Resources' / part, TARGET / 'Assets/Resources' / part, dirs_exist_ok=True)
    if (ROOT / 'Assets/UI Toolkit').exists():
        shutil.copytree(ROOT / 'Assets/UI Toolkit', TARGET / 'Assets/UI Toolkit', dirs_exist_ok=True)
    (TARGET / 'Packages').mkdir(exist_ok=True)
    (TARGET / 'Packages/manifest.json').write_text('{"dependencies":{}}')
    settings = TARGET / 'ProjectSettings'
    settings.mkdir(exist_ok=True)
    shutil.copy2(ROOT / 'ProjectSettings/ProjectVersion.txt', settings / 'ProjectVersion.txt')
    (TARGET / 'source-evidence.json').write_text(json.dumps(dict(source_compile=result,
        package_assembly_sources=references, scope='Isolated compiled rendering check; not full-game/release acceptance'), indent=2))
    print(TARGET)


if __name__ == '__main__':
    main()
