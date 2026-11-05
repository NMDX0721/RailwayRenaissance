"""Read-only source/resource audit; does not certify Unity import or playback."""
import argparse
import collections
import hashlib
import json
import pathlib
import re


ROOT = pathlib.Path(__file__).resolve().parents[2]
RES = ROOT / 'Assets/Resources'
TEXTURES = {'.png', '.jpg', '.jpeg', '.tga', '.psd', '.webp'}
AUDIO = {'.wav', '.mp3', '.ogg', '.aiff', '.aif'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=pathlib.Path, required=True)
    args = parser.parse_args()
    manager = (ROOT / 'Assets/Scripts/VN/CharacterSpriteManager.cs').read_text(encoding='utf-8-sig')
    aliases = dict(re.findall(r'\{\s*"([^"]+)",\s*"([^"]+)"\s*\}', manager))
    audio_source = (ROOT / 'Assets/Scripts/VN/AudioManager.cs').read_text(encoding='utf-8-sig')
    bgm_aliases = dict(re.findall(r'case "([^"]+)":\s*return "([^"]+)";', audio_source))
    background_source = (ROOT / 'Assets/Scripts/VN/BackgroundManager.cs').read_text(encoding='utf-8-sig')
    background_aliases = dict(re.findall(r'case "([^"]+)":\s*return "([^"]+)";', background_source))
    files = list(RES.rglob('*'))
    textures = {p.relative_to(RES).with_suffix('').as_posix() for p in files if p.suffix.lower() in TEXTURES}
    sounds = {p.relative_to(RES).with_suffix('').as_posix() for p in files if p.suffix.lower() in AUDIO}
    references = collections.defaultdict(list)
    character_checks = []
    voice_lines = []
    sources = {}
    counts = collections.Counter()

    def add(kind, name, location, **extra):
        if not name:
            return
        references[(kind, name)].append(dict(location=location, **extra))

    for path in sorted((RES / 'Scripts').glob('prologue_*.json')):
        raw = path.read_bytes()
        sources[path.stem] = hashlib.sha256(raw).hexdigest()
        script = json.loads(raw.decode('utf-8-sig'))
        for si, scene in enumerate(script['scenes']):
            loc = f'{path.stem}/scene/{si}'
            add('bg', scene.get('bg'), loc)
            add('bgm', scene.get('bgm'), loc)
            for di, entry in enumerate(scene.get('d', [])):
                kind = entry.get('t', '')
                counts[kind] += 1
                entry_loc = f'{loc}/entry/{di}'
                if kind == 'cg':
                    add('cg', entry.get('text'), entry_loc, size=entry.get('size', 'full'))
                if kind == 'd' and entry.get('s'):
                    voice_lines.append(dict(location=entry_loc, speaker=entry['s'],
                        text_zh=entry.get('text', ''), emotion=entry.get('e', ''),
                        text_sha256=hashlib.sha256(entry.get('text', '').encode()).hexdigest(),
                        translation_ja=None, audio=None, listening_verified=False))
                # Only ordinary spoken/thought entries reach UpdateDisplay. Narration clears sprites.
                if kind not in {'d', 'thought'} or not entry.get('s'):
                    continue
                chars = entry.get('chars') or scene.get('chars') or []
                speaker = aliases.get(entry['s'], entry['s'])
                reactions = dict(r.split(':', 1) for r in entry.get('reactions', []) if ':' in r)
                for char in chars:
                    name = char.get('name', '')
                    if not name:
                        continue
                    sprite = aliases.get(name, name)
                    emotion = entry.get('e') or scene.get('e')
                    emotion = emotion if emotion and (name == entry['s'] or sprite == speaker) else reactions.get(name, reactions.get(sprite, 'normal'))
                    wanted = [f'characters/{sprite}_{emotion}', f'characters/{sprite}/{emotion}']
                    fallback = [f'characters/{sprite}_normal', f'characters/{sprite}/normal'] if emotion != 'normal' else []
                    exact = next((p for p in wanted if p in textures), None)
                    resolved = exact or next((p for p in fallback if p in textures), None)
                    if not exact:
                        character_checks.append(dict(location=entry_loc, sprite=sprite,
                            emotion=emotion, status='normal_fallback' if resolved else 'missing', resolved=resolved))

    assets = []
    for (kind, name), uses in sorted(references.items()):
        resolved = bgm_aliases.get(name, name) if kind == 'bgm' else background_aliases.get(name, name) if kind == 'bg' else name
        key = f'{kind}/{resolved}'
        present = key in (sounds if kind == 'bgm' else textures)
        # These are explicitly intentional black cards in BackgroundManager.
        intentional = kind == 'bg' and name in {'black', 'news_2050', 'news_2072'}
        assets.append(dict(kind=kind, name=name, resource=key,
            status='present' if present else 'intentional_black' if intentional else 'missing', uses=uses))
    report = dict(schema=1, scope='prologue_01..10; source/filesystem only; indices are zero-based',
        limitations=['No Unity import, visual, branch reachability, audio or release verification.',
            'Voice locations are export coordinates, not stable production IDs; freeze IDs before recording.',
            'Texture presence does not prove image content or correct time of day.',
            'Character audit covers explicit scene/entry chars in d/thought, not dynamic UI or archive assets.'],
        source_sha256=sources, entry_counts=dict(counts), assets=assets,
        character_gaps=character_checks, voice_lines=voice_lines)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    missing = collections.Counter(a['kind'] for a in assets if a['status'] == 'missing')
    print(json.dumps(dict(missing=dict(missing),
        character_missing=sorted({c['sprite'] for c in character_checks if c['status'] == 'missing'}),
        character_normal_fallbacks=len([c for c in character_checks if c['status'] == 'normal_fallback']),
        dialogue_lines=len(voice_lines), output=str(args.out)), ensure_ascii=True))


if __name__ == '__main__':
    main()
