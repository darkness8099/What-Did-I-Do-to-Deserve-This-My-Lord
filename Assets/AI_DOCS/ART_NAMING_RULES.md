# ART_NAMING_RULES — 美术资源命名规则

本文档规定本项目美术资源的统一命名规范。
配套文档：`ART_INTAKE_RULES.md`（目录结构与接入流程）、`ART_INTAKE_LOG.md`（批次日志）。

---

## 一、核心原则

```
小写 + 下划线 + 类别前缀 + 状态后缀 + 序号
```

- 全部 **小写**
- 单词之间用 **下划线 `_`** 分隔
- 类别前缀（`tile_` / `hero_` / `monster_` / `ui_` / `bg_` / `fx_` 等）必须固定
- 状态后缀（`idle` / `walk` / `attack` 等）描述当前动作或变体
- 序号 **两位数字**（`00` / `01` / `02`），保留扩展空间

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
| **Tile** | `tile_<type>_<variant>_<index>` | `tile_soil_normal_00`、`tile_empty_glow_00`、`tile_wall_default_00`、`tile_entrance_default_00`、`tile_demonlordroom_default_00` |
| **Hero** | `hero_<class>_<state>_<index>` | `hero_warrior_idle_00`、`hero_warrior_walk_e_00`、`hero_warrior_attack_00` |
| **Monster** | `monster_<species>_<state>_<index>` | `monster_slime_idle_00`、`monster_slime_attack_00`、`monster_slime_death_00` |
| **DemonLord** | `demonlord_<state>_<index>` 或 `demonlord_room_<element>_<index>` | `demonlord_idle_00`、`demonlord_room_bg_00`、`demonlord_room_throne_00` |
| **UI Icon** | `ui_icon_<name>` | `ui_icon_hp`、`ui_icon_attack`、`ui_icon_magic` |
| **UI Panel** | `ui_panel_<name>` | `ui_panel_victory`、`ui_panel_defeat`、`ui_panel_pause` |
| **UI Button** | `ui_btn_<name>_<state>` | `ui_btn_restart_normal`、`ui_btn_restart_hover` |
| **UI Font** | `ui_font_<name>` | `ui_font_title`、`ui_font_body` |
| **Background** | `bg_<name>_<index>` | `bg_dungeon_00`、`bg_overworld_00` |
| **Prop** | `prop_<name>_<index>` | `prop_chest_00`、`prop_torch_00` |
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

本项目 `GridData.CellType` 枚举已有 4 种类型，命名按以下对应：

| `CellType` 枚举值 | Tile 命名前缀 | 说明 |
|---|---|---|
| `Soil` | `tile_soil_*` | 土块 |
| `Empty` | `tile_empty_*` | 已挖空洞 |
| `Entrance` | `tile_entrance_*` | 入口 |
| `DemonLordRoom` | `tile_demonlordroom_*` | 魔王房间（注意：单词连写，因为 enum 也是连写） |

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

1. **本规则不强制改名美术发来的原始包**
   - 原始包（无论在项目外 `D:\Game Art Drops\` 还是项目内 `_Incoming/`）**保留作者命名**
   - 重命名只在 `_Incoming/ → Art/<category>/` 这一步执行
   - 目的：保留对照原文件的能力，避免误删
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
