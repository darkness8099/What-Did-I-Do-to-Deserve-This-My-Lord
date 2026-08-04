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
- [x] **TASK-029E** — Prefab 自动化实验：用 1 个 Slime sprite 生成 `Slime.prefab`，让 `MonsterRenderer` 改为实例化模式
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
- [x] **TASK-048** — 入口系统重做（综合实现关闭）：多格大尺寸 `entrance_*` 实例化由 `BackgroundLayerRenderer` 承担（TASK-046 / 046B / 050C）；入口连接点 + Entrance 格黑色空洞渲染由 TASK-051 实现；勇者出生 + 魔王放置流程由 TASK-038 实现；不再单独抽象 `EntranceManager` / `EntranceRenderer`
- [x] **TASK-049** — Soil 主题集接入：`GridRenderer` 按 grid 位置 / 主题 / 随机选用 4 色 × 16 张 `tile_soil_<color>_<index>`
- [x] **TASK-049A** — 旧测试土块清理：删除 `tile_soil_surface_00` / `tile_soil_deep_00` 及其测试 prefab，默认土块生成完全切换到新主题集
- [x] **TASK-049B** — 解除地下表层（第 11 行）不可点击限制，顶部 10 行背景区之外恢复统一四邻挖掘规则
- [x] **TASK-046B** — 地表背景草稿生成规则加密：提高 Props / Vegetation 数量，引入主体附属装饰与空白补足，明确 10 格制作 / 5~7 格主要可见 / Editor 草稿定位
- [x] **TASK-050** — `BackgroundLayerRoot` 编辑器辅助化：停止运行时自动生成；在 `BackgroundLayerRenderer` Inspector 上提供随机种子、编辑器生成、清空当前草稿、保存当前背景为 Prefab 的按钮工作流
- [x] **TASK-050A** — 背景装饰草稿生成基线调整：`BackgroundLayerRenderer` 新增可调 `decorationBaselineOffsetCells`，默认向上 1 格，使装饰从第 9 格附近生成，便于基于大背景地表人工微调
- [x] **TASK-050B** — 背景 Prefab 保存命名与目录整理：保存目录改为 `Assets/Prefabs/Backgrounds`，自动生成 `PF_Background_Surface_01` ~ `10` 的未占用编号，避免反复覆盖默认文件
- [x] **TASK-050C** — 游戏模式背景应用：`BackgroundLayerRenderer` 在 Start 时从已保存的 `PF_Background_Surface_01` ~ `10` 中随机选择一个背景 Prefab 实例化；无已保存背景时打印 Error；不再运行时生成草稿
- [x] **TASK-051** — 入口连接点重定义：入口固定为地图中间列、从上往下第 10 格，作为地下世界入口连接空洞；`GridRenderer` 不再显示旧绿色测试入口，Entrance 格按黑色空洞渲染
- [x] **TASK-052** — Scripts 目录分类整理：通过 Unity AssetDatabase 移动脚本到 Core / Grid / Input / Hero / DemonLord / Monsters / Combat / Ecology / Background / UI；Editor 自定义 Inspector 移至 `Assets/Editor/Background`
- [x] **TASK-053** — 土块养分外观规则接入：`TileAttributeData.Nutrient` 映射到 Soil 主题 0-15 图；0 / 1-10 / 11-20 / 21+ 分段决定 1-3 级外观；所有外观仍统一为可挖 `CellType.Soil`
- [x] **TASK-054** — 初始养分生成规则固化：规定地下 Soil 默认 0 养分，初始养分以 Lv1 局部团簇为主，Lv2 仅中后期少量种子点，Lv3 主要由生态循环成长产生；当前全图测试养分分布标记为后续替换对象
- [x] **TASK-055** — 新增 `StageNutrientProfile` / `NutrientClusterSettings` 最小数据结构，用于表达关卡阶段、团簇中心、半径、强度、衰减与 `maxInitialNutrient`
- [x] **TASK-056** — 将 `LevelConfig.ApplyInitialSoilAttributes()` 当前全图测试养分分布替换为 `GenerateInitialNutrients()` 团簇式初始化，保证 Stage 1 主要只生成 `tile_00` ~ `tile_05`
- [x] **TASK-056A** — Stage 1 初始生态启动量调参：默认改为 8 个 Lv1 团簇 + 约 12% 的 1~3 低值基础散布，仍禁止 Lv2 / Lv3 初始生成
- [x] **TASK-056B** — 空 `StageNutrientProfile` 回退默认配置：Inspector 中无团簇、无基础散布、无种子点的 profile 视为未配置，使用默认 Stage 1 生成
- [x] **TASK-056C** — Stage 1 初始养分加厚：默认改为约 35% 的 1~3 低级底噪 + 10 个重叠 Lv1 团簇 + 极少量 Lv2 种子点，仍禁止 Lv3 初始生成
- [x] **TASK-056D** — Stage 1 初始养分 seed 化：同一 Stage 规则下按随机种子改变底噪、团簇与 Lv2 种子点位置；seed 为 0 时每次初始化自动换分布，非 0 时可复现
- [x] **TASK-056E** — Nutrient cluster 改为概率型椭圆团簇：使用 `radiusX / radiusY / density`，团簇内允许空洞与破碎边缘；Lv2 / Lv3 种子点依附团簇高密度区域生成
- [x] **TASK-057** — 接入生态系统动态改变 Soil 养分后的外观刷新：魔物繁殖、死亡回流、捕食溢出、生命周期转化等改变资源时刷新对应土块 sprite

