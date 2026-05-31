# GAME_DESIGN_BASE — 第一阶段游戏设计文档

## 项目概述

- **游戏名称**：（暂定）What Did I Do to Deserve This, My Lord
- **参考作品**：《勇者のくせに生意気だ。》（PSP, 2007）
- **游戏类型**：2D 挖掘防守游戏（Dungeon Defense / Dig & Defend）
- **引擎**：Unity 2022.3.58f1 / URP 2D
- **开发目标**：验证纯 AI 工作流能否完成完整可玩游戏制作，并沉淀为成熟流程

---

## 核心玩法循环

```
挖地（玩家操作）
  → 魔物在空洞中产生
    → 勇者从入口入侵
      → 魔物尝试阻止勇者
        → 胜负判定
          ├── 勇者抓住魔王并带回入口 → 失败
          └── 所有勇者被击败 → 胜利
```

---

## 第一阶段 MVP 内容

### 地图系统
- 当前测试网格尺寸：**70 列 × 50 行**（宽度对齐当前地表背景 70 格规划；高度提升到正式测试尺寸，地表区约为顶部 10 行）
- 每个格子类型：`Soil`（土块）/ `Empty`（空洞）/ `Entrance`（入口）
- 入口连接点默认位于地图中间列、从上往下第 **10** 格；该格属于地下世界入口连接的一部分，视觉上按黑色空洞显示，不再使用旧测试入口绿色格或旧入口素材。
- 顶部地表背景中的可见入口美术后续由背景 / 入口系统表现；勇者未来会从地表走向该入口美术，再进入地下入口连接点。
- 入口下方默认保留 2-3 格固定空洞，魔王放置在该空洞中等待。
- 地下生成起点由地表背景高度决定：顶部 **10 行** 作为背景/地表区留空，从**上往下第 11 格**开始进入地下土层；`GridManager` 按 `LevelConfig.surfaceBackgroundRows` 生成土块。
- 魔王不是地图格子，也不是房间 tile，而是一个特殊单位。

### 挖掘系统
- 玩家点击 Soil 格子 → 变为 Empty
- 当前挖掘合法性：目标 Soil 的上下左右四邻中，至少有一格是 `Empty` 或 `Entrance` 才允许挖掘。
- 初始入口下方空洞必然连通入口；后续每次挖掘都只能从已有道路向外扩展，因此暂不做 BFS 连通性检查。
- **地下表层（入口下方紧邻的一行，由 `LevelConfig.IsSurfaceLayer(y)` 判定）** 仍作为地表与地下的视觉分界线，但**不再额外禁止点击/挖掘**。当前点击无效区域仅来自顶部 10 行背景区本身不参与玩法网格；表层视觉已并入新的 `tile_soil_<color>_<index>` 土块主题体系。
- 无挖掘消耗（第一阶段不计成本）

### 魔物系统
- 基础魔物类型：**Slime**（史莱姆）
- 生成条件：Empty 格子周围形成一定空间时按时间间隔生成
- 行为：原地等待，当勇者进入攻击范围时攻击

### 勇者系统
- 默认等待 10 秒后进入魔王重新放置流程；魔王开局已存在，流程开始时视为抓起当前魔王，玩家左键选择任意 `Empty` 格完成转移后，勇者才从入口房间生成。
- 延迟时间由 `LevelConfig` 配置，后续 UI / 波次系统完善后再接管。
- 勇者沿最短路径（BFS/Dijkstra）向魔王单位移动
- 当前测试阶段魔王默认位置由 `LevelConfig` 推导；倒计时结束后允许玩家把当前魔王转移到任意 Empty 格，再开始勇者流程。

### 胜负判定
- **失败条件**：任意勇者抓住魔王并带回入口
- **胜利条件**：波次内所有勇者被魔物击败

---

## 技术约束（第一阶段）

- 无音效
- 无 UI（仅最基础的胜负文字提示）
- 不做存档/读档
- 不做多关卡

### 视窗与输入（当前规则）

**16:9 gameplay viewport（TASK-041 固化，参考 PSP 原作局部视野）：**

