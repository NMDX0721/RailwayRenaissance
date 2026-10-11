# -*- coding: utf-8 -*-
"""
tools/build_chapter_01_master.py
Generates the fully expanded prologue_01_news.json according to §9.6 specifications
and MiMo work order T269 (Final Chapter of the Batch!).
"""

import json
import os
import re

def build_chapter_01():
    scenes = [
        # =========================================================================
        # Scene 0: black / "" (Part 1: 最后一次发车)
        # =========================================================================
        {
            "bg": "black",
            "bgm": "",
            "transition": "fade",
            "d": [
                {"t": "loc", "text": "2076年4月 · 雾峰站"},
                {"t": "n", "text": "初春的湿冷大雾封锁了整座深山。"},
                {"t": "n", "text": "锈迹斑斑的窄轨无声延伸进竹林深处。"},
                {"t": "n", "text": "昏暗的机车库里，柴油机剧烈咳了三声。"},
                {"t": "n", "text": "浓黑呛人的尾烟弥漫在潮湿的冷空气中。"},
                {"t": "d", "s": "老陈", "text": "再来一次……求你了，再转一把，就一把！", "e": "sad"},
                {"t": "n", "text": "老陈满手油污，颤抖着死死握住启动手柄。"},
                {"t": "n", "text": "高压起动机发出濒死般尖锐的摩擦怪叫。"},
                {"t": "n", "text": "微弱的气缸轰鸣刚刚泛起，转瞬跌回死寂。"},
                {"t": "n", "text": "高压喷油泵位置骤然传来一声沉闷脆响。"},
                {"t": "n", "text": "像某种支撑了数十年的生铁筋骨彻底断裂。"},
                {"t": "d", "s": "老陈", "text": "……不行了。机体抱死了，喷油嘴全炸了。", "e": "sad"},
                {"t": "n", "text": "老陈脱力般松开冰凉发黏的金属闸把。"},
                {"t": "n", "text": "在冰冷喘息的钢铁机车旁，他佝偻站了很久。"},
                {"t": "n", "text": "机油混合着粗盐霜，正从接缝无声滴落。"},
                {"t": "n", "text": "空旷的水泥站台上，没有哪怕一个乘客。"},
                {"t": "loc", "text": "雾峰村线 · 正式停运"},
                {"t": "n", "text": "刺骨山风呼啸穿过空荡荡的木质候车室。"},
                {"t": "n", "text": "发黄受潮的列车时刻表在墙上哗哗拍打。"},
                {"t": "n", "text": "这条坚持了四年的孤线，终究还是倒下了。"}
            ]
        },

        # =========================================================================
        # Scene 1: news_2072 / melancholy (Part 2: 大废线)
        # =========================================================================
        {
            "bg": "news_2072",
            "bgm": "melancholy",
            "transition": "fade",
            "d": [
                {"t": "loc", "text": "2050年 · 伦敦"},
                {"t": "n", "text": "世界首辆沙能试验车奇幻沙子正式面世。"},
                {"t": "n", "text": "沙子，一夜之间被奉为取代石油的新神祇。"},
                {"t": "n", "text": "狂热的资本与跨国车企如同潮水般涌入。"},
                {"t": "n", "text": "浮空飞艇的轰鸣声迅速盖过了旧时代汽笛。"},
                {"t": "n", "text": "没有人注意到，传统铁路正被推向历史绞架。"},
                {"t": "d", "s": "林彪悍", "text": "沙子成了新石油……可真正被抽干的又是谁？", "e": "sad"},
                {"t": "loc", "text": "2056年 · 全球停运潮"},
                {"t": "n", "text": "全球铁路客运总量发生毁灭性断崖下跌。"},
                {"t": "n", "text": "各大陆干线电气化扩建工程被全面叫停。"},
                {"t": "n", "text": "机车制造厂纷纷砸烂模具，改造沙能浮艇。"},
                {"t": "n", "text": "漫长的钢轨开始在荒烟蔓草中迅速锈蚀。"},
                {"t": "d", "s": "林彪悍", "text": "短短六年，几代人流汗铺下的钢轨就被当成了废铁。", "e": "angry"},
                {"t": "loc", "text": "2072年 · 大废线"},
                {"t": "n", "text": "全球铁路网迎来了最后的休克性停摆。"},
                {"t": "n", "text": "官方公报上冠冕堂皇称之为全面战略转型。"},
                {"t": "d", "s": "林彪悍", "text": "全面战略转型……说得真漂亮，不过是一纸强拆令。", "e": "serious"},
                {"t": "n", "text": "交通部一夜之间撤销了全部传统铁道运营牌照。"},
                {"t": "n", "text": "数百万靠铁轨谋生三代的老铁路人被勒令转岗。"},
                {"t": "n", "text": "而守在道班房里的老工人们只叫它大废线。"},
                {"t": "n", "text": "数以万计的重载机车被当成废铜烂铁回炉。"},
                {"t": "n", "text": "数百万公里的轨道被推土机连根野蛮扒起。"},
                {"t": "n", "text": "全境八万公里干线，被硬生生切成互不通联的绝路。"},
                {"t": "n", "text": "曾经吞吐百万吨物资的编组站长满一人高的蓬蒿。"},
                {"t": "n", "text": "铁锈顺枕木渗进泥土，像大地上无法愈合的血痂。"},
                {"t": "n", "text": "钢铁走廊被开辟为沙能飞艇的高压加注通道。"},
                {"t": "n", "text": "我滑动着档案终端上泛黄的历史新闻胶卷。"},
                {"t": "n", "text": "每一次所谓的技术跃进，背后都是生生被撕开的锈轨。"},
                {"t": "n", "text": "曾经隆隆作响的重载枢纽，如今只剩风沙穿堂。"},
                {"t": "n", "text": "最后苟延残喘的，只剩被世界遗忘的深山小线。"},
                {"t": "n", "text": "就像雾峰村这一段孤悬在外的二十三公里。"},
                {"t": "d", "s": "林彪悍", "text": "二十三公里……全境就只剩下这点最后的呼吸了么。", "e": "sad"},
                {"t": "n", "text": "那是地图上甚至不配标注名字的一道灰色盲肠。"},
                {"t": "n", "text": "它在江南湿冷的浓雾里，等待着最后的拆除令。"}
            ]
        },

        # =========================================================================
        # Scene 2: lab / melancholy (Part 3: 休学申请)
        # =========================================================================
        {
            "bg": "lab",
            "bgm": "melancholy",
            "transition": "fade",
            "chars": [
                {"name": "lin_biaohan", "pos": "left"},
                {"name": "daoshi", "pos": "right"}
            ],
            "d": [
                {"t": "loc", "text": "2076年5月 · 平壤 铁道综合大学 实验室"},
                {"t": "n", "text": "通宵运转的机柜阵列发出低沉焦躁的嗡鸣。"},
                {"t": "n", "text": "我揉了揉布满血丝的眼眶，盯着终端屏幕。"},
                {"t": "n", "text": "绿色的单色荧光屏上，光标还在徒劳跳转。"},
                {"t": "n", "text": "屏幕中央是一幅复杂的山区铁道路网模型。"},
                {"t": "n", "text": "然而在雾峰村节点周围，所有参数全呈暗红。"},
                {"t": "n", "text": "动态规划算法的状态空间再度发生致命溢出。"},
                {"t": "n", "text": "终端屏幕再度弹出一排刺眼的报错字符："},
                {"t": "n", "text": "【运力成本超出阈值，最优闭环路径无解】"},
                {"t": "d", "s": "林彪悍", "text": "可恶……不管代入多少次运力收敛公式，算出的惩罚项全都是无穷大。", "e": "worried"},
                {"t": "d", "s": "林彪悍", "text": "没有干线中继，没有动力机车，单纯算法在现实面前根本一文不值。", "e": "sad"},
                {"t": "n", "text": "实验室厚重的隔音木门被轻轻推开。"},
                {"t": "n", "text": "导师端着一杯冒着热气的浓茶，走到我身后。"},
                {"t": "n", "text": "他的目光落在屏幕那片闪烁报错的死局上。"},
                {"t": "d", "s": "导师", "text": "熬了整整三天三夜，你这台电脑里算来算去，果然还是那条线。", "e": "serious"},
                {"t": "d", "s": "林彪悍", "text": "教授……我只是想在数学上证明，那二十三公里哪怕只拉土特产也能活。", "e": "sad"},
                {"t": "d", "s": "导师", "text": "停手吧。算法能算出最优周转率，但算不出一台抱死停转的旧柴油机。", "e": "gentle"},
                {"t": "d", "s": "导师", "text": "铁道从来都不是冷冰冰的数字拓扑，它是活生生的钢铁与人情。", "e": "serious"},
                {"t": "d", "s": "导师", "text": "今天一大早，雾峰村那边有人辗转打通了办公室的长途电话。", "e": "serious"},
                {"t": "n", "text": "导师从风衣口袋掏出一张泛黄的便签条。"},
                {"t": "n", "text": "上面是铅笔匆忙写就的凌乱字迹："},
                {"t": "n", "text": "【高压喷油泵彻底炸裂，耕牛断气，全线停运】"},
                {"t": "d", "s": "林彪悍", "text": "是老陈叔打来的……连耕牛都彻底趴窝了？", "e": "surprise"},
                {"t": "d", "s": "导师", "text": "老陈在电话那头哽咽得说不出整话。他说……他守了四年，今天实在顶不住了。", "e": "sad"},
                {"t": "d", "s": "林彪悍", "text": "爷爷当年在那条险峻的崇山里守了四十年……四年前爷爷走了，老陈叔又苦撑了四年。", "e": "serious"},
                {"t": "d", "s": "林彪悍", "text": "这整整四年里，老陈叔每个月都会往平壤寄一包炒青豆，问我的自动化课业顺不顺利。", "e": "sad"},
                {"t": "d", "s": "导师", "text": "四年前你考进大学那天，你爷爷特意翻出了三十年前的站长呢子大衣。", "e": "gentle"},
                {"t": "d", "s": "导师", "text": "老汉跟我喝酒时说，老林家出了个懂电懂算法的后代，往后的铁道准能开得更远。", "e": "smile"},
                {"t": "d", "s": "导师", "text": "可他没等到看你毕业，大停运的风暴就把一切都吹散了。", "e": "sad"},
                {"t": "d", "s": "林彪悍", "text": "我至今还记得，小时候趴在爷爷肩头，听机车汽笛震响整座山谷。", "e": "gentle"},
                {"t": "d", "s": "林彪悍", "text": "那不是普通的噪音，那是深山里的人和外面世界连着的唯一心跳。", "e": "serious"},
                {"t": "d", "s": "导师", "text": "老汉要是能亲耳听见你这句话，九泉之下也能把腰杆挺直了。", "e": "gentle"},
                {"t": "d", "s": "林彪悍", "text": "我不能躲在象牙塔里敲代码写论文，眼睁睁看着爷爷用命守下来的钢轨被铲进炼钢炉。", "e": "serious"},
                {"t": "d", "s": "林彪悍", "text": "教授，我想回雾峰村去。", "e": "serious"},
                {"t": "d", "s": "导师", "text": "你想清楚了？回去可不是当大工程师，而是接手一个没钱没人的烂摊子。", "e": "serious"},
                {"t": "d", "s": "导师", "text": "沙能财阀的施工车队就在隘口虎视眈眈，只要两个星期没车跑，他们就会强拆钢轨。", "e": "serious"},
                {"t": "d", "s": "导师", "text": "他们做梦都想把那二十三公里的路基刨开，铺设高压电缆与沙能充电桩。", "e": "angry"},
                {"t": "d", "s": "林彪悍", "text": "我想得无比清楚。如果连我这个学铁道的孙子都不回去，那条老线就彻底死了。", "e": "serious"},
                {"t": "n", "text": "导师深深凝视着我，紧绷的嘴角微扬。"},
                {"t": "n", "text": "他从皮夹里抽出一张盖着大学红印的表格。"},
                {"t": "n", "text": "上面工整印着：学业休止与现场科研委派书。"},
                {"t": "d", "s": "导师", "text": "我就猜到你会这么选。字我昨晚就提前签好了，算你公派毕业实践。", "e": "smile"},
                {"t": "d", "s": "导师", "text": "楼下停机坪里，封存着一辆叫0721号的古董试验车，我特批交给你处置。", "e": "normal"},
                {"t": "d", "s": "导师", "text": "你随时可以出发。去看看它吧，那台机器身上，流淌着你爷爷当年的心血。", "e": "gentle"},
                {"t": "n", "text": "我双手接过那张沉甸甸的盖章申请书。"},
                {"t": "n", "text": "转身走到实验室宽大的全景落地窗前。"},
                {"t": "n", "text": "下方的露天停机坪正中，卧着一具深蓝机体。"},
                {"t": "n", "text": "深蓝色的机体线条凌厉，仿佛随时准备挣脱束缚。"},
                {"t": "n", "text": "我的掌心隐隐发烫，胸腔里的心跳与机库轰鸣共振。"},
                {"t": "n", "text": "在午后的刺目阳光下，冷峻厚重如铸铁。"},
                {"t": "thought", "s": "林彪悍", "text": "[四年前我离开雾峰村时，以为自己再也不会回头。但只要还有一根钢轨立在地上，我就得把它找出来。]", "e": "serious"}
            ]
        }
    ]

    result = {
        "id": "prologue_01_news",
        "scenes": scenes,
        "nextScript": "prologue_02_day0"
    }

    return result

