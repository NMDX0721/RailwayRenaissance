"""Archive a native export and compare it to the authorized recording and source."""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'tools/music/alignment_review/lyrictimestamps'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def audit(words, source, duration, rounds, require_confidence=True):
    grouped = collections.defaultdict(list)
    previous = -1
    for word in words:
        index = word['line_index']
        if type(index) is not int or not 0 <= index < len(source):
            raise ValueError('Invalid source line index')
        start, end = (word[k] for k in ('start', 'end'))
        confidence = word.get('confidence')
        if any(type(v) not in (int, float) or not math.isfinite(v) for v in (start, end)):
            raise ValueError('Non-finite or non-numeric timing/confidence')
        if (confidence is None and require_confidence or confidence is not None and
                (type(confidence) not in (int, float) or not math.isfinite(confidence) or not 0 <= confidence <= 1)):
            raise ValueError('Missing or invalid confidence')
        if not 0 <= start < end <= duration:
            raise ValueError('Invalid interval/confidence')
        if start < previous - .001:
            raise ValueError('Reversed or overlapping word intervals')
        previous = end
        grouped[index].append(word)
    if set(grouped) != set(range(len(source))):
        raise ValueError('Missing source line')
    result = []
    for index, text in enumerate(source):
        tokens = grouped[index]
        if ''.join(w['word'] for w in tokens) != ''.join(text.split()):
            raise ValueError(f'Source text mismatch on line {index + 1}')
        start, end = tokens[0]['start'], tokens[-1]['end']
        comparison = []
        for run in rounds:
            local = run['lines'][index]
            comparison.append(dict(role=run['role'], start=local['start'], end=local['end'],
                                   startDifference=round(start-local['start'], 3),
                                   endDifference=round(end-local['end'], 3)))
        flags = []
        if min(abs(c['startDifference']) for c in comparison) > .25:
            flags.append('no_local_start_agreement_within_250ms')
        if min(abs(c['endDifference']) for c in comparison) > .35:
            flags.append('no_local_end_agreement_within_350ms')
        if any(w.get('confidence') is None for w in tokens):
            flags.append('provider_confidence_not_visible')
        if any(w.get('confidence') is not None and w['confidence'] < .5 for w in tokens):
            flags.append('provider_confidence_below_half')
        result.append(dict(line=index+1, text=text, start=start, end=end, words=tokens,
                           flags=flags, localComparison=comparison))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('export', type=Path)
    parser.add_argument('--id', choices=['chollima_ride', 'starlit_rails'], default='chollima_ride')
    args = parser.parse_args()
    track = args.id
    submission_path = FOLDER / f'{track}.submission.json'
    submission = json.loads(submission_path.read_text(encoding='utf-8-sig'))
    audio = ROOT / f'Assets/Resources/bgm/{track}.mp3'
    lyrics = ROOT / f'tools/music/alignment_review/online_input/{track}.lyrics.txt'
    sheet = ROOT / f'Assets/Resources/Music/LyricSheets/{track}.json'
    variant = 'spoken.local.sections.allsections' if track == 'chollima_ride' else 'local.sections'
    local_path = ROOT / f'tools/music/alignment_review/phonetic/{track}.{variant}.provider.boundaries.json'
    local = json.loads(local_path.read_text(encoding='utf-8'))
    if submission['authorizedTracks'] != [track] or submission['audioSha256'] != digest(audio) or submission['lyricsSha256'] != digest(lyrics):
        raise ValueError('Submission identity changed')
    if local['audioSha256'] != digest(audio) or local['sourceSha256'] != digest(sheet):
        raise ValueError('Stale local comparison')
    source = lyrics.read_text(encoding='utf-8-sig').strip().splitlines()
    if source != json.loads(sheet.read_text(encoding='utf-8'))['plainText'].splitlines():
        raise ValueError('Submitted lyrics differ from current source')
    duration = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', str(audio)]))
    data = json.loads(args.export.read_text(encoding='utf-8-sig'))
    lines = audit(data, source, duration, local['rounds'])
    archive = FOLDER / 'native' / args.export.name
    archive.parent.mkdir(exist_ok=True)
    if archive.exists() and digest(archive) != digest(args.export):
        raise ValueError('Refusing to overwrite a different archived export')
    shutil.copyfile(args.export, archive)
    output = dict(id=track, nativeExportObtained=True, published=False,
                  humanListeningVerified=False, automaticCandidateOnly=True,
                  providerConfidenceIsNotAccuracy=True, independentBackendVerified=False,
                  duration=duration, audioSha256=digest(audio), sourceSha256=digest(sheet),
                  exportSha256=digest(archive), exportPath=str(archive.relative_to(ROOT)),
                  comparisonSha256=digest(local_path), toolSha256=digest(Path(__file__)),
                  lineCount=len(lines), wordCount=len(data), lines=lines)
    (FOLDER / f'{track}.native-audit.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf-8')
    submission['nativeExportObtained'] = True
    submission['exportSha256'] = digest(archive)
    submission['observedState'] = 'Native export archived and structurally audited; timing conflicts remain; not published'
    submission_path.write_text(json.dumps(submission, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'NATIVE_AUDIT_OK: {len(lines)} lines, {len(data)} words; not published')
    for line in lines:
        if line['flags']:
            print(line['line'], ','.join(line['flags']))


if __name__ == '__main__':
    main()
