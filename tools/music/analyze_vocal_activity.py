"""Pitch/RMS evidence on a separated vocal crop; not a semantic voice classifier."""
import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[2]


def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()


def main():
    p=argparse.ArgumentParser();p.add_argument('id',choices=['starlit_rails','chollima_ride']);p.add_argument('--start',type=float,required=True);p.add_argument('--end',type=float,required=True);a=p.parse_args()
    import numpy as np
    import librosa
    source=ROOT/f'Assets/Resources/bgm/{a.id}.mp3';waveform=ROOT/f'Library/MusicAlignment/vocals/{a.id}.wav'
    meta=json.loads(waveform.with_suffix('.json').read_text())
    if meta['originalAudioSha256']!=digest(source) or meta['derivedAudioSha256']!=digest(waveform) or meta['timeOffset']!=0:raise ValueError('Wrong separated audio')
    pcm=np.frombuffer(subprocess.check_output(['ffmpeg','-v','error','-i',str(waveform),'-ac','1','-ar','16000','-f','f32le','pipe:1']),dtype='<f4').copy()
    if not 0<=a.start<a.end<=len(pcm)/16000:raise ValueError('Invalid audio crop')
    crop=pcm[round(a.start*16000):round(a.end*16000)]
    f0,voiced,prob=librosa.pyin(crop,sr=16000,fmin=80,fmax=1000,frame_length=1024,hop_length=320,resolution=.2)
    rms=librosa.feature.rms(y=crop,frame_length=1024,hop_length=320)[0]
    times=a.start+np.arange(len(f0))*.02
    floor=max(.0001,float(rms.max())*.0316228)
    active=(prob>=.8)&(rms>=floor)&np.isfinite(f0)
    intervals=[];begin=None
    for i,value in enumerate(np.r_[active,False]):
        if value and begin is None:begin=i
        elif not value and begin is not None:
            intervals.append([round(float(times[begin]),3),round(min(a.end,a.start+i*.02),3)]);begin=None
    joined=[]
    for begin,end in intervals:
        if joined and begin-joined[-1][1]<=.12:joined[-1][1]=end
        else:joined.append([begin,end])
    joined=[x for x in joined if x[1]-x[0]>=.1]
    result=dict(id=a.id,start=a.start,end=a.end,audioSha256=digest(source),waveformSha256=digest(waveform),model='librosa.pyin on HDemucs vocals',librosaVersion=importlib.metadata.version('librosa'),toolSha256=digest(Path(__file__)),configuration=dict(sr=16000,frameLength=1024,hopLength=320,fmin=80,fmax=1000,resolution=.2,voicedProbability=.8,rmsFloor=floor),humanListeningVerified=False,semanticVocalIdentityVerified=False,candidateVoicedIntervals=joined,cropBoundaryIsNotVoiceBoundary=True,intervalTouchesCropEnd=bool(joined and joined[-1][1]>=a.end-.02),frames=[dict(time=round(float(t),3),f0=float(f) if np.isfinite(f) else None,probability=float(p),rms=float(r)) for t,f,p,r in zip(times,f0,prob,rms)])
    target=ROOT/f'tools/music/alignment_review/activity/{a.id}.{a.start:g}-{a.end:g}.json';target.parent.mkdir(exist_ok=True);target.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('VOCAL_ACTIVITY_DIAGNOSTIC_OK',joined,flush=True)


if __name__=='__main__':main()
