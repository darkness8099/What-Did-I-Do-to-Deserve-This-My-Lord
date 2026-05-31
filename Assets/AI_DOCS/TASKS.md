# TASKS — 第一阶段任务清单

## 状态说明
- `[ ]` 待执行
- `[→]` 进行中
- `[x]` 已完成
- `[!]` 阻塞 / 问题

---

## 阶段 0：项目初始化

- [x] **TASK-000** — MCP 连接测试与安全验证
- [x] **TASK-001** — 创建 AI_DOCS 文件夹及初始文档（GAME_DESIGN_BASE、AI_WORKFLOW_LOG、TASKS、UNITY_MCP_RULES）

---

## 阶段 1：场景与基础地图

- [x] **TASK-002** — 创建 GameScene 场景，设置 Camera（正交，尺寸适配 32×18 网格）
- [x] **TASK-003** — 创建 Scripts 文件夹，编写 `GridData.cs`（网格数据模型，CellType 枚举：Soil / Empty / Entrance）
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
- [x] **TASK-011** — 编写 `MonsterManager.cs`（管理地图上的魔物数据，PlaceSlime / HasMonster / GetMonster）
- [x] **TASK-012** — 编写 `MonsterRenderer.cs`（魔物可视化层，CreateMonsterView / HasMonsterView / GetMonsterView）
- [x] **TASK-013** — 扩展 InputHandler：点击 Soil 挖空洞，再点击 Empty 放置 Slime（两步交互）

---

## 阶段 4：勇者系统

- [x] **TASK-014** — 编写 `HeroData.cs`（HP、移速、攻击力，纯 C# 类，无 UnityEngine 依赖）
- [x] **TASK-015** — 编写 `HeroManager.cs`（管理勇者数据与网格位置，SpawnHeroAtEntrance / GetHero / GetAllHeroes）
- [x] **TASK-016** — 编写 `HeroPathfinder.cs`（BFS 寻路，从入口到魔王位置）
- [x] **TASK-017** — 编写 `HeroRenderer.cs`（勇者可视化层，CreateHeroView / HasHeroView / GetHeroView / SetHeroViewPosition）
- [x] **TASK-018** — 编写 `HeroMover.cs`（Start 生成勇者，协程按 MoveSpeed 平滑逐格移动，无路径时等待重试，接近魔王单位后捕获）

---

## 阶段 5：战斗与胜负

- [x] **TASK-019** — 实现魔物与勇者的战斗交互（互相扣血，HP 归零则消灭）
- [x] **TASK-020** — 编写 `MVPGameManager.cs`（检测胜负条件：Hero 带魔王返回入口→Defeat，所有 Hero 被击败→Victory）
- [x] **TASK-021** — 添加最简单的胜负 UI（屏幕中央显示“胜利”/“失败”文字）
- [x] **TASK-022** — 整体流程测试：完整跑一局，确认胜负判定正常

---

## 阶段 6：收尾与文档

- [x] **TASK-023** — 更新 AI_WORKFLOW_LOG（记录本阶段所有问题与经验）
- [x] **TASK-024** — 整理可复用的 AI 工作流模板

---

## 阶段 7：土块属性与自动生成系统

- [x] **TASK-025** — 创建 TileAttributeData 数据结构，扩展 GridData 属性接口
- [x] **TASK-026** — 修改挖掘逻辑：挖开土块时检查属性，MagicPower>0 则自动生成对应魔物

---

## 阶段 8：勇者目标逻辑扩展

- [x] **TASK-027** — 修改勇者目标逻辑：到达魔王位置后切换为返回模式，返回入口才触发失败
- [x] **TASK-028** — 更新 AI 操作规则：Git 禁止操作、测试分级策略、任务汇报格式

---

## 阶段 9：美术资源接入与目录规范

