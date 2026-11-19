"""Check generated sprites and write Point-filtered Unity import metadata."""
from pathlib import Path
from uuid import uuid4
from PIL import Image

ROOT = Path(__file__).resolve().parents[2] / "Assets/Resources/Art/Explore"
TEMPLATE = (ROOT / "Tiles/tile_ground_grass_01.png.meta").read_text(encoding="utf-8")
EXPECTED = {
    "TilesHD": {"tile_ground_grass_01":(128,128),"tile_ground_grass_02":(128,128),"tile_ground_grass_03":(128,128),
                "tile_ground_dirt_01":(128,128),"tile_ground_dirt_02":(128,128),"tile_ground_dirt_03":(128,128),
                "tile_ground_ballast_01":(128,128),"tile_track_horizontal_01":(128,128),
                "tile_platform_surface_01":(128,128),"tile_platform_edge_yellow_01":(128,128)},
    "PropsHD":{"prop_rug":(384,256),"prop_filing_cabinet":(128,256),"prop_shelf_docs":(256,128),
               "prop_plant":(128,128),"prop_wall_map":(256,128),"prop_stove":(128,128),
               "prop_dispatch_desk":(256,128),"prop_strategy_board":(128,256),
               "prop_route_screen_off":(256,128),"prop_route_screen_on":(256,128),
               "prop_radio":(128,128),"prop_wall_clock":(128,128),
               "prop_sofa":(256,128),"prop_teatable":(128,128),
               "prop_station_sign":(256,256),"prop_signal_post_red":(128,256),
               "prop_signal_post_green":(128,256),"prop_platform_bench":(256,128),
               "prop_notice_board":(256,128),"prop_crossing_bar":(256,128),
               "prop_tea_row":(128,128),"prop_pine_tree":(256,384)},
    "InteriorsHD":{"int_wall_cap_h":(128,48),"int_wall_cap_v":(48,128),
                   **{f"int_floor_wood_{v:02d}":(128,128) for v in (1,2,3)},
                   **{f"int_floor_tile_{v:02d}":(128,128) for v in (1,2)},
                   "int_wall_top":(128,128),"int_wall_front":(128,192),
                   **{f"int_wall_corner_{c}":(128,128) for c in ("nw","ne","sw","se")},
                   "int_door_open":(128,256),"int_door_closed":(128,256),
                   "int_window_01":(128,192)},
}

def ensure_meta(path, ppu):
    meta = path.with_name(path.name+".meta")
    if meta.exists(): return
    content = TEMPLATE.replace("6cf2cdf123a54cd8ab6abcb325f4f030",uuid4().hex)
    content = content.replace("tile_ground_grass_01_0",path.stem+"_0")
    content = content.replace("spritePixelsToUnits: 32",f"spritePixelsToUnits: {ppu}")
    meta.write_text(content,encoding="utf-8")

for folder, files in EXPECTED.items():
    directory=ROOT/folder
    for name, size in files.items():
        path=directory/(name+".png")
        with Image.open(path) as im:
            assert im.size==size,(path,im.size,size)
            count=len(im.getcolors(maxcolors=1_000_000) or [])
            assert count<=32,(path,count)
            if folder=="TilesHD":
                assert all(im.getpixel((0,y))==im.getpixel((127,y)) for y in range(128)),path
                if name.startswith("tile_ground_"):
                    assert all(im.getpixel((x,0))==im.getpixel((x,127)) for x in range(128)),path
            if folder=="InteriorsHD" and name.startswith("int_floor_"):
                assert all(im.getpixel((0,y))==im.getpixel((127,y)) for y in range(128)),path
                assert all(im.getpixel((x,0))==im.getpixel((x,127)) for x in range(128)),path
        ensure_meta(path,128)
        print(f"OK {folder}/{name}.png {size[0]}x{size[1]} {count} colors PPU128")

for folder, name in [("TilesHD","outdoor_hd_contact_sheet"),("PropsHD","dispatch_additions_hd_contact_sheet"),
                     ("PropsHD","outdoor_props_hd_contact_sheet"),
                     ("InteriorsHD","dispatch_core_hd_contact_sheet"),
                     ("Concept","wufeng_hd_material_study_v1"),
                     ("Concept","wufeng_station_nf5k_hd_art_board_v3"),
                     ("Concept","nf5k_korean_consist_hd_concept_v3"),
                     ("Concept","nf5k_korean_consist_left_hd_concept_v4"),
                     ("Concept","dispatch_room_hd_art_direction_v2"),
                     ("Concept","nf5k_straight_left_hd_concept_v5"),
                     ("Concept","nf5k_straight_side_hd_concept_v6"),
                     ("Concept","nf5k_straight_left_hd_concept_v7"),
                     ("Concept","wufeng_buildings_hd_art_board_v1"),
                     ("Concept","lin_biaohan_four_direction_hd_concept_v2"),
                     ("Concept","player_anonymous_four_direction_concept_v1"),
                     ("Concept","player_soft_youthful_four_direction_concept_v2"),
                     ("Concept","player_pride_four_direction_concept_v3"),
                     ("Concept","player_pride_sleeve_four_direction_concept_v4"),
                     ("Concept","player_pride_soft_youthful_concept_v5"),
                     ("Concept","nf5k_integrated_headcar_consist_concept_v8")]:
    ensure_meta(ROOT/folder/(name+".png"),128)

for folder in EXPECTED:
    meta=(ROOT/(folder+".meta"))
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding="utf-8")
