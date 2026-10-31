"""Audit rendered online timing evidence; CSS durations are not native-export endpoints."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]


def validate_comparison(phonetic, track, audio, sheet):
    for key, expected in [('id', track), ('audioSha256', hashlib.sha256(audio.read_bytes()).hexdigest()),
                          ('sourceSha256', hashlib.sha256(sheet.read_bytes()).hexdigest())]:
        if phonetic.get(key) != expected:
            raise ValueError(f'Stale or mismatched local comparison: {key}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--id', choices=['starlit_rails', 'chollima_ride'], default='starlit_rails')
    args = parser.parse_args()
    folder = ROOT / 'tools/music/alignment_review/lyricsmith'
    track = args.id
    raw = folder / f'{track}.visible-words.json'
    report = json.loads(raw.read_text(encoding='utf-8'))
    sheet = ROOT / f'Assets/Resources/Music/LyricSheets/{track}.json'
    authored = json.loads(sheet.read_text(encoding='utf-8'))['plainText'].splitlines()
    audio = ROOT / f'Assets/Resources/bgm/{track}.mp3'
    duration = float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(audio)]))
    if len(authored) != len(report['lines']):
        raise ValueError('Source line count mismatch')
    comparison_name = ('starlit_rails.local.sections.provider.boundaries.json' if track == 'starlit_rails'
                       else 'chollima_ride.spoken.local.sections.allsections.provider.boundaries.json')
    phonetic_path = ROOT / 'tools/music/alignment_review/phonetic' / comparison_name
    phonetic = json.loads(phonetic_path.read_text(encoding='utf-8'))
    validate_comparison(phonetic, track, audio, sheet)
    if any(len(r['lines']) != len(authored) for r in phonetic['rounds']):
        raise ValueError('Local comparison line count mismatch')
    evidence, previous, word_count = [], -1., 0
    for index, (text, line) in enumerate(zip(authored, report['lines'])):
        if line['text'] != text:
            raise ValueError(f'Source lyric changed at {index+1}')
        words = line['words']
        if not words or ''.join(w['text'] for w in words) != re.sub(r'\s+', '', text):
            raise ValueError(f'Word text coverage mismatch at {index+1}')
        flags = ['rendered_duration_not_native_export']
        if any(not all(math.isfinite(w[k]) for k in ['start','duration']) or w['start']<0 or w['duration']<=0 or w['start']+w['duration']>duration for w in words):
            flags.append('invalid_rendered_word_interval')
        if any(b['start'] < a['start'] for a,b in zip(words,words[1:])):
            flags.append('reversed_word_starts')
        start = words[0]['start']
        if start < previous:
            flags.append('reversed_line_start')
        previous = start
        comparison = [dict(role=r['role'],start=r['lines'][index]['start'],end=r['lines'][index]['end'],startDifference=round(start-r['lines'][index]['start'],3)) for r in phonetic['rounds']]
        if min(abs(c['startDifference']) for c in comparison)>.25:
            flags.append('no_local_start_agreement_within_250ms')
        evidence.append(dict(line=index+1,text=text,start=start,end=max(w['start']+w['duration'] for w in words),words=words,flags=flags,localComparison=comparison))
        word_count += len(words)
    output = dict(id=track,model='LyricSmith rendered UI evidence; backend unspecified',reviewFile=raw.name,
                  status='automatic_online_candidate_pending_native_export_check',humanListeningVerified=False,
                  nativeExportObtained=False,wordDurationIsPresentationData=True,
                  audioSha256=hashlib.sha256(audio.read_bytes()).hexdigest(),sourceSha256=hashlib.sha256(sheet.read_bytes()).hexdigest(),
                  evidenceSha256=hashlib.sha256(raw.read_bytes()).hexdigest(),url=report['url'],duration=duration,
                  lineCount=len(evidence),wordCount=word_count,lines=evidence,
                  issues=[dict(line=l['line'],flags=l['flags']) for l in evidence])
    (folder/f'{track}.ui-candidate.review.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
    doubtful=[l['line'] for l in evidence if 'no_local_start_agreement_within_250ms' in l['flags']]
    print(f'LYRICSMITH_UI_AUDIT_OK: {len(evidence)} source lines; {word_count} rendered words; start disagreements {doubtful}; not published')


if __name__ == '__main__':
    main()
