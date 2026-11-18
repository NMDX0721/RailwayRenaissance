"""Audit real model handoff files; never treats a PNG or an empty placeholder as a rig."""
import argparse
import json
from pathlib import Path, PurePosixPath

ROOT=Path(__file__).resolve().parents[2]
CATALOGUE=ROOT/'Assets/Resources/VNPortraits/catalogue.json'

def project_path(value):
    if not isinstance(value,str) or not value or '\\' in value or ':' in value:
        raise ValueError('Expected a project-relative forward-slash path: '+str(value))
    part=PurePosixPath(value)
    if part.is_absolute() or '..' in part.parts:
        raise ValueError('Path escapes the project: '+value)
    return ROOT.joinpath(*part.parts)

def model_file(folder,value):
    if not isinstance(value,str) or not value or '\\' in value or ':' in value:
        raise ValueError('Invalid model file reference: '+str(value))
    part=PurePosixPath(value)
    if part.is_absolute() or '..' in part.parts:
        raise ValueError('Model reference escapes its folder: '+value)
    path=folder.joinpath(*part.parts)
    return path.is_file() and path.stat().st_size>0

def validate_model(path,profile,used):
    result={'present':path.is_file(),'runtimeFilesComplete':False,'missing':[]}
    if not path.is_file():
        result['missing'].append(profile['modelAsset'])
        return result
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    refs=data.get('FileReferences')
    if not isinstance(refs,dict):raise ValueError(str(path)+': missing FileReferences')
    check=[refs.get('Moc','')]
    textures=refs.get('Textures',[])
    if not textures:result['missing'].append('FileReferences.Textures')
    check.extend(textures)
    if refs.get('Physics'):check.append(refs['Physics'])
    if refs.get('Pose'):check.append(refs['Pose'])
    expressions={item['Name'].lower():item['File'] for item in refs.get('Expressions',[])}
    aliases={e['script']:e['asset'] for e in profile['expressions']}
    required=set(profile['minimumExpressions'])|{aliases[name] for name in used if name in aliases}
    for name in sorted(required):
        if name.lower() not in expressions:result['missing'].append('expression:'+name)
    check.extend(expressions.values())
    motions=refs.get('Motions',{})
    for group in profile['requiredMotions']:
        if not any(k.lower()==group.lower() and v for k,v in motions.items()):
            result['missing'].append('motion group:'+group)
    for group in motions.values():
        for motion in group:
            check.append(motion['File'])
            if motion.get('Sound'):check.append(motion['Sound'])
    for value in check:
        if not model_file(path.parent,value):result['missing'].append(value)
    result['runtimeFilesComplete']=not result['missing']
    result['parameterInspection']='Requires Cubism import and parameter/part review; binary moc3 not inferred from filenames'
    return result

def audit():
    catalogue=json.loads(CATALOGUE.read_text(encoding='utf-8'))
    if catalogue.get('schemaVersion')!=1:raise ValueError('Unsupported catalogue schema')
    cubism=ROOT/'Assets/Live2D/Cubism'
    files=list(cubism.rglob('*')) if cubism.exists() else []
    sdk={'wrapperPresent':(cubism/'Core/CubismModel.cs').is_file(),
         'windowsNativePresent':any(p.name.lower()=='live2dcubismcore.dll' for p in files),
         'androidNativePresent':any(p.name.lower()=='liblive2dcubismcore.so' for p in files)}
    package=json.loads((ROOT/'Packages/manifest.json').read_text(encoding='utf-8'))['dependencies']
    sdk['urpPackage']=package.get('com.unity.render-pipelines.universal')
    sdk['inputSystemPackage']=package.get('com.unity.inputsystem')
    results=[];ids=set()
    for profile in catalogue['characters']:
        char_id=profile['id']
        if char_id in ids:raise ValueError('Duplicate character '+char_id)
        ids.add(char_id)
        aliases={e['script']:e['asset'] for e in profile['expressions']}
        if len(aliases)!=len(profile['expressions']):raise ValueError('Duplicate expression '+char_id)
        fallback=project_path('Assets/Resources/'+profile['staticFallback']+'.png')
        if not fallback.is_file():raise ValueError('Missing static fallback '+str(fallback))
        if profile.get('staticFallbackBeforeTablet'):
            path=project_path('Assets/Resources/'+profile['staticFallbackBeforeTablet']+'.png')
            if not path.is_file():raise ValueError('Missing no-tablet fallback')
        used=set()
        for path in (ROOT/'Assets/Resources/Scripts').glob('prologue_*.json'):
            for scene in json.loads(path.read_text(encoding='utf-8-sig'))['scenes']:
                for entry in scene['d']:
                    if entry.get('s')==profile['displayName'] and entry.get('e'):used.add(entry['e'])
        model=validate_model(project_path(profile['modelAsset']),profile,used)
        prefab=project_path(profile['prefabAsset'])
        results.append(dict(id=char_id,enabled=profile['enabled'],
            authoringPsdPresent=project_path(profile['sourcePsd']).is_file(),
            editableCmo3Present=project_path(profile['sourceCmo3']).is_file(),
            prefabPresent=prefab.is_file() and prefab.stat().st_size>0,
            missingExpressionMappings=sorted(used-set(aliases)),
            model=model,staticFallbackPresent=True,
            note='A disabled profile does not instantiate or replace a runtime character'))
    return dict(scope='Local asset preflight only; not SDK import, model quality, Unity Play or device acceptance',
        sdk=sdk,characters=results,
        assetsReadyForImport=all(sdk[k] for k in ['wrapperPresent','windowsNativePresent','androidNativePresent']) and
            all(r['model']['runtimeFilesComplete'] and r['prefabPresent'] and not r['missingExpressionMappings'] for r in results),
        runtimeModelAcceptance='not performed',
        mainAcceptanceResolution=[2560,1440])

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--output',default='tools/vn/live2d_preparation_20261105/readiness.json')
    parser.add_argument('--require-assets',action='store_true');args=parser.parse_args()
    result=audit();path=project_path(args.output);path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('LIVE2D_PREFLIGHT_VALID assetsReadyForImport='+str(result['assetsReadyForImport']).lower())
    for char in result['characters']:
        print(char['id'],'model='+str(char['model']['present']).lower(),
              'missingExpressionMappings='+str(char['missingExpressionMappings']))
    if args.require_assets and not result['assetsReadyForImport']:raise SystemExit(1)

if __name__=='__main__':main()
