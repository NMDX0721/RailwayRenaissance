#!/usr/bin/env python3
"""铁路复兴：沙能冲击 — Gemini Speech Generation 批量配音生成。

读取 Assets/Resources/Scripts/*.json 中 t=n/d 的台词，按 tools/voice_map.json
的音色映射调用 Gemini TTS（gemini-3.1-flash-tts-preview），输出 wav 到
Assets/Resources/Voice/{script_id}/s{场景:02d}_d{序号:03d}.wav。

依赖：仅 Python 标准库。需要环境变量 GEMINI_API_KEY（Google AI Studio 取）。

用法：
  python tools/generate_voice.py --dry-run        # 只列出计划，不调 API
  python tools/generate_voice.py --limit 10       # 只生成前 10 条
  python tools/generate_voice.py                  # 全量（已存在自动跳过，可续跑）
"""
import argparse
import base64
import json
import os
import sys
import time
import urllib.error
import urllib.request
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPTS_DIR = ROOT / "Assets" / "Resources" / "Scripts"
VOICE_DIR = ROOT / "Assets" / "Resources" / "Voice"
MAP_PATH = Path(__file__).resolve().parent / "voice_map.json"

MODEL = "gemini-3.1-flash-tts-preview"
API_URL = "https://generativelanguage.googleapis.com/v1beta/interactions"
SKIP_TYPES = {"c", "scroll", "boot", "cg", "special", "loc"}
RATE_HZ = 24000  # 官方输出 PCM16 / 24kHz / 单声道
MAX_INPUT = 490   # 官方单次输入上限 500，留余量


def log(msg: str) -> None:
    print(msg, flush=True)


def clean_text(t: str) -> str:
    """整句 [xxx] 内心活动去括号（朗读用）；硬换行并成空格。"""
    t = t.strip()
    if len(t) > 2 and t.startswith("[") and t.endswith("]"):
        t = t[1:-1]
    return " ".join(t.split("\n")).strip()


def build_input(style: str, text: str) -> str:
    prompt = f"{style}\n{text}"
    if len(prompt) > MAX_INPUT:
        # 风格+台词超限：风格砍到第一句（保住年龄/性别锚定）
        short = style.split(". ")[0] + "."
        prompt = f"{short}\n{text}"
        if len(prompt) > MAX_INPUT:
            raise ValueError(f"输入超 {MAX_INPUT} 字符（{len(prompt)}），台词过长：{text[:40]}...")
    return prompt


def find_audio_b64(obj):
    """在响应 JSON 里递归找音频 base64（兼容 output_audio/outputAudio 等命名）。"""
    if isinstance(obj, dict):
        for key in ("output_audio", "outputAudio"):
            if key in obj:
                v = obj[key]
                if isinstance(v, str):
                    return v
                if isinstance(v, dict):
                    for k2 in ("data", "bytes", "b64", "audio"):
                        if isinstance(v.get(k2), str):
                            return v[k2]
        for v in obj.values():
            found = find_audio_b64(v)
            if found:
                return found
    elif isinstance(obj, list):
        for v in obj:
            found = find_audio_b64(v)
            if found:
                return found
    return None


def call_tts(api_key: str, voice: str, prompt: str, retries: int = 3) -> bytes:
    body = {
        "model": MODEL,
        "input": prompt,
        "response_format": {"type": "audio"},
        "generation_config": {"speech_config": [{"voice": voice}]},
    }
    data = json.dumps(body).encode("utf-8")
    last_err = None
    for attempt in range(retries):
        req = urllib.request.Request(
            API_URL,
            data=data,
            headers={
                "Content-Type": "application/json",
                "x-goog-api-key": api_key,
            },
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=90) as resp:
                payload = json.loads(resp.read().decode("utf-8"))
            b64 = find_audio_b64(payload)
            if not b64:
                raise RuntimeError(f"响应中找不到音频数据：{json.dumps(payload)[:300]}")
            return base64.b64decode(b64)
        except urllib.error.HTTPError as e:
            detail = e.read().decode("utf-8", "replace")[:300]
            last_err = f"HTTP {e.code}: {detail}"
            if e.code in (429, 500, 502, 503, 504) and attempt < retries - 1:
                wait = 5 * (attempt + 1)
                log(f"  [{e.code}] 重试 {attempt + 1}/{retries - 1}，{wait}s 后…")
                time.sleep(wait)
                continue
            raise RuntimeError(last_err) from None
        except Exception as e:  # noqa: BLE001 — 网络类错误统一重试
            last_err = str(e)
            if attempt < retries - 1:
                time.sleep(5 * (attempt + 1))
                continue
            raise RuntimeError(last_err) from None
    raise RuntimeError(last_err or "unknown error")


