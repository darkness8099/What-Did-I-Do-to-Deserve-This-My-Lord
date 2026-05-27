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
          ├── 勇者到达魔王位置 → 失败
          └── 所有勇者被击败 → 胜利
```

---

## 第一阶段 MVP 内容

### 地图系统
- 网格尺寸：**32 列 × 18 行**
- 每个格子类型：`Soil`（土块）/ `Empty`（空洞）/ `Wall`（不可挖边界）
- 初始状态：全部为 Soil，边界为 Wall

### 挖掘系统
- 玩家点击 Soil 格子 → 变为 Empty
- 无挖掘消耗（第一阶段不计成本）

### 魔物系统
- 基础魔物类型：**Slime**（史莱姆）
- 生成条件：Empty 格子周围形成一定空间时按时间间隔生成
- 行为：原地等待，当勇者进入攻击范围时攻击

### 勇者系统
- 每隔若干秒从地图左侧入口生成一名勇者
- 勇者沿最短路径（BFS/Dijkstra）向魔王位置移动
- 魔王位置固定在地图右侧中央

### 胜负判定
- **失败条件**：任意勇者到达魔王格子
- **胜利条件**：波次内所有勇者被魔物击败

---

## 技术约束（第一阶段）

- 无美术资源：全部使用 Unity Primitive + 颜色区分
- 无音效
- 无 UI（仅最基础的胜负文字提示）
- 不做存档/读档
- 不做多关卡

---

## 当前规则（2026-05-27 TASK-027 更新）

### 勇者胜负判定（TASK-027 修改）

**旧规则（已废弃）：**  
~~勇者到达魔王位置 → 立即触发失败~~

**当前规则：**
```
勇者到达魔王位置（DemonLordRoom）
  → 切换为返回模式（HeroRouteState.ReturningToEntrance）
    → 沿最短路径返回入口（BFS，目标改为 Entrance）
      → 勇者到达入口 → 触发失败（Defeat）
      └── 若途中被魔物击败 → 该勇者消亡，继续正常胜负判定
```

| 条件 | 结果 |
|------|------|
| 任意勇者成功返回入口 | 失败（Defeat） |
| 所有勇者在旅途中被击败 | 胜利（Victory） |

**实现方式：**
- `HeroMover.cs`：增加 `HeroRouteState` 枚举（`GoingToDemonLordRoom` / `ReturningToEntrance`），`MoveHero` 协程在到达魔王位置后切换目标为 Entrance
- `MVPGameManager.cs`：`NotifyHeroReachedDemonLordRoom` 改为仅打印日志；新增 `NotifyHeroEscapedToEntrance` 方法触发 Defeat

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

## 正式规则方向（2026-05-27 起逐步推进）

### 土块属性系统

每个土块格子将携带两个属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `MagicPower` | int | 该土块蕴含的魔力值，0 表示普通土块 |
| `ElementType` | TileElementType | 对应元素类型（None / Slime / 未来扩展） |

**挖掘规则（正式版）：**
```
玩家点击 Soil 格
  → MagicPower == 0  →  格子变为 Empty，不生成魔物
  → MagicPower > 0 && ElementType != None  →  格子变为 Empty，并在原地生成对应魔物
```

**当前进度：**
- [x] `TileAttributeData.cs` 数据结构已实现（TASK-025）
- [x] `GridData` 已扩展 `GetTileAttribute` / `SetTileAttribute`（TASK-025）
- [ ] 挖掘时自动生成魔物逻辑（待 TASK-026）
- [ ] 地图初始化时为土块赋予随机属性（待后续）

**MVP 阶段过渡方案：**  
正式挖掘逻辑实装前，"点击 Empty 手动放 Slime"的临时交互暂时保留，
两种规则可并存测试，不互相影响。

---

## 后续阶段方向（暂不实现）

- 魔物培育/升级系统
- 多种勇者类型
- 资源/经济系统
- 正式美术资源替换
- 关卡编辑器