def verify_script(data):
    issues = []
    total_chars = 0
    scene_chars = []
    narr_over25 = []
    text_paren = []
    pronoun_issues = []
    thought_count = 0

    valid_expressions = {
        'normal', 'smile', 'sad', 'surprise', 'serious', 'curious',
        'excited', 'worried', 'angry', 'bored', 'gentle', 'happy',
        'shocked', 'shout', 'smug', 'wink', 'cry', 'calm', 'shy'
    }

    bgm_dwell = {}
    current_bgm = None
    current_bgm_chars = 0

    for s_idx, scene in enumerate(data['scenes']):
        s_chars = 0
        bgm = scene.get('bgm', '')

        for d_idx, d in enumerate(scene.get('d', [])):
            t = d.get('t')
            spk = d.get('s')
            emo = d.get('e')
            txt = d.get('text', '')

            if t in ('d', 'n', 'thought'):
                char_len = len(txt)
                total_chars += char_len
                s_chars += char_len

            if t == 'n' and len(txt) > 25:
                narr_over25.append((s_idx, d_idx, len(txt), txt))

            if re.search(r'[（(][^）)]{1,30}[）)]', txt):
                text_paren.append((s_idx, d_idx, txt))

            if emo and emo not in valid_expressions:
                issues.append(f"Scene {s_idx} #{d_idx} invalid emo: {emo}")

            if t == 'thought':
                thought_count += 1
                if not spk:
                    issues.append(f"Scene {s_idx} #{d_idx} thought missing speaker")
                if not emo:
                    issues.append(f"Scene {s_idx} #{d_idx} thought missing emo")
                if not (txt.startswith('[') and txt.endswith(']')):
                    issues.append(f"Scene {s_idx} #{d_idx} thought missing brackets")

            if t == 'n' and '林彪悍' in txt:
                pronoun_issues.append((s_idx, d_idx, txt))

        scene_chars.append(s_chars)

        if bgm != current_bgm:
            if current_bgm:
                bgm_dwell[current_bgm] = bgm_dwell.get(current_bgm, 0) + current_bgm_chars
            current_bgm = bgm
            current_bgm_chars = s_chars
        else:
            current_bgm_chars += s_chars

    if current_bgm:
        bgm_dwell[current_bgm] = bgm_dwell.get(current_bgm, 0) + current_bgm_chars

    return {
        "total_chars": total_chars,
        "scene_chars": scene_chars,
        "narr_over25": narr_over25,
        "text_paren": text_paren,
        "pronoun_issues": pronoun_issues,
        "thought_count": thought_count,
        "bgm_dwell": bgm_dwell,
        "issues": issues
    }

if __name__ == '__main__':
    data = build_chapter_01()
    report = verify_script(data)
    print("Verification Report:")
    print(f"Total Chars: {report['total_chars']}")
    print(f"Scene Chars: {report['scene_chars']}")
    print(f"Narr over 25 ({len(report['narr_over25'])}): {report['narr_over25']}")
    print(f"Text paren ({len(report['text_paren'])}): {report['text_paren']}")
    print(f"Pronoun issues ({len(report['pronoun_issues'])}): {report['pronoun_issues']}")
    print(f"Thought count: {report['thought_count']}")
    print(f"BGM Dwell: {report['bgm_dwell']}")
    print(f"Other Issues ({len(report['issues'])}): {report['issues']}")

    out_path = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Resources', 'Scripts', 'prologue_01_news.json')
    with open(out_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')
    print(f"Wrote to {out_path} successfully.")
