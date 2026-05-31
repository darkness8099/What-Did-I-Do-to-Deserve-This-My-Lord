# Surface Decoration Rules

本文件定义当前项目地表背景半程序化布置的设计规则、参数边界和系统定位。

配套文档：
- `ART_INTAKE_RULES.md`
- `ART_NAMING_RULES.md`
- `GAME_DESIGN_BASE.md`

---

## 1. 系统定位

当前地表背景系统是：
- **Editor 草稿生成工具**
- 用于快速产出一版“可人工微调”的地表装饰初稿
- 方便用户在 Scene 中继续手工拖拽、删改、补摆

当前地表背景系统不是：
- 运行时 Rogue-like 随机背景系统
- 自动追求最终美术定稿质量的系统
- 与地图核心逻辑、挖掘逻辑、寻路逻辑耦合的玩法系统

生成结果默认视为：
- **草稿**
- **不锁定**
- **允许人工微调后再决定是否保存为固定场景对象组或 Prefab**

---

## 2. 地表尺寸理解

| 项目 | 当前规则 |
|---|---|
| 地表区制作高度 | 顶部约 **10 格** |
| 地表区 y 范围 | ` [height-10, height) ` |
| 当前参考地图 | `70 x 50` |
| 单格像素基准 | `48 x 48 px` |

补充说明：
- 美术资源仍可按 **10 格高**制作。
- 但在当前视角里，**主要需要读清的装饰可见区应理解为地表线上方约 5~7 格**。
- 更上方的天空、远山、树冠顶部等内容主要是视觉缓冲，不是主要布置密度区。

---

## 3. Zone 划分

使用半开区间 `[start, end)`，宽度总计 70 格。

| Zone | 范围 | 作用 |
|---|---|---|
| A | `[0, 14)` | 左侧自然大地标区 |
| B | `[14, 27)` | 左中村庄建筑区 |
| C | `[27, 41)` | 中心入口主地标区 |
| D | `[41, 55)` | 右中村庄过渡区 |
| E | `[55, 70)` | 右侧高地标收尾区 |

默认入口中心仍约在 `x = 34`。

---

## 4. 类别权重原则

权重档位：
- `H` = high
- `M` = medium
- `L` = low
- `X` = forbidden

| 类别 | Zone A | Zone B | Zone C | Zone D | Zone E | 说明 |
|---|---|---|---|---|---|---|
| Entrance | X | X | 固定 1 个 | X | X | 仅中心入口区 |
| SurfaceObject | H | L | M | L | H | 大树、塔、风车等主体 |
| Building | L | H | X | H | M | 村庄建筑主体 |
| Prop | L-M | M | H | M | L-M | 小型补充装饰 |
| Vegetation | H | M | H | M | M | 全区域可布置，A / C 更高 |

---

## 4-1. 装饰生成高度

当前大背景自身已经包含地表线，第 10 格作为大背景地表时，装饰贴在第 10 格会过于接近地底边界，不利于人工微调。

当前默认规则：
- 装饰生成基线使用 `BackgroundLayerRenderer.decorationBaselineOffsetCells`
- 默认值为 `1`
- 等价于从地表背景底线向上 1 格生成，也就是当前 10 格地表背景中的第 9 格附近
- 该值只影响背景装饰草稿的垂直摆放，不修改地图核心、土块生成或玩法坐标

---

## 5. 背景层级规则

| Layer | sortingOrder | 用途 |
|---|---:|---|
| `BG_Base` | -100 | 大背景底图 |
| `BG_BackDeco` | -80 | 远景装饰预留层 |
| `BG_MidDeco` | -60 | 主体装饰：Entrance / SurfaceObject / Building |
| `BG_FrontDeco` | -40 | 前景小装饰：Props |
| `BG_TopDeco` | -30 | 最高背景装饰层：Vegetation |
| `Gameplay` | >= -10 | 土块、勇者、怪物、魔王 |

