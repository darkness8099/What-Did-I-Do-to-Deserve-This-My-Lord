# SURFACE_DECORATION_RULES — 地表背景半程序化布置规则

本文档规定本项目地表背景（地图顶部 10 行）半程序化布置的设计与规则边界。
配套：`ART_INTAKE_RULES.md`（资产入库）、`ART_NAMING_RULES.md`（命名）、`GAME_DESIGN_BASE.md`（视窗 / 玩法基线）。

---

## 一、文档定位

### 本文档是什么

- 规定**地表区域**（地图顶部 10 行）背景与装饰的半程序化布置方式
- 规定区域划分、类别权重、视觉层级、占位宽度等设计参数
- 给"装饰系统"的最小实现留接口与边界

### 本文档不是什么

- 不是完整代码规范（具体 API 留给实现任务）
- 不是数值平衡表
- 不规定单个素材的美感判断（程序化生成的结果是草稿，人工微调权归用户）
- 不限制后续扩展（区域划分可能因 Level / Theme 变化）

---

## 二、地图尺寸基准

| 项 | 值 |
|---|---|
| 地表区宽度 | **70 格** |
| 每格像素 | 48 px |
| 总宽 | 3360 px |
| 地表区高度（顶部 N 行） | **约 10 格** |
| 总高（地表区） | 480 px |
| 地表区 y 范围 | `[height-10, height)`，即假设 height=50 时 `[40, 50)` |
| 核心玩法 tile 尺寸 | 仍以 **48×48 px** 为基准（PPU=48） |
| 装饰素材尺寸 | 不要求 48×48 整除，但导入按 `ART_INTAKE_RULES § 四` 像素素材规则（PPU=48 / Point / Uncompressed / Pivot 见入库规则） |

---

## 三、地表区域划分（Zone A ~ E）

**坐标约定**：使用半开区间 `[start, end)`（end 不含），x 从左到右递增。总宽 70 格。

| Zone | 范围 (x) | 宽 | 主要类别（按权重） | 设计作用 |
|---|---|---|---|---|
| **A 左侧自然大地标区** | `[0, 14)` | 14 格 | SurfaceObjects（高）/ Vegetation（高）/ Props（中-低） | 形成左侧自然视觉锚点 |
| **B 左中村庄建筑区** | `[14, 27)` | 13 格 | Buildings（高）/ Props（中）/ Vegetation（中） | 形成村庄生活感，连接自然区与入口区 |
| **C 中心入口主地标区** | `[27, 41)` | 14 格 | Entrances（固定 1 个）/ Props（高）/ Vegetation（高）/ SurfaceObjects（中） | 固定生成地牢/遗迹/山洞入口，地表主视觉与玩法语义中心 |
| **D 右中村庄过渡区** | `[41, 55)` | 14 格 | Buildings（高）/ Props（中）/ Vegetation（中）/ SurfaceObjects（低） | 延续村庄元素，平衡中心入口后的视觉节奏 |
| **E 右侧高地标收尾区** | `[55, 70)` | 15 格 | SurfaceObjects（高）/ Buildings（中）/ Props（低-中）/ Vegetation（中） | 风车 / 塔 / 高建筑等收尾地标，避免右侧空白 |

**入口默认中心**：Zone C 内约 `x ≈ 34`（Zone C 中段）。具体宽度按选用的 entrance sprite 占位（7~10 格）。

---

## 四、类别权重原则（汇总表）

权重档：**高 = H**、**中 = M**、**低 = L**、**禁止 = ✗**。

| 类别 | Zone A | Zone B | Zone C | Zone D | Zone E | 全图说明 |
|---|---|---|---|---|---|---|
| **Backgrounds** | — | — | — | — | — | 只作为底图 / 远景，**不参与主要随机摆放** |
| **Entrances** | ✗ | ✗ | **固定 1 个** | ✗ | ✗ | 只在 Zone C 生成，每次只生成 1 个主入口 |
| **SurfaceObjects** | **H** | L | M | L | **H** | A / E 锚点优先 |
| **Buildings** | L | **H** | **✗（尽量避免）** | **H** | M | 入口区不放建筑遮挡视觉 |
| **Props** | L-M | M | **H** | M | L-M | C 区氛围最丰富 |
| **Vegetation** | **H（最高）** | M | **H** | M | M | 全区域可生成，A 最高，C 高 |

