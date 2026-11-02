"""Controlled encoding ablation, not a pronunciation claim or publication tool."""
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Library/MusicAlignment/python'))


def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()


def main():
    import torch
    import torchaudio
    torch.set_num_threads(8)
    parent=ROOT/'tools/music/alignment_review/phonetic/starlit_rails.local.sections.provider.boundaries.json'
    reference=json.loads(parent.read_text(encoding='utf-8'))
    if reference['audioSha256']!=digest(ROOT/'Assets/Resources/bgm/starlit_rails.mp3'):raise ValueError('Stale audio')
    if reference['sourceSha256']!=digest(ROOT/'Assets/Resources/Music/LyricSheets/starlit_rails.json'):raise ValueError('Stale source')
    tokenizer=torchaudio.pipelines.MMS_FA.get_tokenizer();aligner=torchaudio.pipelines.MMS_FA.get_aligner()
    result=dict(id='starlit_rails',audioSha256=reference['audioSha256'],sourceSha256=reference['sourceSha256'],parentSha256=digest(parent),toolSha256=digest(Path(__file__)),published=False,humanListeningVerified=False,hypothesis='CTC repeated-consonant target encoding sensitivity; source words unchanged',windows=[])
    for run in reference['rounds']:
        path=ROOT/f'Library/MusicAlignment/mms/starlit_rails.{run["role"]}.pt'
        meta=json.loads(path.with_suffix('.json').read_text())
        waveform=ROOT/('Assets/Resources/bgm/starlit_rails.mp3' if run['role']=='original' else 'Library/MusicAlignment/vocals/starlit_rails.wav')
        if meta['audioSha256']!=digest(waveform) or meta['modelSha256']!=reference['modelSha256']:raise ValueError('Wrong acoustic cache')
        cache=torch.load(path,weights_only=True);emission,times=cache['emission'],cache['times']
        for number in [4,26]:
            target=run['lines'][number-1]
            contexts=[r['lines'][number-1] for r in reference['rounds']]
            for padding in [1.,3.]:
                low=max(0,min(l['start'] for l in contexts)-padding);high=max(l['end'] for l in contexts)+padding
                lo,hi=int(torch.searchsorted(times,low)),int(torch.searchsorted(times,high))
                for variant in ['baseline','collapsed_consonants']:
                    tokens=[w['romanized'] for w in target['words']]
                    if variant=='collapsed_consonants':tokens=[re.sub(r'([klsptj])\1+',r'\1',t) for t in tokens]
                    spans=aligner(emission[lo:hi],tokenizer(tokens));words=[]
                    for source,token,chars in zip(target['words'],tokens,spans):
                        count=sum(c.end-c.start for c in chars)
                        words.append(dict(text=source['text'],input=token,start=float(times[lo+chars[0].start])-.01,end=float(times[lo+chars[-1].end-1])+.01,score=sum(c.score*(c.end-c.start) for c in chars)/max(1,count)))
                    result['windows'].append(dict(line=number,role=run['role'],padding=padding,variant=variant,start=low,end=high,words=words))
    target=ROOT/'tools/music/alignment_review/phonetic_probes/starlit_rails.consonants.json';target.parent.mkdir(exist_ok=True);target.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    for w in result['windows']:print(w['line'],w['role'],w['padding'],w['variant'],[(x['text'],round(x['start'],3),round(x['end'],3),round(x['score'],3)) for x in w['words']])


if __name__=='__main__':main()
