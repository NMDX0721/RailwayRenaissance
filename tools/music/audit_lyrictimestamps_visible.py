"""Check directly rendered word identities and timings without claiming native export."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
from audit_lyrictimestamps import ROOT, FOLDER, audit, digest


def main():
    track = 'starlit_rails'
    evidence_path = FOLDER/f'{track}.visible-comparison.json'
    evidence = json.loads(evidence_path.read_text(encoding='utf-8'))
    submission = json.loads((FOLDER/f'{track}.submission.json').read_text(encoding='utf-8-sig'))
    audio = ROOT/f'Assets/Resources/bgm/{track}.mp3'
    source_path = ROOT/f'tools/music/alignment_review/online_input/{track}.lyrics.txt'
    sheet = ROOT/f'Assets/Resources/Music/LyricSheets/{track}.json'
    local_path = ROOT/f'tools/music/alignment_review/phonetic/{track}.local.sections.provider.boundaries.json'
    local = json.loads(local_path.read_text(encoding='utf-8'))
    source = source_path.read_text(encoding='utf-8').strip().splitlines()
    if (submission['authorizedTracks'] != [track] or submission['audioSha256'] != digest(audio)
            or submission['lyricsSha256'] != digest(source_path) or local['audioSha256'] != digest(audio)
            or local['sourceSha256'] != digest(sheet)
            or source != json.loads(sheet.read_text(encoding='utf-8'))['plainText'].splitlines()):
        raise ValueError('Recording or source identity changed')
    expected = [(index, text, token) for index,text in enumerate(source) for token in text.split()]
    if len(expected) != len(evidence['words']):
        raise ValueError('Word count mismatch')
    variants = {name: [] for name in ['标准', '行引导', '字对齐', '共识']}
    for number, (visible, (index,text,token)) in enumerate(zip(evidence['words'],expected),1):
        counter = re.search(r'第\s*(\d+)\s*个词，共\s*(\d+)\s*个',visible['visibleText'])
        if (not counter or (int(counter[1]),int(counter[2])) != (number,len(expected))
                or visible['word'] != token or visible['context'].split() != text.split()):
            raise ValueError(f'Rendered word/line identity mismatch at {number}')
        found = set()
        for label in visible['options']:
            match = re.fullmatch(r'(.+): 播放此部分 (\d+):(\d+\.\d+) (\d+):(\d+\.\d+)',label)
            if not match or match[1] not in variants or match[1] in found:
                raise ValueError('Missing or duplicate displayed timing variant')
            found.add(match[1])
            variants[match[1]].append(dict(word=token,line_index=index,
                start=int(match[2])*60+float(match[3]),end=int(match[4])*60+float(match[5])))
        if found != set(variants):
            raise ValueError('Incomplete variant comparison')
    duration = float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(audio)]))
    results = {}
    for name, words in variants.items():
        try:
            lines = audit(words, source, duration, local['rounds'], require_confidence=False)
            results[name] = dict(lines=lines, validIntervals=True,
                startConflicts=[l['line'] for l in lines if 'no_local_start_agreement_within_250ms' in l['flags']],
                endConflicts=[l['line'] for l in lines if 'no_local_end_agreement_within_350ms' in l['flags']])
            print(name.encode('unicode_escape').decode(), 'start conflicts', results[name]['startConflicts'])
        except ValueError as error:
            results[name] = dict(validIntervals=False,error=str(error),words=words)
    output = dict(id=track,published=False,humanListeningVerified=False,nativeExportObtained=False,
                  displayedPrecisionSeconds=.01,providerConfidenceAvailable=False,
                  audioSha256=digest(audio),sourceSha256=digest(sheet),evidenceSha256=digest(evidence_path),
                  comparisonSha256=digest(local_path),toolSha256=digest(Path(__file__)),wordCount=len(expected),variants=results)
    (FOLDER/f'{track}.visible-audit.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')


if __name__ == '__main__':
    main()