- [x] **TASK-029A** — 只读分析合作美术发来的资源包清单（项目外路径），输出“资源 vs 项目系统”映射表
- [x] **TASK-029B** — 创建美术接入规则与命名规则文档（`ART_INTAKE_RULES.md` / `ART_NAMING_RULES.md` / `ART_INTAKE_LOG.md`）
- [x] **TASK-029C** — 在 `Assets/Art/` 下创建分类空目录骨架（含 `_Incoming/`），仅 `.gitkeep` 占位
- [x] **TASK-029D** — 选 1–2 张测试 sprite 走完整流程：进 `_Incoming/` → 命名 → 设 Import Setting → 移动到 `Art/Tiles/`
- [x] **TASK-029E-pre** — 全量导入剩余 4 张（入口 / 勇者 / 史莱姆 / Demon Lord），完成初始 Art 目录填充
- [ ] **TASK-029E** — Prefab 自动化实验：用 1 个 Slime sprite 生成 `Slime.prefab`，让 `MonsterRenderer` 改为实例化模式
- [x] **TASK-029E-pre2** — Visual Prefab 试验：用已导入 sprite 创建第一批仅含 `SpriteRenderer` 的视觉 Prefab（不改场景、不改 gameplay 脚本）
- [ ] **TASK-029F** — UI 自动部署实验：用 1 张 victory panel 替换 `MVPResultUI` 的纯文字
- [x] **TASK-030** — `GridRenderer` 接入 Soil sprite（`tile_soil_surface_00` / `tile_soil_deep_00`），替换 Soil 格纯色 Quad 渲染
- [x] **TASK-030B** — `GridRenderer` 接入 Entrance sprite；`HeroRenderer` 接入测试用 DemonLord 单位 sprite；`MonsterRenderer` 接入 Slime sprite
- [x] **TASK-031** — 勇者停下战斗逻辑（协程 CombatSystem）+ 魔王被捕跟随勇者返回逻辑 + 索敌范围系统（AttackRange）
- [x] **TASK-032** — 管理器职责拆分：新增 `LevelConfig` / `DemonLordManager` / `DemonLordRenderer` / `DigActionHandler`，收窄 `GridManager` 与 `InputHandler`
- [x] **TASK-033** — 地下迷宫入口布局调整：入口改为顶部第 4 行中间，入口下方固定空洞，魔王放置在下方空洞，地下从入口下一行开始生成土块
- [x] **TASK-034** — 勇者默认延迟 10 秒出发；挖掘规则改为仅允许挖开四邻存在道路 / 入口的土块
- [x] **TASK-035** — 默认相机视窗调整为约 30×16 格；输入规则补充右键按住拖动视角
- [x] **TASK-036** — 地下表层（入口下方紧邻一行）定义为不可破坏的表面层：`LevelConfig.UsesSurfaceSoilSprite` 改名为 `IsSurfaceLayer`，`GridManager.IsDiggable` 在 surface row 直接早退
- [x] **TASK-037** — 怪物生态框架字段预留：双资源轴（Nutrient / Magic）、MonsterArchetype + Registry、MonsterIdentity 组件、EcologyRole / MoveStrategy 枚举、ResourceFlow + FloatingResourcePool（挖掘剩余扩散 + 死亡回流），生态闭环骨架到位
- [x] **TASK-038** — 勇者出发前加入魔王重新放置流程：魔王开局存在；倒计时结束后进入抓取状态，玩家左键把魔王转移到任意 Empty 格，再生成勇者继续游戏
- [x] **TASK-039** — 生态资源流适用范围固化：引入 `DeathCause`；区分挖掘残余散布与普通非捕食死亡散布；`CombatSystem` 仅以 `HeroKill` 触发普通死亡回流，避免捕食 / 生命周期 / 自然衰弱误用 Scatter
- [x] **TASK-040** — 捕食资源转移 API：新增 `ResourceFlow.TransferResourcesToPredator(prey, predator, reason)`；猎物资源按捕食者容量转移，溢出进入 `FloatingResourcePool`；`PredatorEat` 不触发普通死亡回流
- [x] **TASK-041** — 16:9 gameplay viewport 规则固化（参考 PSP 局部视野）：目标 28×16 格、ortho ≈ 8；代码端 `LevelConfig.CameraViewRows=16` + `InputHandler.ApplyInitialCameraView` 已对齐设计意图；Camera Inspector ortho=9 为 Edit Mode 残值（Play Mode 被 InputHandler 覆盖为 8，无害）；编辑器 Game View aspect 必须保持 16:9
- [x] **TASK-042** — 美术资源批量接入（97 张 PNG）：`Art/{Backgrounds×1, Buildings×3, Entrances×5, Props×11, SurfaceObjects×8, Tiles×64, Vegetation×5}`；新建 4 个类别目录（Buildings / Entrances / SurfaceObjects / Vegetation）；`ART_INTAKE_RULES` / `ART_NAMING_RULES` 同步补充 Pivot 默认表 + 程序化生成池 `<prefix>_<index>` 简化命名 + 新前缀模板（entrance / building / surface / veg）；同步删除旧 `tile_entrance_default_00.png` + `PF_Environment_Entrance_Default.prefab`（入口将重做）；编译 0 Error
- [x] **TASK-043** — 新增 `SURFACE_DECORATION_RULES.md`：地表 70×10 区域划分（Zone A–E）/ 类别 × Zone 权重表 / 4 层 BG sorting / 占位宽度参考 / 第一版生成目标 / 后续抽象（Profile / Spawner / PlacementData）；规则 only，不实现
- [x] **TASK-044** — `SurfaceDecorationProfile` 数据载体：plain class，承载 5 Zone 边界（`[0,14) / [14,27) / [27,41) / [41,55) / [55,70)`）+ 6 类 × 5 Zone 权重矩阵 + 各类别 sprite 路径清单（29 张装饰素材，对齐 TASK-042 导入）+ 类别默认占位宽度回退表 + 入口默认列 34；`CreateDefault()` 工厂；纯数据无副作用
- [x] **TASK-045** — `DecorationPlacementData` + `SurfaceDecorationSpawner`：随机种子 + 按 Profile 生成草稿 + 清除 / 重生；不写 Scene 文件
- [x] **TASK-046** — `BackgroundLayerRenderer`：4 层 BG（Base / BackDeco / MidDeco / FrontDeco）sortingOrder 接入；`bg_overworld_00` 铺底；spawner 输出按 layer 实例化
- [x] **TASK-047** — 地图尺寸正式扩到 70×50（`LevelConfig.width=70, height=50`，入口默认列 ≈ 34）；与 BG 层联动
- [ ] **TASK-048** — 入口系统重做：新 `EntranceManager` / `EntranceRenderer`，多格大尺寸 `entrance_*` 实例化 + 新勇者出生流程（衔接 TASK-038 流程）
- [x] **TASK-049** — Soil 主题集接入：`GridRenderer` 按 grid 位置 / 主题 / 随机选用 4 色 × 16 张 `tile_soil_<color>_<index>`
- [x] **TASK-049A** — 旧测试土块清理：删除 `tile_soil_surface_00` / `tile_soil_deep_00` 及其测试 prefab，默认土块生成完全切换到新主题集
- [x] **TASK-049B** — 解除地下表层（第 11 行）不可点击限制，顶部 10 行背景区之外恢复统一四邻挖掘规则
- [ ] **TASK-050** — 草稿持久化（可选）：草稿对象组烘焙为 `PF_Surface_Decoration_<level>.prefab` 或场景节点

---

## 编号维护说明

- `TASKS.md` 以“当前真实任务状态”为准。
- `AI_WORKFLOW_LOG.md` 中旧的“后续建议任务”如果与后续正式编号冲突，应视为历史建议，不再作为编号依据。
- 当前最新已落档任务为 `TASK-047`，后续新任务从 `TASK-048` 起顺延。

---

*每完成一个 Task，在此文件中将 `[ ]` 改为 `[x]`，并在 `AI_WORKFLOW_LOG.md` 追加记录。*
