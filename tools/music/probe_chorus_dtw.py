"""Compare repeated sung sections acoustically; never publish transferred timestamps."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    import librosa
    import numpy as np
    from scipy.spatial.distance import cdist
    audio = ROOT / 'Assets/Resources/bgm/chollima_ride.mp3'
    vocals = ROOT / 'Library/MusicAlignment/vocals/chollima_ride.wav'
    meta = json.loads(vocals.with_suffix('.json').read_text())
    if meta['originalAudioSha256'] != digest(audio) or meta['derivedAudioSha256'] != digest(vocals) or meta['timeOffset'] != 0:
        raise ValueError('Vocal recording identity changed')
    pcm = np.frombuffer(subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(vocals),
                        '-ac', '1', '-ar', '16000', '-f', 'f32le', 'pipe:1']), dtype='<f4').copy()
    hop, sr = 320, 16000
    spectrum = abs(librosa.stft(pcm, n_fft=1024, hop_length=hop)) ** 2
    mel = librosa.feature.melspectrogram(S=spectrum, sr=sr, n_mels=64)
    mfcc = librosa.feature.mfcc(S=librosa.power_to_db(mel), n_mfcc=20)[1:]
    mfcc = (mfcc-mfcc.mean(axis=1, keepdims=True))/np.maximum(mfcc.std(axis=1, keepdims=True), .01)
    chroma = librosa.feature.chroma_stft(S=spectrum, sr=sr, n_fft=1024, hop_length=hop)
    anchors = [52.16, 54.44, 57.36, 60.86, 63.66, 67.62, 73.64]
    reference = (51., 76.)
    targets = [('repeat_positive', 117., 145.), ('final', 188., 221.),
               ('instrumental_negative', 164., 191.), ('verse_negative', 78., 105.)]
    results = []
    for name, features in [('mfcc', mfcc), ('chroma', chroma)]:
        features = features / np.maximum(np.linalg.norm(features, axis=0, keepdims=True), 1e-6)
        x = features[:, round(reference[0]*sr/hop):round(reference[1]*sr/hop)]
        for target, start, end in targets:
            y = features[:, round(start*sr/hop):round(end*sr/hop)]
            cost = cdist(x.T, y.T, metric='cosine')
            if not np.isfinite(cost).all():
                raise ValueError('Non-finite acoustic distances')
            _, reverse_path = librosa.sequence.dtw(C=cost, subseq=True,
                step_sizes_sigma=np.array([[1, 1], [1, 2], [2, 1]]))
            path = reverse_path[::-1]
            mapped = []
            for anchor in anchors:
                frame = round((anchor-reference[0])*sr/hop)
                cells = path[abs(path[:, 0]-frame) <= 2, 1]
                mapped.append(dict(referenceTime=anchor, targetTime=round(start+float(np.median(cells))*hop/sr, 3)))
            result = dict(feature=name, target=target, targetCrop=[start,end],
                          meanPathCost=float(cost[path[:,0],path[:,1]].mean()),
                          matchedStart=round(start+int(path[0,1])*hop/sr,3),
                          matchedEnd=round(start+int(path[-1,1])*hop/sr,3), anchors=mapped)
            # Fixed-rate correlation is a separate diagnostic, not an assumption
            # that a changed final chorus preserves every vocal boundary.
            offsets = np.arange(y.shape[1]-x.shape[1]+1)
            diagonal_cost = np.array([cost[np.arange(x.shape[1]), np.arange(x.shape[1])+offset].mean()
                                      for offset in offsets])
            best = int(diagonal_cost.argmin())
            remote = diagonal_cost[abs(offsets-best)*hop/sr >= .5]
            lag = start+best*hop/sr-reference[0]
            result['fixedRate'] = dict(lagSeconds=round(lag,3), meanCost=float(diagonal_cost[best]),
                bestAlternativeAtLeast500msAway=float(remote.min()) if len(remote) else None,
                mappedAnchors=[round(a+lag,3) for a in anchors], boundaryTransferVerified=False)
            results.append(result)
            print(name, target, round(result['meanPathCost'],3), mapped, flush=True)
    report = dict(id='chollima_ride', published=False, humanListeningVerified=False,
                  semanticLyricIdentityVerified=False, referenceCrop=reference,
                  model='librosa subsequence DTW; slope steps (1,1),(1,2),(2,1)',
                  librosaVersion=librosa.__version__, sampleRate=sr, hopSamples=hop,
                  audioSha256=digest(audio), vocalSha256=digest(vocals), toolSha256=digest(Path(__file__)),
                  note='Anchors are existing candidate times; acoustic mappings alone do not prove syllable identity or correctness.',
                  results=results)
    output = ROOT / 'tools/music/alignment_review/chorus_dtw'
    output.mkdir(exist_ok=True)
    (output/'chollima_ride.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
