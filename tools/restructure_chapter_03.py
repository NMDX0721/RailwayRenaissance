# -*- coding: utf-8 -*-
"""
tools/restructure_chapter_03.py
Restructures prologue_03_journey.json from 24 fragmented scenes into 6 well-balanced narrative chapters:
- Breaks up Scene 4 (182 lines) into 3 balanced dramatic beats:
    Beat 1: Canyon chase and perimeter warning (with cg_chase)
    Beat 2: Roadblock confrontation and criminal charges (with cg_arrest)
    Beat 3: Release by bureau chief order and grandfather's legacy
- Consolidates fragmented scenes into cohesive narrative segments:
    Act 1: Departure & Frontier (Day 1 Morning) - bg: border_town/china_sky, bgm: cloud_rail
    Act 2: Pursuit & Arrest (Day 1 Midday) - bg: cabin_interior, bgm: mystery -> determination
    Act 3: Behind the Scenes in Pyongyang (Day 1 Afternoon) - bg: tea_house, bgm: calm
    Act 4: Nightflight Dialogue & The Economics of Rails (Day 2) - bg: cabin_interior_night, bgm: starlit_rails
    Act 5: Journey Through the Heartlands & Hope (Day 3) - bg: cabin_interior, bgm: cloud_rail
    Act 6: Dawn Approaching Wufeng (Day 4 Morning) - bg: cabin_interior, bgm: melancholy
"""

import json
import os

def restructure():
    src_path = r'D:\Unity Project\RailwayRenaissance\Assets\Resources\Scripts\prologue_03_journey.json'
    with open(src_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    raw_scenes = data.get('scenes', [])
    print(f"Original scene count: {len(raw_scenes)}")

    # Act 1: Scenes 1, 2, 3 (Day 1 Morning - Border Crossing & Supply)
    act1_d = []
    for s_idx in [0, 1, 2]:
        act1_d.extend(raw_scenes[s_idx]['d'])

    act1 = {
        "bg": "china_sky",
        "bgm": "cloud_rail",
        "transition": "fade",
        "chars": [{"name": "lin_biaohan", "pos": "left"}, {"name": "suiyue", "pos": "right"}],
        "d": act1_d
    }

    # Act 2: Scene 4 broken down into 2 acts
    # 2A: Chase & Airspace Intercept
    s4_d = raw_scenes[3]['d']
    # split around index 45 (after cg_chase and escape attempt failure)
    act2_chase_d = s4_d[:53] # up to arrest command and cg_arrest
    act2 = {
        "bg": "cabin_interior",
        "bgm": "mystery",
        "transition": "fade",
        "chars": [{"name": "lin_biaohan", "pos": "left"}, {"name": "suiyue", "pos": "right"}],
        "d": act2_chase_d
    }

    # 2B: Confrontation & Grandpa's Name Release
    act3_confront_d = s4_d[53:]
    act3 = {
        "bg": "border_town",
        "bgm": "determination",
        "transition": "fade",
        "chars": [{"name": "lin_biaohan", "pos": "left"}],
        "d": act3_confront_d
    }

    # Act 4: Scene 5 & Scene 6 (Pyongyang Tea House - Xu Jiaying Intervention & Post-phone call)
    act4_d = []
    act4_d.extend(raw_scenes[4]['d']) # Scene 5
    act4_d.extend(raw_scenes[5]['d']) # Scene 6
    act4 = {
        "bg": "tea_house",
        "bgm": "calm",
        "transition": "fade",
        "chars": [{"name": "jiaying_xu", "pos": "right"}],
        "d": act4_d
    }

    # Act 5: Day 2 - Night Flight, Grandfather's Archive & Railway Economics (Scenes 7, 8, 9, 10, 11, 12, 13, 14, 15)
    act5_d = []
    for s_idx in range(6, 15):
        act5_d.extend(raw_scenes[s_idx]['d'])
    act5 = {
        "bg": "cabin_interior_night",
        "bgm": "starlit_rails",
        "transition": "fade",
        "chars": [{"name": "lin_biaohan", "pos": "left"}, {"name": "suiyue", "pos": "right"}],
        "d": act5_d
    }

    # Act 6: Day 3 & Day 4 - Approaching Wufeng, Faith & Legacy (Scenes 16 to 24)
    act6_d = []
    for s_idx in range(15, len(raw_scenes)):
        act6_d.extend(raw_scenes[s_idx]['d'])
    act6 = {
        "bg": "cabin_interior",
        "bgm": "melancholy",
        "transition": "fade",
        "chars": [{"name": "lin_biaohan", "pos": "left"}, {"name": "suiyue", "pos": "right"}],
        "d": act6_d
    }

    new_scenes = [act1, act2, act3, act4, act5, act6]
    data['scenes'] = new_scenes

    with open(src_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    print(f"Restructured into {len(new_scenes)} acts successfully!")
    for idx, sc in enumerate(new_scenes):
        print(f"Act {idx+1}: bg={sc['bg']}, bgm={sc['bgm']}, lines={len(sc['d'])}, chars={sum(len(l.get('text', '')) for l in sc['d'])}")

if __name__ == '__main__':
    restructure()
