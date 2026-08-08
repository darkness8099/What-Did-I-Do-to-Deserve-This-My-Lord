# ART_INTAKE_LOG — 美术资源接入批次日志

每批合作美术资源接入完成后，在此追加一行记录。
配套文档：`ART_INTAKE_RULES.md`（接入规则）、`ART_NAMING_RULES.md`（命名规则）。

---

## 字段说明

| 字段 | 说明 |
|---|---|
| **批次日期** | YYYY-MM-DD，对应美术交付当天 |
| **批次名** | 与目录名一致，如 `2026-06-01_slime_pack_v1` |
| **件数** | 本批最终落入 `Assets/Art/<category>/` 或 `Assets/Audio/<category>/` 的文件数（不含 .meta、不含丢弃件） |
| **类别** | 涉及的资源类别（Tiles / Heroes / Monsters / DemonLord / UI / Backgrounds / Props / FX / Audio） |
| **处理结果** | OK / 部分接入 / 退回，简短一句话说明 |
| **执行人** | 人类 / AI 协助（标注是谁主导） |

---

## 接入记录

| 批次日期 | 批次名 | 件数 | 类别 | 处理结果 | 执行人 |
|---|---|---|---|---|---|
| 2026-05-29 | initial_pack（地底组）—— Tiles | 3 | Tiles | OK — `tile_soil_surface_00` / `tile_soil_deep_00` / `tile_entrance_default_00` 导入 `Art/Tiles/` | AI 协助 |
| 2026-05-29 | initial_pack（地底组）—— Characters | 2 | Heroes / Monsters | OK — `hero_warrior_idle_00` → `Art/Characters/Heroes/`；`monster_slime_idle_00` → `Art/Characters/Monsters/` | AI 协助 |
| 2026-05-29 | initial_pack（地底组）—— DemonLord | 1 | DemonLord | OK — `demonlord_idle_00` → `Art/DemonLord/`（入库待用，暂无代码引用） | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — Backgrounds | 1 | Backgrounds | OK — `bg_overworld_00`（3360×480，maxTextureSize=4096 保全宽）→ `Art/Backgrounds/` | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — Buildings | 3 | Buildings | OK — `building_00..02`（程序化生成池，无逐张语义）→ **新建** `Art/Buildings/` | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — Entrances | 5 | Entrances | OK — `entrance_00..04`（多格大尺寸，独立类别）→ **新建** `Art/Entrances/`；同步**删除**旧 `Art/Tiles/tile_entrance_default_00.png` 与 `Prefabs/PF_Environment_Entrance_Default.prefab` | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — Props | 11 | Props | OK — `prop_00..10`（程序化生成池）→ `Art/Props/` | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — SurfaceObjects | 8 | SurfaceObjects | OK — `surface_tree_a_idle_00..04`（5 帧动画）+ `surface_tree_b_00` + `surface_tree_c_00` + `surface_watchtower_00`（修正源目录 typo "watchower"）→ **新建** `Art/SurfaceObjects/` | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — Tiles | 64 | Tiles | OK — Soil 4 色主题集 × 16 张：`tile_soil_brown_00..15` / `tile_soil_dark_blue_00..15` / `tile_soil_dark_green_00..15` / `tile_soil_dark_purple_red_00..15` → `Art/Tiles/` | AI 协助 |
| 2026-05-31 | bulk_art_drop_v1 — Vegetation | 5 | Vegetation | OK — `veg_00..04`（程序化生成池）→ **新建** `Art/Vegetation/` | AI 协助 |
| 2026-06-01 | legacy_soil_cleanup_v1 | 2 删除 | Tiles | OK — 删除旧测试土块 `tile_soil_surface_00` / `tile_soil_deep_00`，当前默认土块生成完全切换到 4 色主题集 | AI 协助 |
| 2026-06-01 | overworld_backgrounds_v2 | 3 | Backgrounds | OK — `bg_overworld_01`（3360×553）/ `bg_overworld_02`（3360×431）/ `bg_overworld_03`（3360×415）→ `Art/Backgrounds/`；maxTextureSize=4096 保全宽；高度不一不影响使用，用户后续在 Editor 调整位置 | AI 协助 |

---

## 备注（可选追加）

- 当一批资源**未通过**审核被退回，仍要记录一行，处理结果填"退回（原因：xxx）"。
- 当**部分接入**（保留 N 件，退回 M 件），件数填实际接入数，处理结果说明退回部分。
- 批次较大或包含多类资源时，可拆成多行（每个类别一行）。
- 详细处理过程不写在本表里，写到 `AI_WORKFLOW_LOG.md` 的对应 TASK 条目。

---

