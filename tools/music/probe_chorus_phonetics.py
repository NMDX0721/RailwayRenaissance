"""Recheck the disputed final-chorus phrase inside an acoustic-repeat window."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Library/MusicAlignment/python'))


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    import torch
    import torchaudio
    torch.set_num_threads(8)
    folder = ROOT/'tools/music/alignment_review'
    parent_path = folder/'phonetic/chollima_ride.spoken.local.sections.allsections.provider.boundaries.json'
    parent = json.loads(parent_path.read_text(encoding='utf-8'))
    dtw_path = folder/'chorus_dtw/chollima_ride.json'
    dtw = json.loads(dtw_path.read_text(encoding='utf-8'))
    audio = ROOT/'Assets/Resources/bgm/chollima_ride.mp3'
    if parent['audioSha256'] != digest(audio) or dtw['audioSha256'] != digest(audio):
        raise ValueError('Recording changed')
    finals = [r for r in dtw['results'] if r['target'] == 'final']
    onset = sum(r['fixedRate']['mappedAnchors'][0] for r in finals)/len(finals)
    if len(finals) != 2 or max(r['fixedRate']['lagSeconds'] for r in finals)-min(r['fixedRate']['lagSeconds'] for r in finals) > .05:
        raise ValueError('Acoustic feature agreement failed')
    for r in finals:
        if r['fixedRate']['bestAlternativeAtLeast500msAway']-r['fixedRate']['meanCost'] < .1:
            raise ValueError('Weak acoustic offset preference')
    aligner = torchaudio.pipelines.MMS_FA.get_aligner()
    tokenizer = torchaudio.pipelines.MMS_FA.get_tokenizer()
    attempts = []
    for run in parent['rounds']:
        role = run['role']
        cache = ROOT/f'Library/MusicAlignment/mms/chollima_ride.{role}.pt'
        meta = json.loads(cache.with_suffix('.json').read_text())
        waveform = audio if role == 'original' else ROOT/'Library/MusicAlignment/vocals/chollima_ride.wav'
        if meta['audioSha256'] != digest(waveform) or meta['modelSha256'] != parent['modelSha256']:
            raise ValueError('Stale emission cache')
        values = torch.load(cache, weights_only=True)
        times, emission = values['times'], values['emission']
        line = run['lines'][43]
        for pad in [.35, .7]:
            lower, upper = onset-pad, min(run['lines'][44]['start'], 199.4+pad)
            lo, hi = int(torch.searchsorted(times, lower)), int(torch.searchsorted(times, upper))
            spans = aligner(emission[lo:hi], tokenizer([w['romanized'] for w in line['words']]))
            words = []
            for source, chars in zip(line['words'], spans):
                count = sum(c.end-c.start for c in chars)
                words.append(dict(text=source['text'], start=float(times[chars[0].start+lo])-.01,
                                  end=float(times[chars[-1].end+lo-1])+.01,
                                  score=sum(c.score*(c.end-c.start) for c in chars)/max(1,count),
                                  maxInternalGap=max([float(times[b.start+lo]-times[a.end+lo-1])
                                                      for a,b in zip(chars,chars[1:])] or [0.])))
            attempts.append(dict(role=role, pad=pad, window=[lower,upper], words=words))
    result = dict(id='chollima_ride', line=44, published=False, humanListeningVerified=False,
                  automaticEvidenceOnly=True, boundaryTransferVerified=False,
                  audioSha256=digest(audio), parentSha256=digest(parent_path), dtwSha256=digest(dtw_path),
                  toolSha256=digest(Path(__file__)), acousticCandidateOnset=onset, attempts=attempts)
    (folder/'chorus_dtw/chollima_ride.phonetics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    for a in attempts:
        print(a['role'],a['pad'],[(round(w['start'],3),round(w['end'],3),round(w['score'],3)) for w in a['words']])


if __name__ == '__main__':
    main()
