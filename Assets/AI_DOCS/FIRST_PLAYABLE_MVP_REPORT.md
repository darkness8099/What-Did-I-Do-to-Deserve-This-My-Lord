# FIRST PLAYABLE MVP REPORT

**项目**：What Did I Do to Deserve This, My Lord  
**引擎**：Unity 2022.3.58f1 / URP 2D  
**完成日期**：2026-05-27  
**任务跨度**：TASK-000 → TASK-022  

---

## 一、MVP 核心闭环

从空项目（仅 SampleScene）出发，实现以下完整可运行闭环：

```
玩家点击 Soil  →  格子变为 Empty（黑色）
玩家点击 Empty →  放置 Slime（黄色方块）
Hero 从入口 (0,9) 生成，BFS 寻路向魔王间 (31,9) 逐格移动
Hero 进入 Slime 所在格 → 即时战斗结算（互相扣血，HP 归零消灭）
  ├── 所有 Hero 被击败 → Victory → 屏幕中央显示黄色 VICTORY
  └── Hero 到达魔王间  → Defeat  → 屏幕中央显示红色 DEFEAT
```

---

## 二、已实现系统

### 数值基线

| 角色 | HP | ATK | 备注 |
|------|----|-----|------|
| Hero | 30 | 3 | MoveSpeed = 2 格/秒 |
| Slime | 10 | 2 | 攻击范围 = 1 格 |

每只 Slime 对 Hero 造成 6 点伤害（Hero 先攻，需 4 轮击杀 Slime，Slime 在此期间反击 2 次）。  
**需要 5 只 Slime 才能击败 1 名 Hero**（HP 消耗路径：30→24→18→12→6→0）。

### 脚本清单（14 个，全部挂载在 GridManager GameObject）

| 脚本 | 层次 | 职责 |
|------|------|------|
| `GridData.cs` | 数据层 | 32×18 网格状态、CellType 枚举、IsInside() |
| `GridManager.cs` | 管理层 | 初始化网格、DigCell()、GetCellType() |
| `GridRenderer.cs` | 表现层 | 576 个 Quad Tile，颜色区分 CellType |
| `InputHandler.cs` | 交互层 | 鼠标点击 → 网格坐标转换，挖掘/放 Slime 分发 |
| `MonsterData.cs` | 数据层 | Slime 属性（HP/ATK/Range）|
| `MonsterManager.cs` | 管理层 | PlaceSlime / HasMonster / RemoveMonster |
| `MonsterRenderer.cs` | 表现层 | 运行时创建/删除 Slime Quad GameObject |
| `HeroData.cs` | 数据层 | Hero 属性（HP/ATK/Speed）|
| `HeroManager.cs` | 管理层 | Spawn / GetHero / SetPosition / RemoveHero |
| `HeroPathfinder.cs` | 逻辑层 | BFS 寻路，直接引用 GridData（挖掘后自动感知新路径）|
| `HeroRenderer.cs` | 表现层 | 运行时创建/删除 Hero Quad GameObject |
| `HeroMover.cs` | 交互层 | 协程驱动逐格平滑移动，到达魔王间触发 Defeat |
| `CombatSystem.cs` | 逻辑层 | Hero 先攻即时战斗，HP 归零触发消灭 |
| `MVPGameManager.cs` | 状态层 | 胜负状态机：Playing / Victory / Defeat |
| `MVPResultUI.cs` | 表现层 | 运行时创建 ScreenSpaceCamera Canvas + Text |

### 运行时场景层级（Play Mode）

```
Scene Root
├── GridManager          ← 持久场景对象，含上述所有 14 个组件
├── Main Camera
├── Directional Light
├── [Runtime] GridTiles           ← GridRenderer 创建，576 Quad
├── [Runtime] MonsterViews        ← MonsterRenderer 创建
├── [Runtime] HeroViews           ← HeroRenderer 创建
└── [Runtime] ResultCanvas        ← MVPResultUI 创建（ScreenSpaceCamera）
    └── ResultText                ← UI.Text，64pt，全屏居中
```

退出 Play Mode 后，所有运行时对象自动清除，Edit Mode 无残留。

---

## 三、端到端测试结果（TASK-022）

### 测试 A：Defeat 流程

| 步骤 | 结果 |
|------|------|
| 挖通 y=9 整行（30 格） | ✓ 格子变黑 |
| 不放 Slime，等待 Hero 移动 | ✓ Hero 沿路径移动 |
| Hero 到达 (31,9) | ✓ State=Defeat |
| 屏幕显示 DEFEAT | ✓ 红色，居中 |
| Console 无 Error | ✓ |

### 测试 B：Victory 流程

| 步骤 | 结果 |
|------|------|
| 挖通 y=9，放置 5 只 Slime（x=5/10/15/20/25）| ✓ |
| Hero HP 消耗：30→24→18→12→6→0 | ✓ 第 5 只 Slime 击败 Hero |
| State=Victory | ✓ |
| 屏幕显示 VICTORY | ✓ 黄色，居中 |
| Console 无 Error | ✓ |

### 测试 C：基础交互保护

| 测试项 | 结果 |
|--------|------|
| 点击 Entrance(0,9) | ✓ CellType 不变（Protected）|
| 点击 DemonLordRoom(31,9) | ✓ CellType 不变（Protected）|
| 对同一 Empty 格重复放 Slime | ✓ 第二次 PlaceSlime 返回 false |
| 点击地图外坐标 | ✓ IsInside=false，无 Error |
| 游戏结束后 Hero 是否停止移动 | ✓ IsPlaying()=false，HeroMover 协程退出 |

### 测试 D：场景对象检查

| 测试项 | 结果 |
|--------|------|
| GridTiles 存在且有 576 个子对象 | ✓ |
| MonsterViews 存在 | ✓ |
| HeroViews 存在 | ✓ |
| ResultCanvas 存在 | ✓ |
| GridManager 挂载 12 个组件 | ✓ |
| 退出 Play Mode 后运行时对象清除 | ✓ 无残留 |

**Console 全程零 Error。**

---

## 四、当前已知限制

以下限制均为 MVP 阶段的设计决策，不是 bug。

| 限制 | 说明 |
|------|------|
| Slime 不阻挡寻路 | BFS 将 Empty 格（含 Slime）视为可通行，Hero 不绕开 Slime |
| 战斗时机 | 战斗在 Hero 抵达该格后立即结算，非范围检测 |
| Hero 立即生成 | 游戏开始后 Hero 立即从入口生成，无延迟/波次 |
| 放 Slime 方式 | 点击 Empty 格手动放置（非原版"挖开土块自动生成"）|
| 无 Restart | 重新游戏需退出并重进 Play Mode |
| 无美术资源 | 全部 Primitive Quad + 颜色区分占位 |
| 无血条/动画/音效 | 完全省略 |
| 土块属性未实现 | 土块魔力/养分系统尚未实现 |

---

## 五、下一阶段候选方向

以下为候选项，均**暂不实现**，由人类在下一轮决策。

**体验完善（较高优先）**
- [ ] Restart 功能（按键重置，让玩家独立完整游玩一局）
- [ ] Slime 阻挡 BFS 寻路（使"封路"成为真正的策略操作）
- [ ] 操作提示 UI（告知玩家点击规则）

**规则还原（中等优先）**
- [ ] 土块魔力/养分属性系统
- [ ] 挖开土块时根据属性自动生成对应魔物
- [ ] 多 Hero 波次系统（间隔生成多名 Hero）

**表现升级（可延后）**
- [ ] Sprite 美术资源替换
- [ ] Hero/Slime 血条显示
- [ ] 战斗动画/特效
- [ ] 音效
