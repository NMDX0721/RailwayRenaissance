"""Scope automatic evidence honestly: word-onset corroboration vs line-only support."""
import hashlib
import json
from pathlib import Path
import unicodedata

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'tools/music/alignment_review'


def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def normalized(text):
    return ''.join(c for c in unicodedata.normalize('NFKC',text).lower() if c.isalnum())


def evaluate_line(line, original, vocals, windows):
    exact=[w for w in windows if normalized(w['text'])==normalized(line['text'])
           and w['start']<=vocals['start'] and w['end']>=vocals['end']]
    local_line_agreement=abs(original['start']-vocals['start'])<=.25 and abs(original['end']-vocals['end'])<=.35
    website_line_agreement=abs(line['start']-vocals['start'])<=.25 and abs(line['end']-vocals['end'])<=.35
    strong_vocals=(not vocals['flags'] and vocals['phoneticScore']>=.15
                   and all(w['score']>=.05 for w in vocals['words']))
    word_start_agreement=(len(line['words'])==len(vocals['words'])
        and all(normalized(a['text'])==normalized(b['text']) and abs(a['start']-b['start'])<=.25
                for a,b in zip(line['words'],vocals['words'])))
    # A preceding crop independently transcribing a different phrase can exclude the old onset.
    exclusions=[w for w in windows if w['start']<=original['start']<w['end']<=vocals['start']
                and normalized(original['words'][0]['text']) not in normalized(w['text'])
                and w['text'].strip()]
    onset_supported=local_line_agreement or bool(exclusions)
    word_supported=bool(exact and strong_vocals and website_line_agreement and onset_supported
                        and word_start_agreement and line['nativeWordFiles'])
    line_supported=bool(exact and strong_vocals and website_line_agreement and onset_supported)
    return dict(line=line['line'],text=line['text'],automaticEvidenceOnly=True,humanListeningVerified=False,
                scope='word_onsets_and_line_end_corroborated' if word_supported else 'line_only_corroborated' if line_supported else 'unresolved',
                nativeWordEvidence=bool(line['nativeWordFiles']),exactBlindWindows=exact,
                selectedAudioRole='vocals' if line_supported else None,
                selectedStart=vocals['start'] if line_supported else None,selectedEnd=vocals['end'] if line_supported else None,
                words=vocals['words'] if word_supported else [],oldOnsetExclusionWindows=exclusions,
                checks=dict(strongVocalPhonetics=strong_vocals,originalVocalLineAgreement=local_line_agreement,
                            onlineVocalLineAgreement=website_line_agreement,onlineVocalWordStartAgreement=word_start_agreement,
                            exactBlindText=bool(exact)),
                note='Scope concerns source word onsets and outer line end; it is not a human accuracy certificate.')


def main():
    native_path=BASE/'lyricsmith/starlit_rails.native-audit.json'
    phonetic_path=BASE/'phonetic/starlit_rails.local.sections.provider.boundaries.json'
    blind_path=BASE/'qwen/starlit_rails.original.blind-asr.json'
    native=json.loads(native_path.read_text(encoding='utf-8'))
    phonetic=json.loads(phonetic_path.read_text(encoding='utf-8'))
    blind=json.loads(blind_path.read_text(encoding='utf-8'))
    audio=ROOT/'Assets/Resources/bgm/starlit_rails.mp3'
    source=ROOT/'Assets/Resources/Music/LyricSheets/starlit_rails.json'
    if any(r['audioSha256']!=digest(audio) for r in [native,phonetic,blind]):raise ValueError('Stale recording')
    if any(r['sourceSha256']!=digest(source) for r in [native,phonetic]):raise ValueError('Stale lyrics')
    if blind['lyricPromptProvided'] or blind['wordTimestampsProvided']:raise ValueError('ASR is not blind wordless evidence')
    rounds={r['role']:r['lines'] for r in phonetic['rounds']}
    results=[evaluate_line(native['lines'][n-1],rounds['original'][n-1],rounds['vocals'][n-1],blind['windows']) for n in [4,14,26,41,46]]
    output=dict(id='starlit_rails',published=False,humanListeningVerified=False,audioSha256=digest(audio),sourceSha256=digest(source),
                inputs=[dict(path=str(p.relative_to(ROOT)),sha256=digest(p)) for p in [native_path,phonetic_path,blind_path]],
                toolSha256=digest(Path(__file__)),decisions=results)
    (BASE/'qwen/starlit_rails.blind-consensus.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
    for result in results:print(result['line'],result['scope'])


if __name__=='__main__':main()