---

## 阶段 10：匍匐苔藓 / 史莱姆生态实现（设计见 `GAME_DESIGN_SLIME.md`）

- [x] **TASK-058** — 编写 `GAME_DESIGN_SLIME.md`：匍匐苔藓生命周期（衰弱来源 / 吸放 / 移动 / 转花苞 / 花苞 / 花 / 繁殖 / 资源分流）正式成文
- [x] **TASK-059** — Slime/Moss 数值入表：`MonsterArchetype` 扩充生命周期字段（HP·养分·Bud·Flower·繁殖·tick），`NutrientCapacity` 5→3，新增枚举值 `MonsterEcologyRole.NutrientCarrier`、`MonsterMoveStrategy.StraightUntilWall`、`SlimeSpawnOriginPriority`，Slime 填入保守模板值。**仅字段占位，未实现行为逻辑**
- [x] **TASK-060** — `SlimeLifecycleStage { Crawling, Bud, Flower }` 枚举 + `MonsterData.Stage`（默认 Crawling）+ `SetLifecycleStage()`；数值定稿（`HpHealPerAbsorb=2`、`HpCostPerMove=1` 固定、Tick 均 1.0 不合并、`InitialHP=16`）；`KeepNutrientOnRelease` 保留独立 + v1 约束注释。仅字段/状态铺垫，无转化行为
- [x] **TASK-061** — Grid 侧数据 + 邻格查询（**不接史莱姆行为**）：确认 `TileAttributeData.Nutrient/Magic` 已存在（无需新增）；`GridManager.GetNeighborCells4`（非分配，buffer 版）/ `TryGetNeighborCells4`（List 版）；`GridData.HasAbsorbableNutrient` + `GridManager.HasAbsorbableNutrient`（派生：`IsSoil && Nutrient>0`，无额外 bool）。每次最多访问 4 邻格
- [x] **TASK-062** — Slime/Moss 规则移动：新增 `MonsterMovementSystem`（`ComputeNextStep` 纯决策：直行/遇阻按固定顺序转向/被困不动；`TryMoveStep` 整合 grid+manager）；`MonsterData.MoveDirection` + setter；`MonsterManager.MoveMonster`（字典改键、不堆叠）+ `MonsterMoved` 事件（即"移动完成"hook，供 063 接生态检测）。**无寻路/无每帧；不扣 HP/不吸放**。execute_code 全用例验证。⚠️ 尚未接 tick 驱动/场景，游戏内还不会动（驱动需新增 MonoBehaviour 入场景，待批准）
- [x] **TASK-063** — 移动完成后的生态检测：新增 `MonsterEcologySystem`（`ResolveAfterMove`/`ResolveAt` + `EcologyAction` 枚举）+ `MonsterData.Heal`。4 邻：`<=1` 从有养分的 Soil 吸 1 点并回血（`HpHealPerAbsorb`）/ `==2` 不动 / `>=3` 向相邻 Soil 释放多余（`surplus=n-KeepNutrientOnRelease`，不低于 Keep）；无可吸养分/无 Soil 可释放则不动作。阈值全读 archetype。execute_code 全用例验证。挂接点＝`MonsterManager.MonsterMoved`（待 tick 驱动订阅）
- [x] **TASK-064** — HP 移动消耗 + `InitialHP` 出生值 + 自然死亡分流：`MonsterData` 出生 HP 改用 `InitialHP`(16,非 MaxHP 21) + `TransformTo(stage,maxHp)`；新增 `MonsterLifecycleSystem`（`ApplyMoveHpCost` 固定/可随机 + `ResolveNaturalDeath` + `LifecycleOutcome`）：Crawling 死亡时 `Nutrient>=BudRequiredNutrient` → 转 Bud(重置 HP=BudMaxHP，保留养分)；否则 StarvationFailed（剩余进 `FloatingResourcePool`、移除）。**不走普通死亡回流**。execute_code 全用例验证
- [x] **TASK-065** — 花苞 Bud：`MonsterLifecycleSystem.BudTick`——5×5(`BudAbsorbRadius`)环形吸收进 `CollectedNutrient`，达 `BudToFlowerNutrient(8)` → `TransformTo(Flower)`；每 tick 扣 `BudHpDecayPerTick`，HP 归零未达阈值 → WitherFailed（资源进 FloatingPool、移除）。Crawling→Bud 时 `SeedCollected`。execute_code 验证（6 tick 转花）
- [x] **TASK-066** — 花 Flower：`FlowerTick`——7×7(`FlowerAbsorbRadius`)吸收(上限 `FlowerMaxAbsorb=11`)，每 tick 扣 `FlowerHpDecayPerTick`，HP 归零 → 繁殖结算 `spawnCount=min(FlowerMaxSpawn,⌊Collected/NutrientPerSpawn⌋)`，`ReproduceSlimes` 按 origin+4 邻固定顺序放新 Crawling（不堆叠）。execute_code 验证（生成 5 只）
- [x] **TASK-067** — Bud/Flower 阶段渲染：建 `AC_Bud`(`anim_plant_growth`)/`AC_Flower`(`anim_flower_bloom`)；`MonsterRenderer` 按 `Stage` 切 AnimatorController + `SyncViews`（增/删/重定位/换阶段）+ 订阅 `MonsterMoved` 平滑移位
- [x] **TASK-068** — 生态 tick 驱动 + 场景接线：新增 `EcologyTickDriver`（固定 1.0s：每怪按阶段派发 移动→扣血→吸放→死亡分流 / Bud / Flower，再 `SyncViews`）；已加到场景 `MonsterManager` 物体并保存 GameScene。**至此可进 Play Mode 试玩完整生命周期**
- [x] **TASK-069** — 史莱姆移动动画实装（视觉层先行）：新建 `Assets/Animations/Monsters/AC_Slime.controller`（默认 `anim_slime_move`，循环）；`Slime.prefab` / `PF_Monster_Slime_Default.prefab` 加 `Animator`，Visual 默认帧 `monster_slime_move_00`；9 个 clip 资产核对通过（均绑 `Visual/m_Sprite`、无空帧）。事件驱动动画（attack/death/absorb/emit、植物/花）待对应行为/生命周期逻辑接入

