"""Verify the frozen V2/V3 records against their archived command files.

The digest format follows FrozenV3.cs: UTF-8(finalState + LF-joined command
records), without the CSV header or trailing newline. This checks recorded
content, not whether a new run would reproduce it on another machine.
"""
import collections
import hashlib
import json
import pathlib
import sys
import zipfile

root = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path('research/ato-v3/runs/20261028T121643698Z')
protocol = json.loads((root / 'protocol.json').read_text(encoding='utf-8-sig'))
rows = json.loads((root / 'results.json').read_text(encoding='utf-8-sig'))
assert (root / 'COMPLETE.txt').exists(), 'run is incomplete'
expected = len(protocol['seeds']) * len(protocol['scenarios']) * 3 * 2 * protocol['repeats']
assert len(rows) == expected
with zipfile.ZipFile(root / 'frozen_sources.zip') as archive:
    for name, digest in protocol['hashes'].items():
        assert hashlib.sha256(archive.read(name)).hexdigest().upper() == digest, name

seen = set()
groups = collections.defaultdict(list)
for row in rows:
    ident = row['id']
    assert ident not in seen, ident
    seen.add(ident)
    initial = json.loads((root / (ident + '-initial.json')).read_text(encoding='utf-8-sig'))
    assert all(initial.get(k) for k in ('config', 'state', 'input', 'task')), ident
    lines = (root / (ident + '-commands.csv')).read_text(encoding='utf-8-sig').splitlines()
    assert lines and lines[0] == 'tick,requestedThrottle,requestedBrake,acceptedThrottle,acceptedBrake', ident
    digest = hashlib.sha256((row['finalState'] + '\n'.join(lines[1:])).encode('utf-8')).hexdigest().upper()
    assert digest == row['fingerprint'], ident
    success = (not row['intervention'] and not row['timeout'] and row['violationTicks'] == 0
               and abs(row['error']) <= 5 and 115 <= row['arrival'] <= 125)
    assert row['success'] == success, ident
    groups[(row['seed'], row['scenario'], row['mode'], row['controller'])].append(row)
for key, group in groups.items():
    assert len(group) == protocol['repeats'] and len({x['fingerprint'] for x in group}) == 1, key
print(f'PRIMARY_EVIDENCE_OK: {len(rows)} source records; {len(groups)} repeat pairs; commands and final states rehashed')