约束：
- 所有背景装饰层必须低于 Gameplay。
- Vegetation 当前是**最高背景装饰层**，但仍不得盖到玩法对象之上。

---

## 6. 占位宽度参考

这是 spawner 的粗略占位规则，不是精细美术规则。

| 类型 | 参考占位 |
|---|---|
| 大型树 / 大型 SurfaceObject | 8~12 格 |
| Watchtower / 中型地标 | 4~5 格 |
| Building | 4~6 格 |
| Entrance | 7~10 格 |
| Prop | 1~2 格 |
| Vegetation | 1 格 |

具体数值仍由 `SurfaceDecorationProfile` 负责。

---

## 7. 当前草稿生成目标

当前版本的草稿生成应满足以下方向：

1. 固定 70 格地表宽度。
2. Zone C 固定生成 1 个入口主体。
3. Zone A 至少生成 1 个大型自然主体。
4. Zone E 至少生成 1 个收尾主体。
5. Zone B / D 各生成 1~2 个建筑主体。
6. **Zone C 入口周围补到 8~12 个 Props / Vegetation。**
7. **全地图再补 25~40 个 Props / Vegetation。**
8. **每生成一个中大型主体（Entrance / SurfaceObject / Building），都在左右 1~3 格范围内补 2~5 个 Props / Vegetation。**
9. **若连续约 4 格没有任何装饰，则补 1 个小装饰。**
10. **若连续约 8 格没有中大型主体，则补 1 个中型主体或建筑，但不能破坏 Zone 权重规则。**
11. 同层内继续使用 footprint 防重叠；跨层允许叠放。
12. 结果仍然只作为草稿，由用户人工微调。

---

## 8. 实现边界

当前允许：
- 调整 `SurfaceDecorationSpawner` 的生成参数
- 增加草稿质量导向的轻量规则
- 调整 `SurfaceDecorationProfile` 中的默认权重和 footprint
- 调整背景层级映射

当前不做：
- 不修改地图核心逻辑
- 不修改土块生成逻辑
- 不改成运行时随机背景系统
- 不做自动美学评分
- 不要求程序直接生成最终可交付摆场

---

## 9. 与人工流程的关系

推荐工作流：

1. 用 Editor 工具生成地表草稿
2. 用户检查整体密度、主体节奏、入口周边阅读性
3. 用户人工微调位置、删除不合适项、补摆装饰
4. 最终再决定是否保存为固定场景对象组或 Prefab

当前 `BackgroundLayerRoot / BackgroundLayerRenderer` 的编辑器辅助按钮流程为：

1. `Randomize Seed`
2. `Generate Draft In Editor`
3. `Clear Generated Background`
4. `Save Current Background As Prefab`

Prefab 保存命名规则：
- 默认目录：`Assets/Prefabs/Backgrounds`
- 默认名称：`PF_Background_Surface_01.prefab` ~ `PF_Background_Surface_10.prefab`
- 保存时自动选择下一个未占用编号
- 达到最大数量后停止保存并输出 Warning，避免误覆盖已人工确认的背景 Prefab

游戏模式应用规则：
- `BackgroundLayerRenderer.loadRandomSavedPrefabOnStart` 默认开启
- Play Mode / 游戏流程开始时，不重新生成草稿背景
- 只从 `Assets/Prefabs/Backgrounds/PF_Background_Surface_01.prefab` ~ `10` 中随机选择一个已存在 Prefab 实例化
- 如果没有任何已保存背景 Prefab，输出 `LogError`
- 当前规则只服务 Unity Editor 内测试；正式 Build 的背景加载方式后续另行固化

说明：
- 不依赖 Play Mode 自动生成
- 不将地表背景系统作为运行时 Rogue-like 随机背景
- 当前更偏向“编辑器草稿生成 + 人工微调 + 再固化为 Prefab”

当前系统目标是：
- **提高草稿可用度**
- **减少明显空白和孤立主体**
- **不给用户制造大规模返工**

不是：
- 取代人工构图
- 取代最终摆场决策
