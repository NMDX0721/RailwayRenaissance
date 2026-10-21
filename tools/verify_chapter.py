# -*- coding: utf-8 -*-
"""
tools/verify_chapter.py
剧本章节综合验收与校验工具 (Antigravity & MiMo 共享统一标准)
用法:
  python tools/verify_chapter.py 05
  python tools/verify_chapter.py prologue_05_inspection
  python tools/verify_chapter.py 06 --json
"""

import os
import sys
import json
import re

if sys.platform == 'win32':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

VALID_EXPRESSIONS = {
    'normal', 'smile', 'sad', 'surprise', 'serious', 'curious',
    'excited', 'worried', 'angry', 'bored', 'gentle', 'happy',
    'shocked', 'shout', 'smug', 'wink', 'cry', 'calm', 'shy'
}

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SCRIPTS_DIR = os.path.join(PROJECT_ROOT, 'Assets', 'Resources', 'Scripts')
BG_DIR = os.path.join(PROJECT_ROOT, 'Assets', 'Resources', 'bg')
BGM_DIR = os.path.join(PROJECT_ROOT, 'Assets', 'Resources', 'bgm')
DOCS_PLOT_DIR = os.path.join(PROJECT_ROOT, '参考资料', '剧情')

def find_script_file(target):
    if not os.path.exists(SCRIPTS_DIR):
        return None
    files = [f for f in os.listdir(SCRIPTS_DIR) if f.endswith('.json')]
    # 优先精确匹配 prologue_XX
    for f in files:
        if f.lower().startswith(f"prologue_{target.lower()}"):
            return os.path.join(SCRIPTS_DIR, f)
    # 其次匹配包含 prologue 和 target
    for f in files:
        if "prologue" in f.lower() and target.lower() in f.lower():
            return os.path.join(SCRIPTS_DIR, f)
    # 再次匹配任何包含 target 的文件
    for f in files:
        if target.lower() in f.lower():
            return os.path.join(SCRIPTS_DIR, f)
    return None

def find_text_file(target):
    if not os.path.exists(DOCS_PLOT_DIR):
        return None
    files = [f for f in os.listdir(DOCS_PLOT_DIR) if f.endswith('.txt')]
    for f in files:
        if f.lower().startswith(f"prologue_{target.lower()}") or (f"prologue_{target.lower()}" in f.lower()):
            return os.path.join(DOCS_PLOT_DIR, f)
    for f in files:
        if target.lower() in f.lower():
            return os.path.join(DOCS_PLOT_DIR, f)
    return None

