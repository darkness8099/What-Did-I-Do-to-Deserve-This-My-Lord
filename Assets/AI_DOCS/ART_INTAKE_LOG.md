# ART_INTAKE_LOG — 美术资源接入批次日志

每批合作美术资源接入完成后，在此追加一行记录。
配套文档：`ART_INTAKE_RULES.md`（接入规则）、`ART_NAMING_RULES.md`（命名规则）。

---

## 字段说明

| 字段 | 说明 |
|---|---|
| **批次日期** | YYYY-MM-DD，对应美术交付当天 |
| **批次名** | 与目录名一致，如 `2026-06-01_slime_pack_v1` |
| **件数** | 本批最终落入 `Assets/Art/<category>/` 的文件数（不含 .meta、不含丢弃件） |
| **类别** | 涉及的资源类别（Tiles / Heroes / Monsters / DemonLord / UI / Backgrounds / Props / FX） |
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

---

## 备注（可选追加）

- 当一批资源**未通过**审核被退回，仍要记录一行，处理结果填"退回（原因：xxx）"。
- 当**部分接入**（保留 N 件，退回 M 件），件数填实际接入数，处理结果说明退回部分。
- 批次较大或包含多类资源时，可拆成多行（每个类别一行）。
- 详细处理过程不写在本表里，写到 `AI_WORKFLOW_LOG.md` 的对应 TASK 条目。