## 2026-06-13 slime_animation_pack_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-06-13 | slime_animation_pack_v1 - Monsters | 17 | Monsters | OK - Imported Slime frames to `Art/Characters/Monsters/Slime/`: `monster_slime_move_00..04`, `monster_slime_absorb_00..05`, `monster_slime_death_00..05`; default idle frame is `monster_slime_move_00`. | AI assist |
| 2026-06-13 | slime_animation_pack_v1 - Vegetation | 28 | Monsters / Slime lifecycle | OK - Imported plant / flower frames to `Art/Characters/Monsters/Slime/{Plants,Flowers}/`: `veg_plant_growth_00..09`, `veg_plant_death_00..05`, `veg_flower_bloom_00..05`, `veg_flower_death_00..05`; plant default is `veg_plant_growth_09`, flower default is `veg_flower_bloom_05`. | AI assist |
| 2026-06-13 | slime_animation_pack_v1 - AnimationClips | 9 | Animations | OK - Added `Assets/Animations/Monsters/` and `Assets/Animations/Vegetation/`; Slime attack reuses move frames, Slime emit uses absorb frames in reverse order. | AI assist |
| 2026-06-13 | slime_animation_pack_v1 - Reclassify lifecycle art | 28 moved | Monsters / Slime lifecycle | OK - Moved `Plants` and `Flowers` folders from `Art/Backgrounds_Props/Vegetation/` into `Art/Characters/Monsters/Slime/` because they are Slime lifecycle products rather than generic vegetation. AnimationClip references preserved through Unity AssetDatabase move. | AI assist |

## 2026-06-13 legacy_slime_idle_cleanup_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-06-13 | legacy_slime_idle_cleanup_v1 | 1 删除 | Monsters | OK - 删除旧测试素材 `monster_slime_idle_00`（连同 .meta）。删除前已把唯二引用重指向到新默认帧 `monster_slime_move_00`：`PF_Monster_Slime_Default.prefab` 的 SpriteRenderer 与 `GameScene` 内 `MonsterRenderer.spriteSlime`。验证：Console 0 Error，全工程 0 残留 GUID 引用。 | AI 协助 |

## 2026-08-08 hero_directional_walk_pack_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-08 | hero_directional_walk_pack_v1 - Heroes | 12 | Heroes | OK - Imported 48×48 directional frames to `Art/Characters/Heroes/`: `hero_warrior_walk_{n,s,e,w}_00..02`; PPU 48 / Point / Uncompressed / no mipmap / Bottom Center. Source frame `01`（原始交付第 2 帧）作为各方向中立站姿。 | AI 协助 |
| 2026-08-08 | hero_directional_walk_pack_v1 - Animations | 9 | Animations | OK - Added 8 looping clips under `Animations/Heroes/` (`idle/walk × n/s/e/w`) plus `Resources/Hero/anim_warrior_ctrl.controller`; walk cycle is `00→01→02→01→00`, default state is `idle_s`. | AI 协助 |

## 2026-08-08 hero_idle_identity_fix_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-08 | hero_idle_identity_fix_v1 | 1 replaced / renamed | Heroes | OK - Confirmed legacy `hero_warrior_idle_00` belonged to a different Hero. Removed its old pixel content, copied the new Hero's `hero_warrior_walk_s_01` neutral pose, and renamed the default idle asset to `hero_warrior_idle_s_00`. Preserved the original `.meta` GUID so `GameScene`, `PF_Hero_Default`, and `anim_hero_warrior_idle_s` remain valid without saving the dirty Scene. | AI 协助 |

## 2026-08-08 hero_directional_walk_revision_v2

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-08 | hero_directional_walk_revision_v2 | 12 replaced + 1 idle refreshed | Heroes | OK - Replaced `hero_warrior_walk_{n,s,e,w}_00..02` from the corrected same-name delivery while preserving every `.meta/GUID`; regenerated `hero_warrior_idle_s_00` from corrected `hero_warrior_walk_s_01`. All 13 sprites remain Sprite/Single, PPU 48, Point, Uncompressed, no mipmap, Clamp, Bottom Center. | AI 协助 |

## 2026-08-08 hero_profession_templates_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-08 | hero_profession_templates_v1 - Heroes | 60 | Heroes | OK - Imported approved Warrior02, Mage01/02 and Priest01/02 directional frames as `hero_<class>_walk_{n,s,e,w}_00..02`; all are 48×48 Sprite/Single, PPU 48, Point, Uncompressed, no mipmap, Clamp, Bottom Center. Processed as five 12-file sub-batches. | AI 协助 |
| 2026-08-08 | hero_profession_templates_v1 - Animations | 45 | Animations | OK - Added 40 looping clips (`idle/walk × n/s/e/w × 5`) and 5 Resources controllers. Walk sequence is `00→01→02→01→00`, 8 FPS; each controller has 8 states and defaults to `idle_s`. | AI 协助 |
| 2026-08-08 | hero_profession_templates_v1 - Archetypes | 5 | Hero Configs | OK - Added `hero_warrior_02`, `hero_mage_01/02`, `hero_priest_01/02` archetypes with independent Sprite/Controller references. Neutral Warrior stats are temporary placeholders; Level 1 wave content was not changed. | AI 协助 |

