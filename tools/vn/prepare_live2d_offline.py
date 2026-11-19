"""Prepare an isolated offline evaluation fallback; never modify main packages.

Run prepare_live2d_review.py first on a fresh target. This workaround is NOT a
normal UPM import acceptance. Package test sources are archived outside Assets.
"""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / 'Library/VNLive2DReviewProject'
OUT = ROOT / 'tools/vn/live2d_integration_20261105'


def main():
    cache = {}
    for path in (ROOT / 'Library/PackageCache').iterdir():
        manifest = path / 'package.json'
        if manifest.exists():
            data = json.loads(manifest.read_text(encoding='utf-8-sig'))
            cache[data['name']] = (path, data)
    pending = ['com.unity.render-pipelines.universal', 'com.unity.inputsystem',
               'com.unity.collections', 'com.unity.ugui']
    selected = {}
    while pending:
        name = pending.pop()
        if name in selected or name.startswith('com.unity.modules.'):
            continue
        source, data = cache[name]
        selected[name] = (source, data['version'])
        pending.extend(data.get('dependencies', {}))
    assets = TARGET / 'Assets/OfflinePackages'
    archived = TARGET / 'ExcludedPackageTests'
    for name, (source, version) in selected.items():
        # Assets registers asmdefs without UPM; Packages preserves shader includes.
        shutil.copytree(source, assets / name, dirs_exist_ok=True)
        shutil.copytree(source, TARGET / 'Packages' / name, dirs_exist_ok=True)
    moved = []
    for path in sorted(assets.rglob('*'), key=lambda p: len(p.parts)):
        if not path.is_dir() or not path.exists():
            continue
        if 'test' not in path.name.lower() and path.name != 'Unity.Collections.BurstCompatibilityGen':
            continue
        if path.parent == assets:
            continue
        destination = archived / path.relative_to(assets)
        assert path.resolve().is_relative_to(TARGET.resolve())
        assert destination.resolve().is_relative_to(TARGET.resolve())
        if destination.exists():
            raise RuntimeError('Use a fresh evaluation target; archive exists: ' + str(destination))
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(path), str(destination))
        meta = Path(str(path) + '.meta')
        if meta.exists():
            shutil.move(str(meta), str(destination) + '.meta')
        moved.append(path.relative_to(TARGET).as_posix())
    patch = TARGET / 'Assets/Live2D/Cubism/Editor/CubismUnityEditorUtility.cs'
    before = patch.read_bytes()
    after = before.replace(b'#if UNITY_6000_6_OR_NEWER', b'#if UNITY_6000_5_OR_NEWER')
    assert before != after, 'Expected official R6.1 compatibility guard'
    patch.write_bytes(after)
    for source, relative in [('VNLive2DModelBridge.cs', 'Assets/Review'),
                             ('VNLive2DBasePose.cs', 'Assets/Review'),
                             ('VNLive2DSampleReview.cs', 'Assets/Editor')]:
        folder = TARGET / relative
        folder.mkdir(parents=True, exist_ok=True)
        shutil.copy2(OUT / 'src' / source, folder / source)
    # Use the existing project's serialized URP resource catalogue; the offline
    # package layout cannot discover resource roots through PackageInfo.
    for suffix in ('', '.meta'):
        shutil.copy2(ROOT / ('Assets/UniversalRenderPipelineGlobalSettings.asset' + suffix),
                     TARGET / ('Assets/Review/ReviewGlobalSettings.asset' + suffix))
    (TARGET / 'Packages/manifest.json').write_text('{"dependencies":{}}', encoding='utf-8')
    receipt = dict(scope='Offline source-copy fallback, isolated project only; not normal UPM acceptance',
                   packages={k: v[1] for k, v in selected.items()}, archivedTests=moved,
                   sdkPatch=dict(path=patch.relative_to(TARGET).as_posix(),
                                 before=hashlib.sha256(before).hexdigest(),
                                 after=hashlib.sha256(after).hexdigest(),
                                 change='Use existing EntityId branch on installed Unity 6000.5'))
    (OUT / 'offline_setup.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print('OFFLINE_LIVE2D_PREPARED', len(selected))


if __name__ == '__main__':
    main()
