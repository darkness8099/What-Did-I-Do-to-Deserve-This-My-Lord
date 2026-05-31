# ART_NAMING_RULES — 美术资源命名规则

本文档规定本项目美术资源的统一命名规范。
配套文档：`ART_INTAKE_RULES.md`（目录结构与接入流程）、`ART_INTAKE_LOG.md`（批次日志）。

---

## 一、核心原则

```
小写 + 下划线 + 类别前缀 + 状态后缀 + 帧序号
```

- 全部 **小写**
- 单词之间用 **下划线 `_`** 分隔
- 类别前缀（`tile_` / `hero_` / `monster_` / `ui_` / `bg_` / `fx_` 等）必须固定
- 状态后缀（`idle` / `walk` / `attack` 等）描述当前动作或变体
- 帧序号 **两位数字**（`00` / `01` / `02`），**`_00` 表示第 0 帧（也是当前单帧的唯一帧）**

### 帧动画前向兼容设计

本项目美术资源命名从一开始就为帧动画扩展预留空间：

| 当前阶段（单帧） | 未来动画阶段 |
|---|---|
| `monster_slime_idle_00.png` — 唯一 idle 帧 | `monster_slime_idle_01.png`、`monster_slime_idle_02.png` … 逐帧追加 |
| `hero_warrior_walk_e_00.png` — 唯一 walk 帧 | `hero_warrior_walk_e_01.png`、`hero_warrior_walk_e_02.png` … |

当美术交付 Sprite Sheet（动画合集）时，使用 `_sheet` 后缀代替 `_00`：

| 类型 | 命名示例 |
|---|---|
| 单帧（当前） | `monster_slime_idle_00.png` |
| 逐帧序列（未来） | `monster_slime_idle_00.png` ~ `monster_slime_idle_NN.png` |
| Sprite Sheet（未来） | `monster_slime_idle_sheet.png`（Sprite Mode 改为 Multiple，切片命名沿用 `_00`/`_01`） |
| AnimationClip | `anim_slime_idle.anim` |
| AnimatorController | `anim_slime_ctrl.controller` |

**`_00` 不是版本号，不是"第一个版本"，而是帧序号中的第 0 帧。** 当前只有一帧时就是 `_00`，后续美术交付更多帧时直接追加 `_01`、`_02` 即可，不需要改 `_00` 的文件名。

### 程序化生成批次的命名简化

当一组资源是**面向程序化生成池**的批量素材（例如 11 个无具体语义的 props、5 个杂植被变体、4 色×16 张 soil tile 变体），允许省略 `<name>` 段，使用纯 `<prefix>_<index>` 形式：

| 形式 | 示例 | 适用 |
|---|---|---|
| 完整模板 `<prefix>_<name>_<index>` | `prop_chest_00`、`monster_slime_idle_00` | 已知具体语义的单件资源 |
| **简化形式 `<prefix>_<index>`** | `prop_00..10`、`veg_00..04`、`building_00..02`、`entrance_00..04` | 程序化生成池的批量素材，无需个别语义 |
| 变体形式 `<prefix>_<type>_<variant>_<index>` | `tile_soil_brown_00..15`、`tile_soil_dark_blue_00..15` | 同一 type 的多 variant 主题集 |

简化形式必须在 `ART_INTAKE_LOG.md` 的对应批次备注里写明"程序化生成池，无逐张语义"，后续如需补语义可批量 rename。

### 与代码命名的区分

| | 命名风格 | 示例 |
|---|---|---|
| **代码（C# 脚本 / 类 / 方法）** | `PascalCase` | `GridRenderer`、`HeroData`、`MoveHero()` |
| **美术资源（文件 / sprite / material）** | `snake_case` | `monster_slime_idle_00`、`mat_tile_soil` |

两套规则在视觉上一眼区分，查找时不会混淆。

---

## 二、各类资源命名模板

