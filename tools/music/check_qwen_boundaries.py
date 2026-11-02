"""Independent local Qwen timing evidence. Does not publish or rewrite lyric sheets."""
import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import subprocess
import unicodedata

ROOT = Path(__file__).resolve().parents[2]
VARIANTS = {
    'starlit_rails': 'local.sections.provider.boundaries',
    'chollima_ride': 'spoken.local.sections.allsections.provider.boundaries',
}


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def lexical(text):
    return ''.join(c for c in unicodedata.normalize('NFKC', text).lower() if c.isalnum())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('id', choices=list(VARIANTS))
    parser.add_argument('--lines', type=int, nargs='+', required=True)
    parser.add_argument('--roles', nargs='+', choices=['original', 'vocals'], default=['original', 'vocals'])
    parser.add_argument('--single-line', action='store_true', help='Separate target-only context ablation; preserve default reports')
    args = parser.parse_args()
    import numpy as np
    import torch
    from qwen_asr import Qwen3ForcedAligner
    torch.set_num_threads(8)
    model_dir = ROOT / 'Library/MusicAlignment/models/qwen3-aligner'
    verification = json.loads((model_dir / 'verification.json').read_text())
    for entry in verification['files']:
        if digest(model_dir / entry['file']) != entry['sha256']:
            raise ValueError('Model file changed: ' + entry['file'])
    model = Qwen3ForcedAligner.from_pretrained(str(model_dir), dtype=torch.float32, device_map='cpu', local_files_only=True)
    # Preserve upstream repair diagnostics: inferred order repairs are not measured timings.
    raw_fix = model.aligner_processor.fix_timestamp
    repairs = []
    def capture_fix(timestamps):
        fixed = raw_fix(timestamps)
        repairs.append(dict(raw=timestamps.tolist(), fixed=fixed, changed=[i for i, (a,b) in enumerate(zip(timestamps.tolist(), fixed)) if abs(a-b)>.01]))
        return fixed
    model.aligner_processor.fix_timestamp = capture_fix
    folder = ROOT / 'tools/music/alignment_review'
    parent = folder / f'phonetic/{args.id}.{VARIANTS[args.id]}.json'
    reference = json.loads(parent.read_text(encoding='utf-8'))
    source = ROOT / f'Assets/Resources/Music/LyricSheets/{args.id}.json'
    sheet = json.loads(source.read_text(encoding='utf-8'))
    audio = ROOT / f'Assets/Resources/bgm/{args.id}.mp3'
    if digest(source) != reference['sourceSha256'] or digest(audio) != reference['audioSha256']:
        raise ValueError('Stale source evidence')
    authored = sheet['plainText'].splitlines()
    rounds = {r['role']: r['lines'] for r in reference['rounds']}
    if any([line['text'] for line in lines] != authored for lines in rounds.values()):
        raise ValueError('Altered source words')
    target = folder / f'qwen/{args.id}{".singleline" if args.single_line else ""}.boundaries.json'
    target.parent.mkdir(exist_ok=True)
    result = dict(id=args.id, model=verification['repo'], revision=verification['revision'],
                  toolSha256=digest(Path(__file__)), packageVersion=importlib.metadata.version('qwen-asr'),
                  audioSha256=digest(audio), sourceSha256=digest(source), parentSha256=digest(parent),
                  automaticEvidenceOnly=True, humanListeningVerified=False, singleLine=args.single_line, windows=[])
    for role in args.roles:
        waveform = audio if role == 'original' else ROOT / f'Library/MusicAlignment/vocals/{args.id}.wav'
        if role == 'vocals':
            meta = json.loads(waveform.with_suffix('.json').read_text())
            if meta['originalAudioSha256'] != digest(audio) or meta['derivedAudioSha256'] != digest(waveform) or meta['timeOffset'] != 0:
                raise ValueError('Invalid vocal source')
        pcm = np.frombuffer(subprocess.check_output(['ffmpeg','-v','error','-i',str(waveform),'-ac','1','-ar','16000','-f','f32le','pipe:1']),dtype='<f4').copy()
        duration = len(pcm)/16000
        for number in args.lines:
            if number<1 or number>len(authored):
                raise ValueError('Invalid line')
            first,last = max(0,number-2),min(len(authored),number+1)
            if args.single_line: first,last=number-1,number
            texts = authored[first:last]
            alignment_texts = [t.replace('100','백') for t in texts]
            # Include neighbouring source lines to constrain repeated short phrases.
            start = min(rounds[r][first]['start'] for r in rounds)
            end = max(rounds[r][last-1]['end'] for r in rounds)
            for pad in [1.,3.]:
                a,b = max(0,start-pad),min(duration,end+pad)
                repairs.clear()
                aligned = model.align(audio=(pcm[round(a*16000):round(b*16000)],16000),text=' '.join(alignment_texts),language='Korean')[0]
                items = [dict(text=i.text,start=round(a+i.start_time,3),end=round(a+i.end_time,3)) for i in aligned]
                expected = ''.join(map(lexical,alignment_texts))
                actual = ''.join(lexical(i['text']) for i in items)
                spans,offset = [],0
                for index,text in enumerate(alignment_texts):
                    line_end = offset+len(lexical(text));position=0;matched=[];crossing=False
                    for item in items:
                        following=position+len(lexical(item['text']))
                        if position<line_end and following>offset:
                            matched.append(item)
                            crossing |= position<offset or following>line_end
                        position=following
                    spans.append(dict(line=first+index+1,text=texts[index],alignmentInput=text,start=min((i['start'] for i in matched),default=None),end=max((i['end'] for i in matched),default=None),tokenCrossesSourceLine=crossing))
                    offset=line_end
                issues=[]
                if actual!=expected: issues.append('token_text_mismatch')
                if any(r['changed'] for r in repairs): issues.append('upstream_timestamp_repair')
                if any(i['end']<=i['start'] or i['start']<a or i['end']>b for i in items): issues.append('invalid_token_interval')
                if any(s['tokenCrossesSourceLine'] for s in spans): issues.append('token_crosses_source_line')
                result['windows'].append(dict(targetLine=number,role=role,waveformSha256=digest(waveform),start=a,end=b,padding=pad,items=items,lines=spans,repairs=list(repairs),issues=issues))
                target.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
                print(f'{args.id} line {number} {role} pad {pad}: {issues or "raw timestamps retained"}',flush=True)
    print(target,flush=True)


if __name__ == '__main__':
    main()