权重的具体数值（如 H=3, M=2, L=1）由 `SurfaceDecorationProfile` 实现时决定，不在规则里钉死。

---

## 五、视觉层级（Sorting Order）

地表背景分 4 层 + 1 层玩法对象。Unity SpriteRenderer 按 `sortingOrder` 由低到高渲染：

| Layer | sortingOrder | 内容 | 备注 |
|---|---|---|---|
| `BG_Base` | **-100** | 大背景底图（`bg_overworld_00` 等） | 最底，整张铺 70×10 |
| `BG_BackDeco` | **-80** | 远景装饰（远山轮廓、远景树等） | 当前批次无对应素材，预留 |
| `BG_MidDeco` | **-60** | 主装饰：房屋、大树、入口、塔、风车 | SurfaceObjects / Buildings / Entrances 默认层 |
| `BG_FrontDeco` | **-40** | 前景小装饰：草丛、石头、木桶、栅栏、小花 | Props / Vegetation 默认层 |
| `Gameplay` | **-10 及以上** | 土块（GridRenderer，-10）/ 怪物 / 勇者 / 魔王 | 现有约定，不变 |

**约束**：
- 所有 BG 层的 sortingOrder 必须严格 < Gameplay 层
- 同层内允许按 Y 坐标二次排序（Unity `Transparency Sort Mode = Custom Axis (0,1,0)`）让靠下的物件遮挡靠上的——这是后续实现的可选优化，本规则不强制

---

## 六、占位宽度参考（footprint）

仅作 spawner 防重叠的粗略参考。装饰素材实际像素宽不一定整除 48；占位宽度按"近似格数"理解：

| 物件 | 占位宽度（格） | 默认类别 / sprite 前缀 |
|---|---|---|
| 大树 | 8 ~ 12 | `surface_tree_*` 大型 |
| 风车 | 4 ~ 6 | `surface_windmill_*`（未来） |
| 瞭望塔 | 4 ~ 5 | `surface_watchtower_*` |
| 房屋 / 教堂 / 商店 | 4 ~ 6 | `building_*` |
| 地牢入口 | 7 ~ 10 | `entrance_*` |
| 小树 | 2 ~ 3 | `surface_tree_*` 小型 |
| 石头堆 | 1 ~ 2 | `prop_*` |
| 草丛 / 木桶 / 箱子 / 路牌 | ~1 | `prop_*` / `veg_*` |
| 栅栏 | 1 ~ 3 | `prop_*` |

具体每张 sprite 的占位宽度由 `SurfaceDecorationProfile` 按 sprite asset 单独配置，可缺省按类别推。**不在本规则里逐张列举**。

---

## 七、第一版半程序化生成目标

按优先级列出，**实现时按顺序对齐**：

1. 固定地表长度 **70 格**
2. 入口固定在 Zone C，默认中心约 `x = 34`
3. 从 `Assets/Art/Entrances/` 随机选 1 个 `entrance_*`
4. Zone A 随机放 **1 个**大型自然地标（`surface_tree_*` 大型 / 或大岩石）
5. Zone E 随机放 **1 个**高地标 / 收尾地标（`surface_watchtower_*` / `building_*` 高 / `surface_tree_*`）
6. Zone B / Zone D 各随机放 **1~2 个**建筑（`building_*`）
7. Zone C 入口周围随机放 **3~6 个** Props / Vegetation
8. 全地图空隙随机撒 **10~20 个** Props / Vegetation
9. 简单占位宽度防重叠（同 layer 内不允许 footprint 重叠，跨 layer 允许）
10. 生成结果作为草稿（**不锁定**），允许用户在 Scene 中拖拽微调

