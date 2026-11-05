"""Check physical retirement of the old archive UI and its scene references."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
removed = ['Assets/Scripts/TitleArchiveUI.cs', 'Assets/Scripts/TitleArchiveUI.cs.meta',
           'Assets/Scripts/MainStoryUI.cs', 'Assets/Scripts/MainStoryUI.cs.meta']
assert all(not (ROOT / name).exists() for name in removed)
references = []
for path in (ROOT / 'Assets/Scripts').rglob('*.cs'):
    if re.search(r'\b(?:TitleArchiveUI|MainStoryUI)\b', path.read_text(encoding='utf-8-sig')):
        references.append(path.relative_to(ROOT).as_posix())
assert not references, references
guid_check = subprocess.run(['rg', '-l', '0b61dd7548b0714428d5742700e5ae04|6f7bc80d50fcea64587e1e4589cc5960',
    'Assets', '-g', '*.unity', '-g', '*.prefab', '-g', '*.asset'], cwd=ROOT, capture_output=True, text=True)
assert guid_check.returncode == 1, guid_check.stdout + guid_check.stderr
catalogue = (ROOT / 'Assets/Scripts/VN/StoryArchiveCatalog.cs').read_text(encoding='utf-8-sig')
counts = {}
for name in ['Cgs', 'Characters', 'Trains', 'MusicEntries', 'SceneEntries', 'ChapterEntries']:
    body = re.search(r'public static readonly \w+\[\] ' + name + r'\s*=\s*\{([\s\S]*?)\n    \};', catalogue).group(1)
    counts[name] = len(re.findall(r'new \w+\s*\{', body))
old_music = set(re.findall(r'new MusicInfo \{ id = "([^"]+)"', catalogue))
music = (ROOT / 'Assets/Scripts/UI/Tablet/Apps/TabletMusicAppUI.cs').read_text(encoding='utf-8-sig')
assert old_music <= set(re.findall(r'new TabletMusicEntry \{ id = "([^"]+)"', music))
files = ['Assets/Scripts/VN/StoryArchiveCatalog.cs', 'Assets/Scripts/VN/StoryArchiveProgress.cs',
         'Assets/Scripts/VN/StoryReplayService.cs', 'Assets/Scripts/BookmarkManager.cs',
         'Assets/Scripts/UI/Tablet/Apps/TabletBookmarksUI.cs', 'Assets/Scripts/UI/Tablet/Apps/TabletHistoryUI.cs']
report = dict(scope=__doc__, physically_removed=removed, runtime_references=references, dangling_scene_guid_references=[],
    catalogue_counts=counts, legacy_music_ids_covered=sorted(old_music),
    source_sha256={name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in files},
    limitations=['Source audit only; actual new tablet launcher and unlock preservation verified separately in Unity.',
                 'Two historic CG planning entries without art stay out of the gallery; seven actual prologue CGs are shown.'])
output = ROOT / 'tools/vn/archive_migration_audit_20261103.json'
output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('ARCHIVE_UI_RETIRED_OK files=4 references=0 dangling_guids=0', counts)