---

## 阶段 11：项目收尾（Closeout）——冻结范围，目标＝可打包 + 可录像

> **本阶段目标**：不再增加玩法内容。把"纯 AI 工作流能否产出完整可玩游戏"这个立项目标的最后一环——**打包产物**——补上，并保证录像时画面构图符合设计。
> **验收标准**：双击 exe 能进 GameScene、能挖、能跑完一局出现胜负 UI、画面比例正确、无 Console Error。
> **范围冻结**：本节 TASK-070 ~ TASK-076 完成即收尾。不新增编号。

### 执行顺序（重要）

先打一个**废包**拿真实报错清单，再修。构建失败是静默的（见 TASK-071），不实际打一次无法知道还有多少隐藏失效点。

- [x] **TASK-070** — 基线废包诊断完成。`Builds/Baseline/` Windows64 development build：**succeeded / 0 Error / 3 Warning / 72.9s / 105.37 MB**。构建管线本身健康。3 条警告全部是 `BackgroundLayerRenderer` 的 `savedPrefabFolder`(14) / `savedPrefabNamePrefix`(15) / `savedPrefabMaxCount`(16) — CS0414「赋值但从未使用」。**这直接证实了 TASK-071 的诊断**：Player 构建剥离 `#if UNITY_EDITOR` 后，这三个字段唯一的消费者 `PickRandomSavedBackgroundPrefab()` 一并消失，即背景加载路径在产物中不存在。`MonsterRenderer` 因字段带 `[SerializeField]`（序列化算使用）未报同类警告，但其加载代码同样被剥离，运行时为 null。**结论：构建 0 Error 但功能静默失效，与预期一致。** 未运行该废包（仅含空 SampleScene，运行无信息量），真实烟测归于 TASK-076。
- [x] **TASK-071** — 已修复。诊断结论：场景实例上 `MonsterRenderer.slimePrefab / acCrawling / acBud / acFlower` **全部为 null**（此前完全依赖 `#if UNITY_EDITOR` 的 AssetDatabase 兜底），构建后史莱姆靠 `spriteSlime` 回退仍可见但**无 Animator、完全不动**。处理：① 场景接线 4 个引用（Slime.prefab + AC_Slime/AC_Bud/AC_Flower）；② `BackgroundLayerRenderer` 新增 `[SerializeField] GameObject[] savedBackgroundPrefabs` + `PickRandomBackgroundPrefab()`——序列化列表为构建期唯一有效来源，编辑器文件夹扫描降级为回退，`LoadRandomSavedBackgroundForGameplay` 不再整体包在 `#if UNITY_EDITOR` 里；③ 3 个纯编辑器字段移入 `#if UNITY_EDITOR`，消除 TASK-070 的 3 条 CS0414。已接入 4 个 `PF_Background_Surface_01~04`。GameScene 已保存。**编译 0 Error / 0 Warning。** 另记：`LoadSprite`(348) 同为 AssetDatabase，但仅服务编辑器草稿流程 `RebuildLayers()`，运行时不走，非构建阻塞项，保持原样。
  > 方案变更记录：原计划改用 `Resources.Load` + 移动资产到 `Assets/Resources/`。实际因 MCP 恢复后可直接操作场景，改用**序列化引用**——更符合 Unity 惯例、不产生 Resources 目录、无需移动任何资产文件。结果等价且风险更低。
