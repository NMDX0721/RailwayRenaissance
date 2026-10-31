"""Extract original embedded OST lyrics. Requires ffprobe on PATH; never guesses timings."""
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / "Assets/Resources/Music/LyricSheets"
IDS = ("south_wind", "starlit_rails", "chollima_ride", "sleepers")


def clean(raw):
    lines = []
    for text in raw.splitlines():
        text = text.strip()
        if not text or text.startswith("[") or text.startswith("```"):
            continue
        text = re.sub(r"\([^)]*\)", "", text).strip()
        if not text or text == "G... C... G... D...":
            continue
        lines.append(text)
    return "\n".join(lines)


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    for track_id in IDS:
        audio = ROOT / "Assets/Resources/bgm" / f"{track_id}.mp3"
        result = subprocess.run(
            ["ffprobe", "-v", "error", "-show_entries", "format_tags", "-of", "json", str(audio)],
            check=True, capture_output=True, encoding="utf-8",
        )
        tags = json.loads(result.stdout)["format"]["tags"]
        raw = tags["lyrics-eng"]
        sheet = dict(id=track_id, title=tags.get("title", ""), artist=tags.get("artist", ""),
                     source="原声音频内嵌歌词", plainText=clean(raw), originalText=raw)
        destination = TARGET / f"{track_id}.json"
        if destination.exists():
            authored = json.loads(destination.read_text(encoding="utf-8"))
            if authored.get("promptRevision"):
                authored.update(originalText=raw, embeddedPlainText=clean(raw))
                sheet = authored
        destination.write_text(json.dumps(sheet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{track_id}: {len(sheet['plainText'].splitlines())} untimed lines")


if __name__ == "__main__":
    main()