| 类别 | 模板 | 示例 |
|---|---|---|
| **Tile** | `tile_<type>_<variant>_<index>` | `tile_soil_normal_00`、`tile_empty_glow_00`、`tile_wall_default_00`、`tile_entrance_default_00` |
| **Hero** | `hero_<class>_<state>_<index>` | `hero_warrior_idle_00`、`hero_warrior_walk_e_00`、`hero_warrior_attack_00` |
| **Monster** | `monster_<species>_<state>_<index>` | `monster_slime_idle_00`、`monster_slime_attack_00`、`monster_slime_death_00` |
| **DemonLord** | `demonlord_<state>_<index>` | `demonlord_idle_00`、`demonlord_captured_00` |
| **UI Icon** | `ui_icon_<name>` | `ui_icon_hp`、`ui_icon_attack`、`ui_icon_magic` |
| **UI Panel** | `ui_panel_<name>` | `ui_panel_victory`、`ui_panel_defeat`、`ui_panel_pause` |
| **UI Button** | `ui_btn_<name>_<state>` | `ui_btn_restart_normal`、`ui_btn_restart_hover` |
| **UI Font** | `ui_font_<name>` | `ui_font_title`、`ui_font_body` |
| **Background** | `bg_<name>_<index>` | `bg_dungeon_00`、`bg_overworld_00` |
| **Entrance**（多格大尺寸） | `entrance_<name>_<index>` 或简化 `entrance_<index>` | `entrance_cave_00`、`entrance_00..04`（程序化池） |
| **Building** | `building_<name>_<index>` 或简化 `building_<index>` | `building_house_00`、`building_00..02` |
| **SurfaceObject** | `surface_<name>_<state>_<index>` | `surface_tree_a_idle_00..04`（动画帧）、`surface_tree_b_00`、`surface_watchtower_00` |
| **Vegetation** | `veg_<name>_<index>` 或简化 `veg_<index>` | `veg_grass_00`、`veg_00..04` |
| **Prop** | `prop_<name>_<index>` 或简化 `prop_<index>` | `prop_chest_00`、`prop_00..10` |
| **FX** | `fx_<event>_<index>` | `fx_dig_00`、`fx_hit_00`、`fx_slime_spawn_00` |
| **Sprite Sheet（合集）** | 上述 + `_sheet` 后缀 | `monster_slime_idle_sheet`、`hero_warrior_walk_sheet` |
| **Material** | `mat_<对应资源名>` | `mat_tile_soil`、`mat_monster_slime` |
| **Animation Clip** | `anim_<对应角色>_<state>` | `anim_slime_idle`、`anim_warrior_walk` |
| **Animator Controller** | `anim_<对应角色>_ctrl` | `anim_slime_ctrl`、`anim_warrior_ctrl` |
| **Prefab** | 对应角色名首字母大写 + `.prefab` | `Slime.prefab`、`Warrior.prefab`、`TileSoil.prefab`（**Prefab 是代码引用对象，PascalCase** + `.prefab`） |

> **方向后缀**（仅在需要朝向时使用）：`e`（east 右）/ `w`（west 左）/ `n`（north 上）/ `s`（south 下）。
> 例：`hero_warrior_walk_e_00`。

---

## 三、Tile 命名映射本项目 CellType

本项目 `GridData.CellType` 枚举已有 3 种类型，命名按以下对应：

| `CellType` 枚举值 | Tile 命名前缀 | 说明 |
|---|---|---|
| `Soil` | `tile_soil_*` | 土块（可多变体：`surface` / `deep` / `<color>` 等） |
| `Empty` | **不需要 sprite** | 已挖空洞 —— 用 Camera clear color 黑色表达，无需贴图 |
| `Entrance` | **不再用 tile 命名**（TASK-042 后） | 入口已升级为多格大尺寸 sprite，独立 `Assets/Art/Entrances/`，命名 `entrance_*`；旧 `tile_entrance_*` 已删除 |
魔王不属于 `CellType`，不使用魔王房间 tile 命名；魔王相关资源按单位命名为 `demonlord_*`。

