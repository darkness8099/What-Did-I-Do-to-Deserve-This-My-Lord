# TASKS — 第一阶段任务清单

## 状态说明
- `[ ]` 待执行
- `[→]` 进行中
- `[x]` 已完成
- `[!]` 阻塞/问题

---

## 阶段 0：项目初始化

- [x] **TASK-000** — MCP 连接测试与安全验证
- [x] **TASK-001** — 创建 AI_DOCS 文件夹及初始文档（GAME_DESIGN_BASE、AI_WORKFLOW_LOG、TASKS、UNITY_MCP_RULES）

---

## 阶段 1：场景与基础地图

- [x] **TASK-002** — 创建 GameScene 场景，设置 Camera（正交，尺寸适配 32×18 网格）
- [x] **TASK-003** — 创建 Scripts 文件夹，编写 `GridData.cs`（网格数据模型，CellType 枚举：Soil/Empty/Entrance/DemonLordRoom）
- [x] **TASK-004** — 编写 `GridManager.cs`（初始化 32×18 网格，管理格子状态）
- [x] **TASK-005** — 编写 `GridRenderer.cs`（用 Unity Primitive 或 SpriteRenderer 渲染每个格子，颜色区分类型）
- [x] **TASK-006** — 在 Scene 中测试：运行后应看到 32×18 的彩色网格

---

## 阶段 2：挖掘系统

- [x] **TASK-007** — 编写 `InputHandler.cs`（鼠标点击 → 屏幕坐标转换为网格坐标）
- [x] **TASK-008** — 在 `GridManager` 中实现 `DigCell(int x, int y)`（Soil → Empty，更新渲染）
- [x] **TASK-009** — 测试：点击土块应变为空洞（颜色变化）

---

## 阶段 3：魔物系统

- [x] **TASK-010** — 编写 `MonsterData.cs`（基础 Slime 数据：HP、攻击力、攻击范围）
- [x] **TASK-011** — 编写 `MonsterManager.cs`（管理地图上的魔物数据，PlaceSlime/HasMonster/GetMonster）
- [x] **TASK-012** — 编写 `MonsterRenderer.cs`（魔物可视化层，CreateMonsterView/HasMonsterView/GetMonsterView）
- [x] **TASK-013** — 扩展 InputHandler：点击 Soil 挖空洞，再点击 Empty 放置 Slime（两步交互）

---

## 阶段 4：勇者系统

- [x] **TASK-014** — 编写 `HeroData.cs`（HP、移速、攻击力，纯 C# 类，无 UnityEngine 依赖）
- [x] **TASK-015** — 编写 `HeroManager.cs`（管理勇者数据与网格位置，SpawnHeroAtEntrance/GetHero/GetAllHeroes）
- [x] **TASK-016** — 编写 `HeroPathfinder.cs`（BFS 寻路，从入口到魔王位置）
- [x] **TASK-017** — 编写 `HeroRenderer.cs`（勇者可视化层，CreateHeroView/HasHeroView/GetHeroView/SetHeroViewPosition）
- [x] **TASK-018** — 编写 `HeroMover.cs`（Start 生成勇者，协程按 MoveSpeed 平滑逐格移动，无路径时等待重试，到达 DemonLordRoom 时输出 Log）

---

## 阶段 5：战斗与胜负

- [x] **TASK-019** — 实现魔物与勇者的战斗交互（互相扣血，HP 归零则消灭）
- [x] **TASK-020** — 编写 `MVPGameManager.cs`（检测胜负条件：Hero 到达 DemonLordRoom→Defeat，所有 Hero 被击败→Victory）
- [ ] **TASK-021** — 添加最简单的胜负 UI（屏幕中央显示"胜利"/"失败"文字）
- [ ] **TASK-022** — 整体流程测试：完整跑一局，确认胜负判定正常

---

## 阶段 6：收尾与文档

- [ ] **TASK-023** — 更新 AI_WORKFLOW_LOG（记录本阶段所有问题与经验）
- [ ] **TASK-024** — 整理可复用的 AI 工作流模板

---

*每完成一个 Task，在此文件中将 `[ ]` 改为 `[x]`，并在 AI_WORKFLOW_LOG 追加记录。*