def verify(target_name, output_json=False):
    script_path = find_script_file(target_name)
    if not script_path:
        err = f"未找到目标 JSON 剧本文件: {target_name} (搜索路径: {SCRIPTS_DIR})"
        if output_json:
            print(json.dumps({"status": "ERROR", "message": err}, ensure_ascii=False))
        else:
            print(f"[ERROR] {err}")
        return 1

    rel_name = os.path.basename(script_path)
    issues = []
    warnings = []

    try:
        with open(script_path, 'r', encoding='utf-8') as fh:
            data = json.load(fh)
    except Exception as e:
        issues.append(f"JSON 解析失败: {e}")
        if output_json:
            print(json.dumps({"status": "FAIL", "issues": issues}, ensure_ascii=False))
        else:
            print(f"[FAIL] {rel_name} JSON 解析失败: {e}")
        return 1

    scenes = data.get('scenes', [])
    if not scenes:
        issues.append("剧本中无 scenes 列表或为空")

    # 1. 资源库盘点
    bg_assets = set()
    if os.path.exists(BG_DIR):
        for f in os.listdir(BG_DIR):
            if f.lower().endswith(('.png', '.jpg', '.jpeg')):
                bg_assets.add(os.path.splitext(f)[0])

    bgm_assets = set()
    if os.path.exists(BGM_DIR):
        for f in os.listdir(BGM_DIR):
            if f.lower().endswith(('.mp3', '.ogg', '.wav')):
                bgm_assets.add(os.path.splitext(f)[0])

    # 2. 统计字段与遍历
    speaker_counts = {}
    total_dialogues = 0
    total_narrations = 0
    total_thoughts = 0
    total_locations = 0
    thought_issues = []
    expr_issues = []

    for s_idx, sc in enumerate(scenes):
        bg = sc.get('bg', '')
        if bg and bg not in bg_assets:
            warnings.append(f"Scene {s_idx}: bg '{bg}' 资源未就绪 (美术待补)")
        bgm = sc.get('bgm', '')
        if bgm and bgm not in bgm_assets:
            warnings.append(f"Scene {s_idx}: bgm '{bgm}' 音频未就绪 (音频待补)")

        dialogue_list = sc.get('d', [])
        for d_idx, d in enumerate(dialogue_list):
            t = d.get('t')
            spk = d.get('s')
            emo = d.get('e')
            txt = d.get('text', '')

            if t == 'd':
                total_dialogues += 1
                if spk:
                    speaker_counts[spk] = speaker_counts.get(spk, 0) + 1
                else:
                    issues.append(f"Scene {s_idx} 对话条目 #{d_idx} 缺少说话人 s")
            elif t == 'n':
                total_narrations += 1
                if spk:
                    warnings.append(f"Scene {s_idx} 旁白 #{d_idx} 附带了 speaker '{spk}'")
            elif t == 'thought':
                total_thoughts += 1
                if not spk:
                    thought_issues.append(f"Scene {s_idx} 独白 #{d_idx} 缺少说话人 s (会导致立绘/名牌丢失): '{txt[:20]}...'")
                if not emo:
                    thought_issues.append(f"Scene {s_idx} 独白 #{d_idx} 缺少表情 e: '{txt[:20]}...'")
                if not (txt.startswith('[') and txt.endswith(']')):
                    thought_issues.append(f"Scene {s_idx} 独白 #{d_idx} 未保留字面方括号 []: '{txt[:20]}...'")
            elif t == 'loc':
                total_locations += 1

            if emo and emo not in VALID_EXPRESSIONS:
                expr_issues.append(f"Scene {s_idx} #{d_idx} 非法表情: '{emo}'")

    if thought_issues:
        issues.extend(thought_issues)
    if expr_issues:
        issues.extend(expr_issues)

    # 3. 纯文本回环与行级比对
    text_path = find_text_file(target_name)
    roundtrip_status = "N/A"
    diff_count = 0

    if text_path:
        with open(text_path, 'r', encoding='utf-8') as fh:
            ref_lines = fh.readlines()

        # 生成回环文本
        header_lines = [l for l in ref_lines if l.startswith('#')]
        gen_lines = list(header_lines)
        if gen_lines and not gen_lines[-1].endswith('\n\n') and gen_lines[-1] != '\n':
            gen_lines.append('\n')

        for s_idx, sc in enumerate(scenes):
            bg = sc.get('bg', '')
            bgm = sc.get('bgm', '')
            tr = sc.get('transition', '')
            tr_str = f' transition={tr}' if tr else ''
            if s_idx > 0:
                gen_lines.append('\n')
            gen_lines.append(f'━━━ scene {s_idx}  [bg={bg} bgm={bgm}{tr_str}] ━━━\n')
            for d in sc.get('d', []):
                dt = d.get('t', 'd')
                spk = d.get('s', '')
                txt = d.get('text', '')
                if dt == 'loc':
                    gen_lines.append(f'  〔地点卡〕{txt}\n')
                elif dt == 'n':
                    gen_lines.append(f'  {txt}\n')
                elif dt == 'thought':
                    gen_lines.append(f'  {spk}：{txt}\n')
                else:
                    if spk:
                        gen_lines.append(f'  {spk}：{txt}\n')
                    else:
                        gen_lines.append(f'  {txt}\n')

        import difflib
        diff = list(difflib.unified_diff(ref_lines, gen_lines, fromfile='ref', tofile='gen', lineterm=''))
        diff_count = len(diff)
        if diff_count == 0:
            roundtrip_status = "100% 逐行完全匹配 (0 diff)"
        else:
            roundtrip_status = f"存在差异 ({diff_count} 行 diff)"
            issues.append(f"回环纯文本与手改稿存在 {diff_count} 行差异")

    status_str = "PASS" if not issues else "FAIL"
    report = {
        "status": status_str,
        "script": rel_name,
        "scenes_count": len(scenes),
        "total_dialogues": total_dialogues,
        "total_narrations": total_narrations,
        "total_thoughts": total_thoughts,
        "total_locations": total_locations,
        "speakers": speaker_counts,
        "roundtrip": roundtrip_status,
        "issues": issues,
        "warnings": warnings[:5]  # 只展示前5个警告
    }

    if output_json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print("=" * 60)
        print(f"【剧本章节校验结果】: {rel_name} -> {status_str}")
        print("=" * 60)
        print(f"- Scene 数量: {len(scenes)}")
        print(f"- 对话 / 旁白 / 独白 / 地点卡: {total_dialogues} / {total_narrations} / {total_thoughts} / {total_locations}")
        print(f"- 说话人分布: {speaker_counts}")
        print(f"- 文本覆盖度: {roundtrip_status}")
        if warnings:
            print(f"- 警告提示 ({len(warnings)} 项):")
            for w in warnings[:5]:
                print(f"   [WARN] {w}")
        if issues:
            print(f"- 存在问题 ({len(issues)} 项):")
            for iss in issues:
                print(f"   [FAIL] {iss}")
        else:
            print("- 校验状态: 全部规则符合规范！")
        print("=" * 60)

    return 0 if not issues else 1

if __name__ == '__main__':
    args = sys.argv[1:]
    target = '05'
    use_json = False
    for a in args:
        if a == '--json':
            use_json = True
        else:
            target = a
    sys.exit(verify(target, use_json))
