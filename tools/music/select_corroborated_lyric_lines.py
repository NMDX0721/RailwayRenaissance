"""Select scoped, traceable automatic line evidence; never publish or relabel recordings."""
import copy
import json
from pathlib import Path
from audit_blind_consensus import BASE, ROOT, digest, evaluate_line
from publish_phonetic_draft import valid_word_intervals


def main():
    paths = [BASE/'phonetic/starlit_rails.local.sections.provider.boundaries.ordered.nasal.json',
             BASE/'lyricsmith/starlit_rails.native-audit.json',
             BASE/'qwen/starlit_rails.original.blind-asr.json']
    local, native, blind = [json.loads(p.read_text(encoding='utf-8')) for p in paths]
    audio_hash = digest(ROOT/'Assets/Resources/bgm/starlit_rails.mp3')
    source_hash = digest(ROOT/'Assets/Resources/Music/LyricSheets/starlit_rails.json')
    if any(r['audioSha256'] != audio_hash for r in [local,native,blind]):
        raise ValueError('Recording mismatch')
    if any(r['sourceSha256'] != source_hash for r in [local,native]):
        raise ValueError('Source mismatch')
    if blind['lyricPromptProvided'] or blind['wordTimestampsProvided']:
        raise ValueError('Blind-text evidence policy mismatch')
    for entry in native['nativeFiles']:
        if digest(ROOT/entry['file']) != entry['sha256']:
            raise ValueError('Native evidence changed')
    runs = {r['role']:r['lines'] for r in local['rounds']}
    selections = []
    for index, (original,vocals) in enumerate(zip(runs['original'],runs['vocals'])):
        decision = None
        if index+1 in [4,26,41]:
            decision = evaluate_line(native['lines'][index], original, vocals, blind['windows'])
        corroborated = decision and decision['scope'] != 'unresolved' and valid_word_intervals(vocals)
        selected = copy.deepcopy(vocals if corroborated else original)
        selected.update(selectedAudioRole='vocals' if corroborated else 'original',
                        selectionEvidence=decision if corroborated else None,
                        automaticEvidenceOnly=True,humanListeningVerified=False)
        if corroborated:
            selected['wordTimingScope'] = decision['scope']
            if decision['scope'] == 'line_only_corroborated':
                selected['words'] = []
        else:
            selected['wordTimingScope'] = 'unverified_phonetic_candidate'
        selections.append(selected)
    unresolved = []
    for selected, original, vocals in zip(selections,runs['original'],runs['vocals']):
        if selected['selectionEvidence']:
            continue
        if (original['flags'] or vocals['flags'] or not valid_word_intervals(original)
                or not valid_word_intervals(vocals) or abs(original['start']-vocals['start']) > .25
                or abs(original['end']-vocals['end']) > .35):
            unresolved.append(selected['line'])
    result = dict(id='starlit_rails',published=False,humanListeningVerified=False,
                  audioSha256=audio_hash,sourceSha256=source_hash,
                  inputs=[dict(path=str(p.relative_to(ROOT)),sha256=digest(p)) for p in paths],
                  toolSha256=digest(Path(__file__)),
                  note='Only 4/26/41 receive scoped cross-evidence selection. Other lines retain unverified candidates; agreement alone is not a word-accuracy certificate.',
                  unresolvedLocalGateLines=unresolved,lines=selections)
    (BASE/'phonetic/starlit_rails.scoped-selection.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('SCOPED_SELECTION_OK',[(l['line'],l['selectedAudioRole'],l['wordTimingScope']) for l in selections if l['selectionEvidence']])
    print('Remaining local gate lines:',unresolved)


if __name__ == '__main__':
    main()