- [x] **TASK-072** — GameScene 入 Build Settings 并设为 index 0，移除 SampleScene。**需要 Editor（ProjectSettings 由运行中的 Editor 持有，外部改会被覆盖）。**
- [x] **TASK-073** — 锁定分辨率与画面比例。TASK-041 的「16:9 / 约 28×16 格」是靠人工在 Editor Game View 顶部选 Aspect 保证的，**打包后无任何机制保证**：`InputHandler.ApplyInitialCameraView` 只按 `CameraViewRows` 推纵向 `orthographicSize`，横向可见列数完全随窗口宽高比漂移（21:9 全屏会看到 40+ 列，构图与设计不符）。方案：Player Settings 固定 1920×1080、窗口模式、禁用 resizable。**需要 Editor（ProjectSettings）。**
- [x] **TASK-074** — 关闭构建产物中的诊断开销。`EcologyTickDriver.enableSlimeEcologyDiagnostics` 默认 `true`：每 tick 写文件 + 每 5 秒全图扫描 3500 格。⚠️ **注意：该字段是 `[SerializeField]`，场景实例上已序列化为 `true`，仅改代码默认值无效**，必须改场景实例值（或改为 `#if !UNITY_EDITOR` 强制关闭）。**需要 Editor（场景）或代码侧强制。**
- [x] **TASK-075** — 完成 TASK-029F（全清单唯一遗留项）+ 补重开键。`MVPResultUI` 目前是代码生成 Legacy Text 打 "VICTORY"/"DEFEAT" 字符串，而这是录像结尾唯一露脸的 UI。用现有 147 张素材里的 victory panel 替换；同时补 R 键重开，便于多镜头重录。**美术资源若缺失，先用 Unity 内置组件平替并向用户确认。**
- [x] **TASK-076** — 正式打包 + 启动烟测完成（录制彩排待人工）。彩排目的：确认镜头前节奏可用（tick=1.0s、Bud 需攒 8 养分、Flower 要 HP 衰减到 0 才繁殖，生态演化在镜头前可能过慢）。若节奏不适合录像，**在此处一次性调完 tick 参数，不回头再开新任务**。

### 收尾执行结果（2026-08-03）

**产物**：`Builds/Closeout/WhatDidIDo.exe` — Windows64 release，**0 Error / 0 Warning / 38.4s / 99.67 MB**。

**逐项落地**：