**Soil 多变体规则**：同一个 `Soil` 格子可以有多张视觉变体 sprite，用 `<variant>` 段区分。当前项目内 Soil variant 命名约定：

| 变体类型 | 文件名模式 | 示例 |
|---|---|---|
| 功能变体（按规则 / 区段选用） | `tile_soil_<function>_<index>` | 预留给未来明确的功能性土块，不再作为当前默认土块生成主路径 |
| 颜色主题集（程序化生成池） | `tile_soil_<color>_<index>` | `tile_soil_brown_00..15`、`tile_soil_dark_blue_00..15`、`tile_soil_dark_green_00..15`、`tile_soil_dark_purple_red_00..15` |

`GridRenderer` 可按行深度 / 随机 / 属性 / 主题选择变体，选用逻辑属于代码层，不影响命名规则。

未来如果新增 `Wall` 类型，命名前缀为 `tile_wall_*`。

---

## 四、Monster 命名映射本项目 Slime

当前唯一魔物是 Slime，命名前缀固定 `monster_slime_*`：

| 状态 | 文件名示例 |
|---|---|
| Idle（待机） | `monster_slime_idle_00.png` |
| Attack（攻击） | `monster_slime_attack_00.png` |
| Hurt（受击） | `monster_slime_hurt_00.png` |
| Death（消亡） | `monster_slime_death_00.png` |
| 动画序列 | `monster_slime_idle_sheet.png` |

---

## 五、Hero 命名映射本项目勇者

当前 `HeroData` 没有兵种区分，默认 `class` 用 `warrior`。未来扩展时按职业命名（`warrior` / `mage` / `archer` 等）。

| 状态 | 文件名示例 |
|---|---|
| Idle | `hero_warrior_idle_00.png` |
| Walk East（向右走） | `hero_warrior_walk_e_00.png` |
| Walk West | `hero_warrior_walk_w_00.png` |
| Attack | `hero_warrior_attack_00.png` |
| Death | `hero_warrior_death_00.png` |

---

## 六、红线与执行约束

1. **进入 Unity 工程的文件必须使用规范英文命名**
   - 所有落入 `Assets/Art/**` 的文件名必须符合本规则（snake_case + 类别前缀 + 状态后缀 + 帧序号）
   - **项目外暂存区**（`D:\Game Developer Tools\Game Art Drops\MyLord\`）不受本规则约束，保留美术原始命名
   - 重命名发生在"复制进入 `_Incoming/` 的同时"，即进入 Unity 项目那一刻就已是英文规范名
2. **重命名必须连 `.meta` 一起处理**
   - 直接 Rename：在 Unity Editor 内 / 通过 `manage_asset` 工具进行
   - 不要手动改 `.meta` 文件名（GUID 关联会断）
3. **不允许跳过命名规则**
   - 即使是临时测试资源，也按规则命名（可加 `_test_` 中缀，如 `monster_slime_test_00`）
4. **不允许 AI 自行决定命名**
   - 当资源无法直接套入上述模板时，AI 必须先向人类确认命名后再操作
5. **大小写敏感**
   - Windows 不区分但 git 区分。统一小写避免跨平台冲突

---

## 七、未涵盖事项

以下命名约定暂不规定，待真正需要时再补：

- 多语言文本资源（`txt_*` / `loc_*` 等）
- 音效 / 音乐（`sfx_*` / `bgm_*` 等，本项目 MVP 无音效）
- 后处理 Volume Profile
- Light2D / 阴影遮罩
- VFX Graph / Shader Graph 资源
- Sprite Atlas 切片命名策略（待 Sprite Sheet 接入时定）

（TASK-042 已补：`entrance_*` / `building_*` / `surface_*` / `veg_*` 模板，以及程序化生成池的 `<prefix>_<index>` 简化形式。）
