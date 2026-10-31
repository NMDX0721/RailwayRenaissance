"""Native Hangul CTC diagnostics on local audio; not a lyric publication tool."""
import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import subprocess
import os
import re
from korean_ctc_path import align

ROOT=Path(__file__).resolve().parents[2]
VARIANTS={'starlit_rails':'local.sections.provider.boundaries','chollima_ride':'spoken.local.sections.allsections.provider.boundaries'}


def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()


def main():
    p=argparse.ArgumentParser();p.add_argument('id',choices=list(VARIANTS));p.add_argument('--lines',nargs='+',type=int,required=True)
    p.add_argument('--roles',nargs='+',choices=['original','vocals'],default=['original','vocals'])
    p.add_argument('--spoken-normalization',action='store_true');p.add_argument('--tag',default='');args=p.parse_args()
    if args.tag and not re.fullmatch('[A-Za-z0-9_-]+',args.tag):raise ValueError('Invalid evidence tag')
    os.environ['HF_HUB_OFFLINE']='1';os.environ['TRANSFORMERS_OFFLINE']='1'
    import numpy as np
    import torch
    from transformers import Wav2Vec2FeatureExtractor,Wav2Vec2ForCTC
    torch.set_num_threads(8)
    directory=ROOT/'Library/MusicAlignment/models/korean-ctc'
    proof=json.loads((directory/'verification.json').read_text())
    for item in proof['files']:
        if digest(directory/item['file'])!=item['sha256']:raise ValueError('Model asset mismatch')
    model=Wav2Vec2ForCTC.from_pretrained(str(directory),local_files_only=True).eval()
    extractor=Wav2Vec2FeatureExtractor.from_pretrained(str(directory),local_files_only=True)
    vocab=json.loads((directory/'vocab.json').read_text(encoding='utf-8'));reverse={v:k for k,v in vocab.items()};blank=model.config.pad_token_id
    audio=ROOT/f'Assets/Resources/bgm/{args.id}.mp3';source=ROOT/f'Assets/Resources/Music/LyricSheets/{args.id}.json'
    authored=json.loads(source.read_text(encoding='utf-8'))['plainText'].splitlines()
    reference=json.loads((ROOT/f'tools/music/alignment_review/phonetic/{args.id}.{VARIANTS[args.id]}.json').read_text(encoding='utf-8'))
    if digest(audio)!=reference['audioSha256'] or digest(source)!=reference['sourceSha256']:raise ValueError('Stale source')
    rounds={r['role']:r['lines'] for r in reference['rounds']}
    result=dict(id=args.id,audioSha256=digest(audio),sourceSha256=digest(source),model=proof['repo'],revision=proof['revision'],toolSha256=digest(Path(__file__)),pathCodeSha256=digest(Path(__file__).with_name('korean_ctc_path.py')),packageVersions={name:importlib.metadata.version(name) for name in ['torch','transformers','numpy']},humanListeningVerified=False,spokenNormalization=args.spoken_normalization,inputPolicy='AI=>에이아이;100=>백;optional 천리마=>철리마; source display unchanged',windows=[])
    output=ROOT/'tools/music/alignment_review/korean_ctc'
    output.mkdir(exist_ok=True);output=output/(args.id+('.spoken' if args.spoken_normalization else '')+('.'+args.tag if args.tag else '')+'.json')
    for role in args.roles:
        waveform=audio if role=='original' else ROOT/f'Library/MusicAlignment/vocals/{args.id}.wav'
        if role=='vocals':
            meta=json.loads(waveform.with_suffix('.json').read_text())
            if meta['timeOffset']!=0 or meta['originalAudioSha256']!=digest(audio) or meta['derivedAudioSha256']!=digest(waveform):raise ValueError('Invalid vocal provenance')
        pcm=np.frombuffer(subprocess.check_output(['ffmpeg','-v','error','-i',str(waveform),'-ac','1','-ar','16000','-f','f32le','pipe:1']),dtype='<f4').copy()
        for number in args.lines:
            if not 1<=number<=len(authored):raise ValueError('Invalid source line')
            first,last=max(0,number-2),min(len(authored),number+1)
            words=[];targets=[]
            for index in range(first,last):
                for text in authored[index].split():
                    spoken=text.replace('100','백').replace('AI','에이아이')
                    if args.spoken_normalization:spoken=spoken.replace('천리마','철리마')
                    spoken=''.join(c for c in spoken if '\uac00'<=c<='\ud7a3')
                    if not spoken:continue
                    if any(c not in vocab for c in spoken):raise ValueError('Unknown Hangul in '+spoken)
                    if targets:targets.append(vocab['|'])
                    begin=len(targets);targets.extend(vocab[c] for c in spoken)
                    words.append(dict(line=index+1,text=text,input=spoken,firstTarget=begin,lastTarget=len(targets)))
            start=min(r[first]['start'] for r in rounds.values());end=max(r[last-1]['end'] for r in rounds.values())
            for padding in [1.,3.]:
                a,b=max(0,start-padding),min(len(pcm)/16000,end+padding)
                features=extractor(pcm[round(a*16000):round(b*16000)],sampling_rate=16000,return_tensors='pt')
                with torch.inference_mode():log_prob=model(**features).logits[0].log_softmax(-1).cpu().numpy()
                spans,path_score=align(log_prob,targets,blank)
                step=model.config.inputs_to_logits_ratio/16000
                selected=[]
                for word in words:
                    chars=spans[word['firstTarget']:word['lastTarget']]
                    selected.append(dict(line=word['line'],text=word['text'],input=word['input'],start=round(a+chars[0]['start']*step,3),end=round(a+chars[-1]['end']*step,3),meanCharacterScore=float(np.mean([c['score'] for c in chars])),minCharacterScore=min(c['score'] for c in chars),characters=[dict(symbol=reverse[c['token']],start=round(a+c['start']*step,3),end=round(a+c['end']*step,3),score=c['score']) for c in chars]))
                greedy=[];previous=None
                for token in log_prob.argmax(-1):
                    token=int(token)
                    if token!=previous and token!=blank:greedy.append(reverse[token])
                    previous=token
                result['windows'].append(dict(targetLine=number,role=role,padding=padding,start=a,end=b,greedyText=''.join(greedy).replace('|',' '),pathScore=path_score,words=selected))
                output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
                print(args.id,number,role,padding,[(w['text'],w['start'],w['end'],round(w['meanCharacterScore'],3)) for w in selected if w['line']==number],flush=True)


if __name__=='__main__':main()
