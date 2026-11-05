"""Export VN source context for translators; validate returned independent locale files.

No external requests, translation generation, or production script mutation.
"""
import argparse
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
READABLE = {'d', 'n', 'thought', 'c', 'loc', 'scroll', 'boot'}


def source_hash(text):
    return hashlib.sha256(text.replace('\r\n', '\n').encode('utf-8')).hexdigest()


def source_entries(script):
    result = {}
    for si, scene in enumerate(script.get('scenes', [])):
        for di, entry in enumerate(scene.get('d', [])):
            if entry.get('t') not in READABLE:
                continue
            key = script['id'] + '/' + (entry.get('id') or f's{si}/d{di}')
            parts = [(key, entry.get('text', ''), entry.get('t'))]
            parts += [(key + f'/option-{oi}', option.get('text', ''), 'option') for oi, option in enumerate(entry.get('opts') or [])]
            for entry_key, text, kind in parts:
                if not text or not text.strip():
                    continue
                if entry_key in result:
                    raise ValueError('Duplicate source ID: ' + entry_key)
                result[entry_key] = dict(key=entry_key, source=text, sourceHash=source_hash(text),
                    kind=kind, speaker=entry.get('s', ''), emotion=entry.get('e', ''), sceneIndex=si, dialogueIndex=di)
    return result


def export(script, locale):
    entries = []
    for entry in source_entries(script).values():
        # Fix source boundaries before translation. Never infer cross-language alignment at runtime.
        segments = re.findall(r'.+?(?:[。！？]+[」』”]*|$)', entry['source'], re.S)
        assert ''.join(segments) == entry['source']
        entries.append(dict(entry, status='draft', text='', segments=[dict(source=s, text='') for s in segments]))
    return dict(schemaVersion=1, scriptId=script['id'], locale=locale, sourceLanguage='zh-Hans', entries=entries)


def validate(package, script):
    if package.get('schemaVersion') != 1 or package.get('scriptId') != script['id']:
        raise ValueError('Invalid schemaVersion/scriptId')
    available = source_entries(script)
    seen = set()
    ready = 0
    for entry in package.get('entries', []):
        key = entry['key']
        if key in seen or key not in available:
            raise ValueError('Unknown/duplicate ID: ' + key)
        seen.add(key)
        source = available[key]
        if entry.get('sourceHash') != source['sourceHash']:
            raise ValueError('Source changed: ' + key)
        if 'source' in entry and entry['source'] != source['source']:
            raise ValueError('Source overwritten: ' + key)
        if entry.get('status') != 'ready':
            continue
        if not entry.get('text', '').strip():
            raise ValueError('Empty translation: ' + key)
        segments = entry.get('segments')
        if not segments:
            raise ValueError('Missing explicit segments: ' + key)
        if segments:
            if any(not s.get('source') or not s.get('text', '').strip() for s in segments):
                raise ValueError('Empty segment: ' + key)
            if ''.join(s['source'] for s in segments) != source['source']:
                raise ValueError('Segment source mismatch: ' + key)
            if ''.join(s['text'] for s in segments) != entry['text']:
                raise ValueError('Segment target mismatch: ' + key)
        if re.search(r'\[(?:whispers|laughs|sighs|curious|crying)\]', entry['text'], re.I):
            raise ValueError('TTS tags in displayed translation: ' + key)
        ready += 1
    return ready, len(available) - ready


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--locale', required=True)
    parser.add_argument('--out', type=pathlib.Path)
    parser.add_argument('--validate', type=pathlib.Path)
    parser.add_argument('--pattern', default='prologue_*.json')
    args = parser.parse_args()
    languages = json.loads((ROOT / 'Assets/Resources/Localization/VN/languages.json').read_text(encoding='utf-8'))['languages']
    if args.locale not in {l['code'] for l in languages} - {'zh-Hans'}:
        parser.error('Register a target locale in languages.json first')
    if not args.out and not args.validate:
        parser.error('Provide --out or --validate')
    if args.validate:
        ready_total = missing_total = 0
        files = sorted(args.validate.glob('*.json'))
        if not files:
            raise ValueError('No locale files found')
        for path in files:
            package = json.loads(path.read_text(encoding='utf-8-sig'))
            if not re.fullmatch(r'[A-Za-z0-9_-]+', package.get('scriptId', '')) or path.stem != package['scriptId']:
                raise ValueError('Invalid scriptId/filename: ' + str(path))
            if package.get('locale') != args.locale:
                raise ValueError('Locale mismatch: ' + str(path))
            script = json.loads((ROOT / 'Assets/Resources/Scripts' / (package['scriptId'] + '.json')).read_text(encoding='utf-8-sig'))
            ready, missing = validate(package, script)
            ready_total += ready
            missing_total += missing
        print(f'VN_TRANSLATION_VALID files={len(files)} ready={ready_total} missing_or_draft={missing_total}')
    else:
        args.out.mkdir(parents=True, exist_ok=True)
        scripts = sorted((ROOT / 'Assets/Resources/Scripts').glob(args.pattern))
        if not scripts:
            raise ValueError('No source scripts found')
        for path in scripts:
            output = args.out / path.name
            if output.exists():
                raise FileExistsError('Refusing to overwrite translator work: ' + str(output))
            script = json.loads(path.read_text(encoding='utf-8-sig'))
            output.write_text(json.dumps(export(script, args.locale), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        print(f'VN_TRANSLATION_EXPORTED files={len(scripts)} locale={args.locale} out={args.out}')


if __name__ == '__main__':
    main()