| Task | 结果 |
|---|---|
| 072 | Build Settings 现为 `GameScene`(index 0)，SampleScene 已移除 |
| 073 | PlayerSettings 固定 `1920x1080` / Windowed / `resizable=false` / `nativeRes=false`。实测 aspect=1.778 → 横向可见 **≈28.4 格**，命中 TASK-041 设计目标 28（可接受 27–30） |
| 074 | 场景实例 `EcologyTickDriver.enableSlimeEcologyDiagnostics` 由 `true` 改为 `false`（代码默认值无效，必须改实例，已验证） |
| 075 | `MVPResultUI` 重写为内置组件面板：全屏压暗 + 900×420 卡片 + 强调条 + 118px 标题 + `Press R to Restart`；仅在状态变化时重绘（旧版每帧改写文本）。`MVPGameManager` 加 R 键重开（仅在非 Playing 时响应）。**连带修复**：`FloatingResourcePool` 是静态累加器，场景重载不清零，重开会继承上一局游离资源 → 新增 `Reset()` 并在 `Restart()` 中调用。UI 文案全英文（内置 LegacyRuntime 字体无中日韩字形）|
| 076 | 启动烟测通过。Player.log 全程 0 异常，各 Manager 正常初始化，`[BackgroundLayerRenderer] Loaded gameplay background prefab: PF_Background_Surface_04` —— **构建产物中背景加载成功，TASK-071 得到真实验证** |

**烟测未覆盖（属 C 类，需人工 Play）**：挖掘、魔物生成、生态生命周期（Crawling→Bud→Flower→繁殖）、战斗、胜负判定与结算面板实际显示。启动烟测只证明「能起、能加载、无异常」。

**遗留待人工决策**：场景中 `LevelConfig.heroSpawnDelaySeconds = 100`（代码默认 10）。开发期为留足挖掘测试时间而设，但录像开场会有 100 秒无事发生。是否下调由录制者决定。

**待清理**：`Assets/Editor/Closeout/CloseoutPlayerSettings.cs` 为临时菜单项（因 MCP `execute_code` 存在 BOM 编译 bug 而建），PlayerSettings 已持久化，可随时删除。

---

### 本阶段明确不做（防止范围再次膨胀）

| 不做项 | 原因 |
|---|---|
| asmdef / namespace 整理 | 纯工程洁癖，录像不可见，却要动全部 36 个文件 |
| `GridRenderer` 改 Tilemap | 3500 个 GameObject 当前跑得动；收尾期风险最高、收益最不确定。除非 TASK-070 废包实测掉帧，否则不碰 |
| 补自动化测试 / asmdef 测试程序集 | 其价值是迭代期的回归保护网。项目停止迭代后 ROI 归零 |
| 死代码清理（`MonsterType` / `Hunger` / `TransferResourcesToPredator` 零调用方 / `MonsterData.Tick()` 空方法） | 不可见。全部完成后有余力再说 |
| 第二种魔物 / 波次 / 音效 / 存档 / 多关卡 | 与"验证 AI 工作流"目标的边际贡献为零 |
| 文档漂移修复（TASK-062/063/067 描述的 `MonsterManager.MonsterMoved` / `MoveMonster` 已随 List 重构消失） | 记录在此备查；收尾完成后统一订正，不占用构建路径 |

---

## 编号维护说明

- `TASKS.md` 以“当前真实任务状态”为准。
- `AI_WORKFLOW_LOG.md` 中旧的“后续建议任务”如果与后续正式编号冲突，应视为历史建议，不再作为编号依据。
- 当前最新已落档任务为 `TASK-076`。**阶段 11 为收尾阶段，范围已冻结，不再新增编号。**
- 阶段 10 完成情况：061→068（Grid 数据/邻格 → 规则移动 → 移动后生态检测 → HP/出生/死亡分流 → Bud → Flower → 阶段渲染 → tick 驱动+场景接线）；TASK-069 移动动画为视觉层先行。（阶段 10 已完成 061→068：Grid 数据/邻格 → 规则移动 → 移动后生态检测 → HP/出生/死亡分流 → Bud → Flower → 阶段渲染 → tick 驱动+场景接线；TASK-069 移动动画为视觉层先行。整条匍匐苔藓生命周期已可进 Play Mode 试玩）

---

*每完成一个 Task，在此文件中将 `[ ]` 改为 `[x]`，并在 `AI_WORKFLOW_LOG.md` 追加记录。*