- 目标可见范围：**约 28 列 × 16 行 土块**。
- 可接受范围：横向 **27–30 格** / 纵向 **16–18 格**。
- 1 土块 = 1 Unity Unit；推导 `Camera.orthographicSize = CameraViewRows ÷ 2 ≈ 8`。
- 实现：`LevelConfig.CameraViewRows = 16`，`InputHandler.ApplyInitialCameraView` 在 Start 推导 ortho size 并覆盖 Camera Inspector 值。Inspector 上的 `orthographicSize` 仅作 Edit Mode 占位，Play Mode 总会被覆盖。
- **必须保持 Game View aspect = 16:9**（编辑器 Game View 面板顶部 Aspect 下拉）。超宽屏（21:9）/ 现代手机比例不应被用来扩大可操作视野；多余宽度未来留给 UI / 背景 / 安全边距。
- 不一次显示完整 64×50（未来地图）。必须靠玩家挖掘 + 视角拖动逐段展开。

**输入：**
- 鼠标左键：挖掘土块；`PlacingDemonLord` 阶段（TASK-038 流程）期间改为放置魔王。
- 鼠标右键按住拖动：平移视角。

---

## 当前规则（2026-05-27 TASK-027 更新）

### 勇者胜负判定（TASK-027 修改）

**旧规则（已废弃）：**  
~~勇者到达魔王位置 → 立即触发失败~~

**当前规则：**
```
勇者接近魔王单位
  → 捕获魔王，切换为返回模式（HeroRouteState.ReturningToEntrance）
    → 沿最短路径返回入口（BFS，目标改为 Entrance）
      → 勇者到达入口 → 触发失败（Defeat）
      └── 若途中被魔物击败 → 该勇者消亡，继续正常胜负判定
```

| 条件 | 结果 |
|------|------|
| 任意勇者成功返回入口 | 失败（Defeat） |
| 所有勇者在旅途中被击败 | 胜利（Victory） |

**实现方式：**
- `GridManager.cs`：当前测试阶段临时保存魔王单位起始坐标；后续由玩家放置流程替代
- `HeroMover.cs`：使用 `HeroRouteState` 枚举（`GoingToDemonLord` / `ReturningToEntrance`），勇者寻路到魔王单位相邻可通行格，捕获后切换目标为 Entrance
- `HeroRenderer.cs`：运行时创建测试用魔王单位视图；捕获后移除原视图并创建跟随勇者的 CaptiveDemonLord 视图
- `MVPGameManager.cs`：`NotifyHeroReachedDemonLord` 仅打印日志；`NotifyHeroEscapedToEntrance` 触发 Defeat

**魔王单位规则（当前方向）：**
- 魔王是特殊单位，不参与战斗，不能被击杀。
- 当前坐标仅为测试用固定值，后续由玩家放置。
- 魔王不占用 `CellType`，地图上不存在魔王房间格。

---

## 系统职责边界（TASK-032 更新）

| 系统 | 负责 | 不负责 |
|------|------|--------|
| `GridData` | 保存格子类型与土块属性数据 | 单位生命周期、渲染、输入 |
| `GridManager` | 地图格子权威入口：土块、空洞、入口、土块属性、挖掘与地形查询 | 魔王位置、勇者状态、魔物生命、关卡测试配置 |
| `LevelConfig` | 当前关卡/测试局的初始配置：地图尺寸、入口、魔王测试坐标、测试土块属性点 | 运行时规则、单位状态 |
| `DemonLordManager` | 魔王特殊单位的位置与捕获状态 | 地图格子状态、战斗 |
| `DemonLordRenderer` | 魔王单位与被捕获魔王的显示 | 勇者显示、魔王规则 |
| `HeroManager` | 勇者数据、位置与生命周期 | 魔王数据、地图属性 |
| `HeroMover` | 勇者路线状态、移动与捕获/返程流程 | 魔王数据所有权、格子数据写入 |
| `MonsterManager` | 魔物数据、占位、生成与移除 | 土块属性判定、水流规则 |
| `InputHandler` | 玩家输入坐标转换与命令转发 | 挖掘后果、生成魔物、刷新渲染 |
| `DigActionHandler` | 玩家挖掘命令的执行编排：挖掘、刷新格子、按土块属性生成魔物 | 鼠标输入、单位 AI |

