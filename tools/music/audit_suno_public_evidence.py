"""Audit already fetched, explicitly authorized Suno pages offline. No network or writes to Assets."""
import difflib
import hashlib
import html
import json
from pathlib import Path
import re
from extract_ost_lyric_sheets import ROOT, clean


def main():
    folder = ROOT / "tools/music/alignment_review"
    records = []
    for track, song_id in (
        ("starlit_rails", "58153497-be67-48f3-b92a-7d490e2827b4"),
        ("chollima_ride", "1faca03b-156a-41c1-8d4f-c7bc8cb2226a"),
    ):
        page = folder / f"suno-{song_id}.html"
        raw = page.read_text(encoding="utf-8-sig")
        # Public rendered lyric paragraph immediately precedes its copy button.
        match = re.search(r'<p\b[^>]*>((?:(?!<p\b).)*?)</p><button title="Copy lyrics to clipboard"', raw, re.S)
        if not match:
            raise ValueError("Public lyric paragraph not found: " + track)
        lyrics = html.unescape(re.sub(r"<[^>]+>", "", match[1]))
        plain = clean(lyrics)
        sheet = json.loads((ROOT / f"Assets/Resources/Music/LyricSheets/{track}.json").read_text(encoding="utf-8"))
        api = {}
        for version in ("v2", "v3"):
            response = folder / f"suno-{song_id}-{version}.json"
            payload = json.loads(response.read_text(encoding="utf-8-sig"))
            if payload != {"detail": "Unauthorized"}:
                raise ValueError("Unexpected response; inspect before interpreting: " + str(response))
            api[version] = dict(httpStatus=401, response=payload,
                                responseSha256=hashlib.sha256(response.read_bytes()).hexdigest())
        (folder / f"{track}.suno-public-lyrics.txt").write_text(lyrics + "\n", encoding="utf-8")
        record = dict(id=track, publicPage=f"https://suno.com/song/{song_id}",
                      pageSha256=hashlib.sha256(page.read_bytes()).hexdigest(),
                      publicLineCount=len(plain.splitlines()), authoredLineCount=len(sheet["plainText"].splitlines()),
                      equalsAuthored=plain == sheet["plainText"],
                      equalsEmbedded=plain == sheet.get("embeddedPlainText"),
                      diff=list(difflib.unified_diff(sheet["plainText"].splitlines(), plain.splitlines(),
                                                    fromfile="authored_prompt", tofile="public_generation_prompt", lineterm="")),
                      alignedLyrics=api, timingRetrieved=False, humanListeningVerified=False)
        records.append(record)
        print(f"{track}: public {record['publicLineCount']}, authored {record['authoredLineCount']}, "
              f"authored match={record['equalsAuthored']}, embedded match={record['equalsEmbedded']}; no timing retrieved")
    (folder / "suno-public-evidence.json").write_text(json.dumps(dict(
        checkedOn="2026-10-25", authorization="Only two song IDs; anonymous read-only GET; no audio/lyrics upload or credentials",
        interpretation="Public text is generation input, not proof of performed wording or timing. No private access attempted.",
        records=records), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
