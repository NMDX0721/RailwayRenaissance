"""Verify archive digests, repeat determinism and optional exact rerun fingerprints."""
import hashlib,json,pathlib,sys,zipfile

def load(root):
    protocol=json.loads((root/'protocol.json').read_text(encoding='utf-8'))
    assert (root/'COMPLETE.txt').exists(), 'incomplete run'
    with zipfile.ZipFile(root/'frozen_sources.zip') as archive:
        for name,expected in protocol['hashes'].items():
            assert hashlib.sha256(archive.read(name)).hexdigest().upper()==expected,name
    rows=[]
    for variant in protocol['variants']:
        rows.extend(json.loads((root/(variant+'-results.json')).read_text(encoding='utf-8')))
    expected=len(protocol['seeds'])*len(protocol['scenarios'])*len(protocol['modes'])*len(protocol['variants'])*protocol['repeats']
    assert len(rows)==expected,(len(rows),expected)
    index={row['id']:row for row in rows};assert len(index)==len(rows)
    groups={}
    for row in rows:groups.setdefault((row['seed'],row['scenario'],row['mode'],row['variant']),[]).append(row)
    for key,group in groups.items():
        assert len(group)==protocol['repeats'] and len({x['fingerprint'] for x in group})==1,key
    return protocol,index

root=pathlib.Path(sys.argv[1]);p,rows=load(root)
print('VERIFIED',root,'rows',len(rows),'repeat pairs',len(rows)//p['repeats'])
if len(sys.argv)>2:
    q,other=load(pathlib.Path(sys.argv[2]));assert p==q,'different protocols'
    assert {k:v['fingerprint'] for k,v in rows.items()}=={k:v['fingerprint'] for k,v in other.items()},'rerun mismatch'
    print('RERUN_FINGERPRINTS_IDENTICAL',len(rows))