未来水流扩展方向：水量、湿度、压力等数据可以进入 Grid 体系；持续扩散、流速、喷涌等复杂模拟应由独立 `WaterFlowManager` / `GridEnvironmentManager` 处理，暂不在当前阶段实现。

---

## 旧规则记录（2026-05-27 TASK-026 更新）

**旧规则（已废弃）：**  
~~点击 Soil → 挖成 Empty；点击 Empty → 手动放置 Slime~~

**当前规则：**
```
点击 Soil → 挖成 Empty
  └── 若该格 TileAttributeData.CanSpawnMonster() == true
        └── ElementType == Slime → 自动生成 Slime

点击 Empty → 无操作（Debug.Log 记录）
```

**MVP 阶段测试配置（临时）：**  
GridManager.Awake() 在 y=9 行的 x=6/10/14/18/22 预设 MagicPower=1、ElementType=Slime 的土块属性，用于验证自动生成逻辑。后续将替换为正式地图配置或资源数据驱动。

---

## 正式规则方向（2026-05-30 TASK-037 更新：生态化重构）

### 土块属性系统（双资源轴）

每个 **Soil** 格子（仅 Soil，Empty / Entrance 不持有资源）携带：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Nutrient` | int | 养分轴：低阶到中阶生物链的基础资源 |
| `Magic` | int | 魔分轴：稀有、偏魔法系生态的资源（原 `MagicPower` 改名） |
| `ElementType` | `TileElementType` | 生成倾向标签（None / Slime / 未来扩展） |

读写守卫：`GridManager.GetTileAttribute / SetTileAttribute` 在非 Soil 格子上读返回 `Default`、写打 Warning 并拒绝。

### 土块养分外观规则（TASK-053）

土块外观仍然只表现 `CellType.Soil`。不同 sprite 不是不同玩法类型，也不是独立 Prefab / 继承类；是否可挖仍统一由 `CellType.Soil` 与四邻连通规则决定。

当前每套 Soil 主题有 16 张图，index `0..15` 由 `TileAttributeData.Nutrient` 决定：

| Nutrient | visual index | 生态等级 |
|---:|---:|---:|
| `0` | `0` | 1 |
| `1-10` | `1-5` | 1 |
| `11-20` | `6-10` | 2 |
| `21+` | `11-15`，继续增长时最高停在 `15` | 3 |

- `0-5` 图没有花等外观装饰，定义为 1 级土块；可作为 1 级养分魔物（当前 Slime）的生成来源。
- `6-10` 为 2 级土块表现。
- `11-15` 为 3 级土块表现。
- 持续增加的养分只继续增加数值，不再让外观超过 index `15`。
- 当前 `LevelConfig.ApplyInitialSoilAttributes()` 会为所有 Soil 填入可重复的测试养分分布；这只是 TASK-053 接入外观映射时的临时实现，不代表正式关卡生成规则。

### 初始养分生成规则（TASK-054）

初始地图养分不应平均分布，也不应在普通初始地图中大量自然生成高阶养分。地下土块以低级养分底噪为基础，并叠加多个 Lv1 团簇；贫瘠区仍存在，但早期关卡不应被理解为只有少数孤立团簇。高级养分主要由玩家经营生态后，通过魔物繁殖、死亡、捕食、生命周期转化等循环逐步产生。

正式生成方向：

- 默认地下 Soil 可有较高覆盖率的低级底噪，值通常为 `1~3`。
- 根据关卡阶段生成若干 `NutrientCluster`。
- 每个团簇包含 `center`、`radiusX`、`radiusY`、`power`、`falloff`、`density`。
- 团簇是概率型椭圆区域：范围内只是提高养分出现概率，不是每格必定赋值。
- 越靠近中心，出现概率与高值概率越高；边缘允许破碎、空洞、低高养分混杂。
- 团簇之外可以是贫瘠区或低级底噪区。
- Lv2 / Lv3 种子点必须依附已有 nutrient cluster，优先出现在中心或高密度区域附近，可替换已有 Lv1 格子，但不得孤立出现在大片 0 养分区。
- 最后按当前关卡限制裁剪 `maxInitialNutrient`。

阶段规则：

| 阶段 | 初始养分方向 | Lv2 初始出现 | Lv3 初始出现 |
|---|---|---|---|
| Stage 1 | 较高覆盖率低级底噪 + 多个重叠 Lv1 团簇 | 极少量种子点 | 0 |
| Stage 2 | Lv1 团簇数量或范围略增加 | 极少量种子点 | 0 |
| Stage 3+ | Lv1 / Lv2 初始资源逐渐增加 | 可增加但仍受控 | 非常克制，主要由生态循环产生 |

16 阶段养分 tile 语义：

| visual index | 语义 | 使用阶段 |
|---:|---|---|
| `tile_00` | 0 养分，普通土 | 贫瘠区 / 默认初始土 |
| `tile_01` ~ `tile_05` | Lv1，草 / 苔藓 / 小芽阶段 | 早期关卡主要使用 |
| `tile_06` ~ `tile_10` | Lv2，小花阶段 | 中后期初始种子或生态成长反馈 |
| `tile_11` ~ `tile_15` | Lv3，大花 / 繁花阶段 | 成熟生态区域，普通初始地图应少见 |

后续实现建议拆为小任务：养分值到 `tile_00` ~ `tile_15` 的视觉映射复核，以及生态系统动态改变 Soil 养分后刷新外观。

当前实现状态（TASK-056）：

- 已新增 `StageNutrientProfile` / `NutrientClusterSettings`。
- `LevelConfig.ApplyInitialSoilAttributes()` 已改为调用团簇式 `GenerateInitialNutrient()`。
- 默认 profile 为 Stage 1：较高覆盖率低级基础散布 + 多个重叠 Lv1 团簇 + 极少量 Lv2 种子点，不生成 Lv3 初始种子点。
- 当前 70x50 默认地图使用 10 个概率型椭圆 Lv1 团簇，并叠加约 35% 的低值散布层，散布值为 `1~3`。
- `LevelConfig.initialNutrientSeed = 0` 时，每次初始化自动生成随机 seed，同一 Stage 1 规则下分布位置会变化；填入非 0 seed 时，分布可复现。
- 如果 Inspector 中存在一个空的 `StageNutrientProfile`（无团簇、无基础散布、无种子点），视为未配置，仍回退到默认 Stage 1 配置。
- `nutrient > 0` 且 visual index 不超过 Lv1 范围时，才写入 `TileElementType.Slime` 生成倾向。
- 后续如需 Stage 2 / Stage 3+，应通过 profile 调整 `maxInitialNutrient`、`clusters` 与种子点数量，而不是恢复全图平均测试分布。

### 挖掘规则（正式版）

```
玩家点击 Soil 格
  → IsDiggable(x, y) 通过（四邻有路）
  → 读 attr，DigCell 改 cell 为 Empty
  → if attr.CanSpawnMonster():
      → 按 ElementType 选 archetype（Slime → MonsterArchetype.Slime）
      → PlaceMonster + monster.AbsorbFromTile(ref attr)
      → attr.ElementType = None（消费 spawn 倾向）
  → if attr.HasResource()（仍有残余 Nutrient/Magic）:
      → ResourceFlow.ScatterDigLeftoverResources 扩散到周围 Soil（r=1→2→3 chebyshev），都没找到则进 FloatingResourcePool
