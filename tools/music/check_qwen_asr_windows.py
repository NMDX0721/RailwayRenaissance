"""Blind, offline ASR of bounded original/vocal windows; no lyric prompt, no publishing."""
import argparse
from dataclasses import asdict
import hashlib
import json
from pathlib import Path
import subprocess
import os
import re
import sys

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest()


def main():
    # Redirected Windows terminals can otherwise abort after a successful Korean result.
    sys.stdout.reconfigure(encoding='utf-8')
    parser=argparse.ArgumentParser()
    parser.add_argument('id',choices=['starlit_rails','chollima_ride'])
    parser.add_argument('--window',action='append',required=True)
    parser.add_argument('--role',choices=['original','vocals'],default='original')
    parser.add_argument('--tag',default='')
    parser.add_argument('--model',choices=['asr','asr-1.7b'],default='asr')
    args=parser.parse_args()
    if args.tag and not re.fullmatch('[A-Za-z0-9_-]+',args.tag):raise ValueError('Invalid evidence tag')
    os.environ['HF_HUB_OFFLINE']='1';os.environ['TRANSFORMERS_OFFLINE']='1'
    import numpy as np
    import torch
    from qwen_asr import Qwen3ASRModel
    torch.set_num_threads(8)
    directory=ROOT/f'Library/MusicAlignment/models/qwen3-{args.model}'
    proof=json.loads((directory/'verification.json').read_text())
    for entry in proof['files']:
        if digest(directory/entry['file'])!=entry['sha256']:raise ValueError('Model asset mismatch')
    model=Qwen3ASRModel.from_pretrained(str(directory),dtype=torch.float32,device_map='cpu',local_files_only=True,max_new_tokens=128,max_inference_batch_size=1)
    original=ROOT/f'Assets/Resources/bgm/{args.id}.mp3'
    audio=original if args.role=='original' else ROOT/f'Library/MusicAlignment/vocals/{args.id}.wav'
    if args.role=='vocals':
        meta=json.loads(audio.with_suffix('.json').read_text())
        if meta['timeOffset']!=0 or meta['originalAudioSha256']!=digest(original) or meta['derivedAudioSha256']!=digest(audio):raise ValueError('Vocal source mismatch')
    pcm=np.frombuffer(subprocess.check_output(['ffmpeg','-v','error','-i',str(audio),'-ac','1','-ar','16000','-f','f32le','pipe:1']),dtype='<f4').copy()
    windows=[]
    for spec in args.window:
        a,b=map(float,spec.split(':'))
        if not 0<=a<b<=len(pcm)/16000:raise ValueError('Invalid window')
        result=model.transcribe(audio=(pcm[round(a*16000):round(b*16000)],16000),language='Korean',context='',return_time_stamps=False)[0]
        windows.append(dict(start=a,end=b,language=result.language,text=result.text))
        tag=('.'+args.model if args.model!='asr' else '')+('.'+args.tag if args.tag else '')
        output=ROOT/f'tools/music/alignment_review/qwen/{args.id}.{args.role}{tag}.blind-asr.json'
        output.parent.mkdir(exist_ok=True)
        output.write_text(json.dumps(dict(id=args.id,audioSha256=digest(original),waveformSha256=digest(audio),role=args.role,model=proof['repo'],revision=proof['revision'],toolSha256=digest(Path(__file__)),lyricPromptProvided=False,wordTimestampsProvided=False,humanListeningVerified=False,windows=windows),ensure_ascii=False,indent=2),encoding='utf-8')
        print(f'{args.id} {args.role} {spec}: {result.text}',flush=True)


if __name__=='__main__':main()