def save_wav(pcm: bytes, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".tmp")
    with wave.open(str(tmp), "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(RATE_HZ)
        wf.writeframes(pcm)
    tmp.replace(path)


def iter_entries():
    """产出 (script_id, scene_idx, dialogue_idx, speaker, text)。"""
    for f in sorted(SCRIPTS_DIR.glob("*.json")):
        if f.name.startswith("_"):
            continue
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
        except json.JSONDecodeError as e:
            log(f"跳过（JSON 解析失败）：{f.name} {e}")
            continue
        script_id = data.get("id", f.stem)
        for si, scene in enumerate(data.get("scenes", [])):
            for di, entry in enumerate(scene.get("d", [])):
                if entry.get("t") in SKIP_TYPES:
                    continue
                text = (entry.get("text") or "").strip()
                if not text:
                    continue
                yield script_id, si, di, entry.get("s") or "", text


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # Windows 控制台中文
    except Exception:  # noqa: BLE001
        pass

    ap = argparse.ArgumentParser(description="Gemini Speech Generation 批量配音")
    ap.add_argument("--dry-run", action="store_true", help="只列出计划，不调 API")
    ap.add_argument("--limit", type=int, default=0, help="最多生成 N 条（0=不限）")
    ap.add_argument("--sleep", type=float, default=1.5, help="每条之间的节流秒数")
    ap.add_argument("--only", default="", help="只处理指定 script_id（如 prologue_01_news）")
    ap.add_argument("--force", action="store_true", help="已存在也重新生成")
    ap.add_argument("--api-key", default="", help="覆盖 GEMINI_API_KEY 环境变量")
    args = ap.parse_args()

    voice_map = json.loads(MAP_PATH.read_text(encoding="utf-8"))
    speakers: dict = voice_map.get("speakers", {})
    fallback: dict = voice_map.get("fallback", {"voice": "Charon", "style": "Native Japanese."})

    api_key = args.api_key or os.environ.get("GEMINI_API_KEY", "")
    if not args.dry_run and not api_key:
        log("错误：缺少 GEMINI_API_KEY（环境变量或 --api-key）")
        log("  Google AI Studio → Get API key")
        return 1

    planned = 0
    written = 0
    skipped = 0
    failed = []

    for script_id, si, di, speaker, raw in iter_entries():
        if args.only and script_id != args.only:
            continue
        out = VOICE_DIR / script_id / f"s{si:02d}_d{di:03d}.wav"
        if out.exists() and not args.force:
            skipped += 1
            continue
        if args.limit and planned >= args.limit:
            break
        planned += 1

        cfg = speakers.get(speaker) or fallback
        if speaker not in speakers:
            log(f"  警告：说话人「{speaker}」不在 voice_map，使用 fallback")
        text = clean_text(raw)

        if args.dry_run:
            log(f"[dry] {script_id} s{si:02d} d{di:03d} {speaker or '旁白'} -> {cfg['voice']} | {text[:36]}")
            continue

        try:
            prompt = build_input(cfg["style"], text)
            pcm = call_tts(api_key, cfg["voice"], prompt)
            save_wav(pcm, out)
            written += 1
            log(f"[{written}] {script_id} s{si:02d} d{di:03d} {speaker or '旁白'} ({cfg['voice']}) -> {out.relative_to(ROOT)}")
        except Exception as e:  # noqa: BLE001 — 单条失败不中断全量
            failed.append((str(out.relative_to(ROOT)), str(e)))
            log(f"  失败：{out.name} {e}")

        if args.sleep > 0:
            time.sleep(args.sleep)

    log("")
    log(f"完成：计划 {planned}，生成 {written}，已存在跳过 {skipped}，失败 {len(failed)}")
    for path, err in failed:
        log(f"  失败列表：{path} — {err}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