```

挖完 cell 已是 Empty，不再写回 attr —— 资源**只能存在于 Soil**。

### 怪物生态身份（Ecology Role）

`MonsterEcologyRole` 枚举：`None / Carrier / Consumer / Predator / Magical / Support / Apex`。  
`MonsterMoveStrategy` 枚举：`Static / RandomWalk / WallFollow / SeekResource / SeekFood / Flee`（字段预留，行为未实装）。

身份单一来源：`MonsterArchetype`（plain class，未来可迁 ScriptableObject），由 `MonsterArchetypeRegistry` 按 `Id` 字符串注册查找。Prefab Root 挂 `MonsterIdentity` 组件填 `archetypeId`，运行时 `Resolve()` 反查 archetype。

| Archetype | Role | Move | HP | Atk | NutrientCap | MagicCap | SpawnElement |
|---|---|---|---|---|---|---|---|
| `Slime` | Carrier | Static | 10 | 2 | 5 | **0** | Slime |

> 基础史莱姆为养分系，`MagicCapacity = 0`（用户决定的设计基线，TASK-037 F6）。

### 资源携带（怪物身上）

`MonsterData.CurrentNutrient` / `CurrentMagic` —— spawn 时为 0，`AbsorbFromTile(ref tile)` 在挖掘时填充至 `archetype.*Capacity` 上限。`Hunger` 字段预留，本轮不参与逻辑。

### 死亡原因与普通死亡回流（Death Cause / Death Return）

`DeathCause` 当前最小分类：`HeroKill / PredatorEat / NaturalDecay / Starvation / LifecycleTransform / LifecycleWither / EnvironmentDeath / Unknown`。

普通非捕食死亡回流只允许以下原因触发：
- `HeroKill`
- `EnvironmentDeath`

当前 `CombatSystem` 勇者击杀怪物时，在 `RemoveMonster` 之前：
```
ResourceFlow.ScatterOrdinaryDeathResources(deathPos, monster, gm, DeathCause.HeroKill, "<name>")
```
算法：从死亡格起按 chebyshev 半径 r=1→2→3 找 Soil 环；首个非空环平均分发 Nutrient / Magic（余数给前几格）；都没找到则进 `FloatingResourcePool`（系统级游离资源缓冲）。

禁止把所有 `HP <= 0` 统一接到死亡回流。捕食死亡、史莱姆自然衰弱、生命周期转化、花枯萎 / 繁殖结算均不得默认调用普通死亡散布。捕食应优先把猎物携带资源转移给捕食者；生命周期事件应进入各自策略；Empty 仍不是资源容器。

### 捕食资源转移（Predation Resource Transfer）

捕食是资源向生态链上层转移，不是普通死亡散布。

当前最小 API：
```
ResourceFlow.TransferResourcesToPredator(prey, predator, reason)
```

规则：
- 从猎物 `CurrentNutrient / CurrentMagic` 抽出资源。
- 捕食者按 `NutrientCapacity / MagicCapacity` 剩余容量接收。
- 捕食者装不下的剩余资源进入 `FloatingResourcePool`，后续可由蘑菇 / 游离资源系统另行处理。
- `PredatorEat` 不调用 `ScatterOrdinaryDeathResources`，也不写入 Empty Tile。
- 本轮只提供数据 API，不实现咬咬虫 AI、史莱姆生命周期、花苞、蘑菇。

> 设计参考：[yuunama wiki 怪物数据循环](https://wikiwiki.jp/yuunama/%E3%83%A2%E3%83%B3%E3%82%B9%E3%82%BF%E3%83%BC%E3%83%87%E3%83%BC%E3%82%BF)。

### 旧 `MonsterType` enum 状态

标 `[System.Obsolete]`，代码内 0 实际引用，保留以避免未来外部脚本（若有）编译断裂。后续 cleanup 任务再删。

### 本轮**未做**

- 移动 AI（`MoveStrategy` 仅字段）
- 饥饿消耗（`Hunger` 仅字段）
- Monster vs Monster 捕食
- Renderer 改实例化 Prefab 模式（继续走运行时 `new GameObject + SpriteRenderer`，TASK-029E 时一起改）
- Debug overlay / 血条 / UI
- ScriptableObject 数据资产（plain class + registry 够用）

---

## 后续阶段方向（暂不实现）

- 魔物培育/升级系统
- 多种勇者类型
- 资源/经济系统
- 正式美术资源替换
- 关卡编辑器