**约束**：
- 不在 Gameplay 层生成任何对象（地表装饰不影响挖掘 / 寻路）
- 不修改 `GridData` / `GridManager` / 资源系统
- 不参与 Hero / Monster / DemonLord 流程

---

## 八、后续功能规划（仅设计，不在本轮实现）

| 抽象 | 性质 | 职责 | 不做 |
|---|---|---|---|
| **`SurfaceDecorationProfile`** | ScriptableObject 或 plain class | 承载：Zone 边界、类别 × Zone 权重表、每 sprite 占位宽度、各类别可用 sprite 清单 | 不承载生成逻辑 |
| **`DecorationPlacementData`** | plain struct / class | 单条记录：`spriteId`、`gridPosition (Vector2Int)`、`category`、`footprintWidth`、`sortingLayer` | 不持有运行时 GameObject 引用 |
| **`SurfaceDecorationSpawner`** | MonoBehaviour | 读 Profile + 随机种子 → 输出 `List<DecorationPlacementData>` → 实例化到对应 BG 层；支持清除草稿、按当前种子重生 | 不写 Scene 文件 / 不直接持久化 |
| **随机种子** | `int seed` 字段 | 同一 seed 产生相同布局，方便美术调试 / 回溯 | 不固定为某个 magic number |
| **草稿 → Prefab / 场景对象组** | 后续选项 | 把生成结果烘焙为 `PF_Surface_Decoration_<level>` Prefab 或挂场景节点 | 第一版不烘焙，每次 Play 重生即可 |

实现拆分为后续 TASK（见 TASKS.md）。

---

## 九、AI 边界 / 不做事项

明确**不要 AI 做**的事项：

- **不细分小物件美感判断**——不要求 AI 区分木箱、路牌、栅栏、蘑菇、草丛的具体摆放美感
- **不为小物件建立过细规则**——避免无意义复杂度和额外 token 消耗
- **不要求完全自动化美观**——程序生成是"可人工微调的草稿"
- **不主动建议视觉调整**——用户保留全部人工调整权
- **不引入 AI 美学评分**——本系统不调用任何模型对生成结果打分

人工调整后是否要把调整结果反哺成新 Profile / Prefab，是后续 TASK 决策，不在本规则里钉死。

---

## 十、与其他规则文档的关系

| 文档 | 关系 |
|---|---|
| `ART_INTAKE_RULES.md` § 一 | 装饰素材已在 `Backgrounds/` `Buildings/` `Entrances/` `Props/` `SurfaceObjects/` `Vegetation/` 目录就位（TASK-042 完成） |
| `ART_INTAKE_RULES.md` § 四 | Pivot：地表装饰物默认 `BottomCenter`，本规则不重复 |
| `ART_NAMING_RULES.md` § 二 | 装饰素材命名模板（`building_*` / `entrance_*` / `surface_*` / `veg_*` / `prop_*`），本规则按既有前缀引用 |
| `GAME_DESIGN_BASE.md` 视窗段 | 16:9 viewport，约 28×16 格可见。装饰可见性必须考虑相机拖动后的边界 |
| `LevelConfig` | 地表区高度（默认 10 行）与入口列定义；装饰系统只读取，不改写 |
| `UNITY_MCP_RULES.md` § 三 | AI 不自动 save Scene；草稿对象由用户决定是否保存 |

---

## 十一、未涵盖事项（暂不规定）

- 多关卡 / 多 Theme 的 Profile 切换策略（待第二关出现时定）
- 日夜系统对装饰可见性的影响
- 装饰与玩家挖掘的交互反馈（如挖到入口正下方土块时入口动画）
- 装饰素材的动画 / 粒子触发规则（如风车旋转）
- 移动端性能预算（draw call 数量上限）

---

*本规则在出现明显反例 / 玩法需求变化 / 多关卡接入时复审更新。*
