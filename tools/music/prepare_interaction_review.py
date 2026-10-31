"""Copy a disposable music runtime review project; never alter concurrent Editor work."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / "Library/MusicInteractionReviewProject"


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    for folder in ("Assets/Scripts", "Assets/Resources", "Assets/UI Toolkit", "Assets/UI", "Packages", "ProjectSettings"):
        shutil.copytree(ROOT / folder, TARGET / folder, dirs_exist_ok=True)
    editor = TARGET / "Assets/Editor"
    editor.mkdir(parents=True, exist_ok=True)
    for name in ("RailwayMusicTerminalReview.cs", "RailwayMusicTerminalReview.cs.meta"):
        shutil.copy2(ROOT / "Assets/Editor" / name, editor / name)
    files = ("Assets/Scripts/UI/Tablet/Apps/RailwayMusicTerminalView.cs",
             "Assets/Resources/Music/Lyrics/south_wind.txt", "Assets/Resources/Music/Lyrics/south_wind_info.json", "Assets/Resources/Music/LyricSheets/south_wind.json",
             "Assets/Scripts/UI/Tablet/Apps/MusicReactiveStageView.cs",
             "Assets/Scripts/UI/Tablet/Apps/TabletMusicAppUI.cs", "Assets/Scripts/UI/Tablet/Apps/MusicLibrarySession.cs",
             "Assets/Scripts/UI/Tablet/Apps/MusicLyricsView.cs", "Assets/Scripts/UI/Tablet/Apps/MusicTerminalIcons.cs", "Assets/Resources/UI/MusicTerminal.uss",
             "Assets/Editor/RailwayMusicTerminalReview.cs")
    (TARGET / "music-review-snapshot.json").write_text(json.dumps({
        "sourceProject": str(ROOT), "scope": "runtime scripts/resources and music review only; other Editor reviews excluded",
        "sha256": {name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in files}}, indent=2), encoding="utf-8")
    print(TARGET)


if __name__ == "__main__":
    main()