## 2026-08-09 hero_profession_direction_fix_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-09 | hero_profession_direction_fix_v1 | 30 corrected | Heroes | OK - Corrected swapped east/west pixel payloads for Warrior02, Mage01/02 and Priest01/02 across frames `00..02`; retained all filenames, `.meta` files and GUIDs. Existing idle/walk clips and controllers already referenced direction-matched sprite paths, so no animation assets required modification. Original `hero_warrior` was intentionally untouched. | AI 协助 |

## 2026-08-08 unity_fx_sprite_pack_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-08 | unity_fx_sprite_pack_v1 - Projectiles | 30 | FX / Projectiles | OK - `AR101/102/103/105/106` mapped to `fx_projectile_chevron_{red,green,cyan,pink,purple}_00..05`. | AI 协助 |
| 2026-08-08 | unity_fx_sprite_pack_v1 - Attacks | 66 | FX / Attacks | OK - `ATFX101..106` mapped to six-color `fx_attack_impact_*_00..03`; `S6012_01..07` mapped to seven-color `fx_attack_sweep_*_00..05`. | AI 协助 |
| 2026-08-08 | unity_fx_sprite_pack_v1 - Blood | 9 | FX / Blood | OK - `B101` mapped to `fx_blood_burst_red_00..04`; `FL203` mapped to `fx_blood_arc_red_00..03`. | AI 协助 |
| 2026-08-08 | unity_fx_sprite_pack_v1 - Explosions | 36 | FX / Explosions | OK - `EXP001..006` mapped to `fx_explosion_burst_{cyan,green,purple,yellow,red,white}_00..05`. | AI 协助 |
| 2026-08-08 | unity_fx_sprite_pack_v1 - Smoke | 6 | FX / Smoke | OK - `SM301` mapped to `fx_smoke_ring_purple_00..05`. | AI 协助 |
| 2026-08-08 | unity_fx_sprite_pack_v1 - Animation Assets | 4 | Animations / FX | OK - Preserved and renamed the supplied green-chevron and red-blood-burst AnimationClip / AnimatorController pairs; all sprite references and GUIDs remain valid. | AI 协助 |

## 2026-08-09 bgm_music_pack_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-09 | bgm_music_pack_v1 - Loops | 7 | Audio / Music / Loops | OK - Renamed and archived as `bgm_battle_march_loop`, `bgm_dream_on_loop`, `bgm_journey_into_fog_loop`, `bgm_knight_power_loop`, `bgm_magic_within_loop`, `bgm_simple_positive_01_loop`, `bgm_simple_positive_04_loop`. | AI 协助 |
| 2026-08-09 | bgm_music_pack_v1 - FullTracks | 4 | Audio / Music / FullTracks | OK - Renamed and archived as `bgm_dream_on_full`, `bgm_magical_major_theme_02`, `bgm_magical_major_theme_07`, `bgm_simple_positive_04_full`. | AI 协助 |

## 2026-08-09 fun_basic_pixel_ui_buttons_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-09 | fun_basic_pixel_ui_buttons_v1 - Buttons | 4 | UI / Buttons | OK - Extracted one blank primary button family as `normal / hover / pressed / hover_pressed`; Sprite/Single, PPU 48, Point, Uncompressed, no mipmap, Clamp, Center Pivot, 4px sliced border. | AI 协助 |
| 2026-08-09 | fun_basic_pixel_ui_buttons_v1 - Icons | 36 | UI / Icons | OK - Extracted Home, Play, Fullscreen01, OpenList, WindowMode01, Sound01/02 and Music01/02 with the same four source states. | AI 协助 |
| 2026-08-09 | fun_basic_pixel_ui_buttons_v1 - Font | 1 | UI / Fonts | OK - Generated original dynamic ASCII pixel TTF `ui_font_demo_pixel`; covers English letters, digits and current Demo punctuation at runtime sizes / Bold. Uncommon symbols use a placeholder box; no CJK glyphs. | AI 协助 |

## 2026-08-09 fun_basic_pixel_ui_main_menu_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-09 | fun_basic_pixel_ui_main_menu_v1 | 5 | UI / Panels / Buttons | OK - Extracted `ui_panel_menu_tall` plus the blank `ui_btn_menu_*` four-state family. Menu labels remain runtime text; buttons use 3px sliced borders. | AI 协助 |

## 2026-08-09 fun_basic_pixel_ui_settings_v1

| Date | Batch | Count | Category | Result | Operator |
|---|---|---:|---|---|---|
| 2026-08-09 | fun_basic_pixel_ui_settings_v1 | 8 | UI / Panels / Buttons / Icons | OK - Extracted the blank Settings panel, stretchable dropdown field, compact four-state button family, and active / inactive meter segments. Reused the matching common icons already archived by TASK-089 instead of importing duplicates. | AI 协助 |
