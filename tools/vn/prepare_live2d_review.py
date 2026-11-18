"""Unpack the official SDK unchanged into a dedicated Unity evaluation project."""
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import tarfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/vn/live2d_integration_20261105'
TARGET=ROOT/'Library/VNLive2DReviewProject'
PACKAGE=OUT/'CubismSdkForUnity-5-r.6.1.unitypackage'
EXPECTED='7e16fda1140b3199b5d231422243b3396d7c5b07b4ce1bb510bacc72246bd2ad'

def main():
    assert hashlib.sha256(PACKAGE.read_bytes()).hexdigest()==EXPECTED
    TARGET.mkdir(parents=True,exist_ok=True)
    receipt=[]
    with tarfile.open(PACKAGE,'r:gz') as archive:
        members={m.name:m for m in archive.getmembers()}
        for m in members.values():
            if not m.name.endswith('/pathname'):continue
            name=archive.extractfile(m).read().decode('utf-8').strip()
            rel=PurePosixPath(name)
            assert not rel.is_absolute() and '..' not in rel.parts and rel.parts[0]=='Assets',name
            path=TARGET.joinpath(*rel.parts)
            assert path.resolve().is_relative_to(TARGET.resolve()),name
            prefix=m.name.rsplit('/',1)[0]
            asset=members.get(prefix+'/asset')
            if asset is not None:
                path.parent.mkdir(parents=True,exist_ok=True)
                raw=archive.extractfile(asset).read()
                if path.exists():assert path.read_bytes()==raw,'Do not overwrite changed SDK '+name
                path.write_bytes(raw)
                receipt.append(dict(path=name,sha256=hashlib.sha256(raw).hexdigest()))
            else:path.mkdir(parents=True,exist_ok=True)
            meta=members.get(prefix+'/asset.meta')
            if meta is not None:Path(str(path)+'.meta').write_bytes(archive.extractfile(meta).read())
    # Use the project's actually cached package versions, not an invented Unity environment.
    cache={}
    for path in (ROOT/'Library/PackageCache').iterdir():
        manifest=path/'package.json'
        if manifest.is_file():
            data=json.loads(manifest.read_text(encoding='utf-8-sig'));cache[data['name']]=(path,data)
    pending=['com.unity.render-pipelines.universal','com.unity.inputsystem','com.unity.collections']
    dependencies={};versions={}
    while pending:
        name=pending.pop()
        if name in dependencies:continue
        if name.startswith('com.unity.modules.'):
            dependencies[name]='1.0.0';continue
        assert name in cache,'Required package not cached: '+name
        path,data=cache[name]
        dependencies[name]='file:'+path.resolve().as_posix()
        versions[name]=data['version']
        pending.extend(data.get('dependencies',{}))
    dependencies['com.unity.modules.audio']='1.0.0'
    dependencies['com.unity.modules.imageconversion']='1.0.0'
    dependencies['com.unity.modules.ui']='1.0.0'
    dependencies['com.unity.modules.uielements']='1.0.0'
    packages=TARGET/'Packages';packages.mkdir(exist_ok=True)
    (packages/'manifest.json').write_text(json.dumps(dict(dependencies=dependencies),indent=2),encoding='utf-8')
    settings=TARGET/'ProjectSettings';settings.mkdir(exist_ok=True)
    shutil.copy2(ROOT/'ProjectSettings/ProjectVersion.txt',settings/'ProjectVersion.txt')
    record=dict(scope='Official SDK and sample models for local internal evaluation only; not game-character art or release acceptance',
        sdk='Cubism 5 SDK for Unity R6_1',downloadUrl='https://cubism.live2d.com/sdk-unity/bin/CubismSdkForUnity-5-r.6.1.unitypackage',
        packageSha256=EXPECTED,target=str(TARGET),resolvedPackages=versions,files=receipt)
    (OUT/'sdk_import.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
    print('LIVE2D_REVIEW_PREPARED files='+str(len(receipt))+' URP='+versions['com.unity.render-pipelines.universal'])

if __name__=='__main__':main()
