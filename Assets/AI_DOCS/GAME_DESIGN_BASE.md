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
- 魔物之间没有移动阻挡，可以互相穿过，也允许复数魔物处于同一格。
- 当勇者进入某个魔物的攻击范围时，该魔物进入**战斗占用**：停止地形移动，并暂停移动 HP 消耗、养分吸放和生命周期推进，直至附近不再有可交战勇者。
- 能战斗的 Crawling 魔物会参与攻击；Bud / Flower 为被动阶段，不主动反击，但接战期间仍暂停自身生态阶段推进。

### 战斗目标与同格群攻（TASK-078）

- 勇者的一次普通攻击只锁定并伤害一个确切魔物个体；同一格存在多个魔物时，不因共享格子而变成范围伤害。
- 当前目标死亡后，勇者才从仍在攻击范围内的存活魔物中重新锁定目标。
- 怪物反击按**个体**结算：所有处于自身攻击范围内、存活且可战斗的 Crawling 魔物，每个反击阶段各攻击勇者一次。
- 因此多个魔物可以同格或相邻格共同围攻同一勇者；魔物彼此仍不构成碰撞或移动阻挡。
- 战斗占用只改变接敌时的优先级，不给匍匐苔藓增加寻路追击勇者的能力。

### 低素材战斗表现（TASK-085）

- Demo 阶段不要求每个职业 / 怪物具备四向攻击帧。普通攻击统一先用程序化的“朝目标短距离撞击后返回”表达，后续可按职业替换或叠加刀光、血花、弹道等表现。
- 撞击只改变视图的 `Visual.localPosition`，不得移动承载格子中心的 Root，也不得修改 `HeroManager` / `MonsterManager` 的逻辑坐标；动作结束后必须恢复各自原始局部位置，因此不会破坏勇者脚点对齐或怪物移动插值。
- 勇者每次仍只攻击一个确切魔物；怪物反击阶段则让本阶段所有有效 Crawling 个体同步撞向勇者，表现与同格 / 邻格群殴的逐个伤害结算一致。
- 伤害在撞击到达目标方向的瞬间结算。默认完整表现阶段为 0.22 秒；若攻击间隔更短则按间隔压缩，阶段后的等待时间扣除表现耗时，避免单纯加入动画后降低配置中的实际攻击频率。
- 受击反馈使用短暂纯白剪影。白闪结束后恢复目标原本的 URP 2D Lit 材质；同一目标发生重叠闪白时以引用计数管理，不允许较早结束的闪白把另一段效果提前覆盖掉。
- 本阶段不新增死亡动画、专属攻击动画或通用技能框架；死亡继续沿用现有清理流程，待获得合适素材后另开小任务补充。

### 战斗受击特效（TASK-101）

- 勇者只在怪物反击阶段至少有一个有效攻击者并实际结算伤害时，叠加一次 `fx_blood_arc_red` 四帧流血效果；血弧从 0° / 90° / 180° / 270° 中随机选择方向。同一阶段即使有多个怪物群攻也只生成一份，实际伤害仍按怪物个体逐一结算。
- 魔物被勇者近战或法师火球实际命中时，叠加一次 `fx_attack_impact_white` 四帧冲击效果。素材基准朝右，语义为攻击者从目标左侧打入；运行时按“攻击源指向受击目标”的四向力方向旋转：右=0°、上=90°、左=180°、下=-90°。
- 两套效果均为 12 FPS、非循环、世界空间 Sprite-Lit-Default 表现，约 0.4 秒后自动销毁。勇者血弧使用 Scale 1.5，视觉约 1.28×1.0 格，Sorting Order 22，高于勇者自身 Order 20；魔物冲击使用 Scale 1.5，视觉约 0.69×1.0 格，Sorting Order 12，高于魔物自身 Order 10。两者均绘制在受击单位前方，不添加 Collider / Rigidbody，也不参与伤害、格子占用、索敌或死亡判断。
- 既有目标纯白剪影继续保留，Sprite 特效作为额外可读层。致死命中在移除魔物视图前生成冲击，因此不会因死亡清理而丢失最后一次命中特效。

### 法师远程战斗（TASK-097）

- `mage_01` 与 `mage_02` 使用 `HeroAttackType.Ranged`，当前 `AttackRange=8`、`AttackSpeed=2`、`Attack=3`；释放频率与伤害继续读取职业配置，不为法师另写魔法数字。
- 法师只沿当前寻路留下的四向面向做直线索敌；最多检查前方 8 个格子，遇到 Soil、地图边界即停止，不能隔墙锁定。直线内找到存活魔物后停下移动并保持当前方向待机，按释放频率持续施放；当前方向 8 格内没有可攻击魔物时才恢复寻路。
- 没有施法动画时，法师只移动 `Visual` 子节点沿面向前顶 0.22 格后返回；Root 与逻辑格坐标不动。前顶到达释放点时创建火球，不在施法瞬间直接伤害远处目标。
- 火球是独立投射物，不保存或追踪最初目标。它以 4 格/秒沿当前方向逐格前进，进入格子时命中该格第一个存活魔物并造成单体伤害；若原目标先被其他火球或勇者杀死，剩余火球继续前进，可命中同路线后续魔物。遇到 Soil、地图边界时在通道边缘消失；没有射程结束限制。
- 火球击杀继续走 `HeroKill` 普通死亡资源回流、怪物死亡表现与 Manager 移除流程。`Resources/FX/PF_Hero_Fireball` 已在 TASK-099 接入正式飞行 Sprite 动画；TASK-101 已让火球把自身四向飞行方向传给魔物白色冲击特效，声音仍等待后续素材 / 小任务。

### 法术投射物素材池（TASK-098）

- 已接入一套完整的 32×32 法术投射物图集，包含 Fireball / Ice / Earth / Nature / Air / Arcane / Lightning 七种元素、每种四个视觉变体、每变体八帧，共 224 个 Sprite。
- 图集保留为一张 Sprite Mode Multiple 纹理，子 Sprite 使用 `fx_projectile_<element>_<variant>_<frame>`；对应 28 个 `anim_fx_projectile_<element>_<variant>` Clip 均按源 JSON 的 100 ms / 帧设置为 10 FPS 循环。
- 除已选中的 `anim_fx_projectile_fireball_01` 外，其余 27 个 Clip 仍只是候选表现素材库，不代表技能属性、伤害类型或职业绑定；可在以后按职业 / 技能逐项选择使用。
- 原 JSON 是 Aseprite 导出元数据，不是游戏技能配置；Unity 切片已经记录在纹理 `.meta`，因此 JSON 不复制进 `Assets`、不参与构建或运行时加载。

### 法师火球正式表现（TASK-099）

- 当前 `mage_01 / mage_02` 共用 `anim_fx_projectile_fireball_01`，由 `Resources/FX/PF_Hero_Fireball` 提供构建可用的 SpriteRenderer + Animator 表现；图集仍保持 PPU 48，Prefab 以 1.5 倍表现缩放把 32px 原帧调整为约 1×1 格，不改变逻辑判定尺寸。
- 源序列的基准朝向为屏幕右方。`HeroProjectileSystem.Launch` 使用施法瞬间的 `FacingDirection` 旋转投射物根节点：East=0°、North=90°、West=180°、South=-90°；逻辑位移和视觉都读取同一个四向向量，不会出现法师转向后火球仍朝旧方向的情况。
- AnimationClip 只绑定根节点 `SpriteRenderer.m_Sprite`，没有 Transform 曲线，因此 0.8 秒循环动画不会覆盖上述旋转。火球发出后保持自己的发射方向，不会因法师之后再次转身而中途拐弯。
- 当前不添加碰撞体、刚体或粒子尾迹；伤害 / 撞墙 / 越界仍完全由既有格子投射物逻辑判定。
- 释放频率与飞行速度是两个独立参数：职业 `AttackSpeed` 控制施法间隔，`HeroProjectileSystem.projectileSpeed` 只控制每枚火球的位移。Launch 后火球进入独立 Shot 列表，法师不会等待上一枚火球结束；因此远距离、高攻速时允许多枚火球同时在途。当前 Mage 为 2 次/秒、火球 4 格/秒，8 格路径约飞 2 秒，目标持续存在时理论可见约 4 枚在途火球。

### 勇者系统
- 勇者配置按“`HeroLevelConfig` 关卡 → `HeroWaveConfig` 波次 → `Heroes` 独立勇者槽位 → `HeroArchetypeConfig` 职业”组织；每个槽位只代表一个确切勇者，职业配置保存属性和表现资源，生成后的 `HeroData` 是独立运行时快照。
- 每一波固定执行：**准备倒计时 → 请求重新摆放魔王 → 等待玩家放到任意 `Empty` 格 → 生成本波勇者 → 等待本波所有勇者死亡**。
- 只有上一波所有勇者死亡后，下一波的准备倒计时才开始；倒计时期间魔王停留在当前世界位置，不提前进入摆放暂停。
- 每一波倒计时结束后都允许重新摆放魔王，以适应玩家新挖出的迷宫路线；完成合法落位后才生成该波勇者。
- 同一波直接在 `Heroes` 列表中逐行添加勇者；每行只配置职业和独立 `SpawnDelay`，不再提供批量 `Count` 或同类 `SpawnInterval`。多个槽位并行计时，延迟相同可同批出现；Demo 预期单波通常不超过 4 人，但数据层不写死数量上限。
- 当前 `hero_level_001` 配置为两波且每波准备 10 秒：Wave 1 为 1 名 Warrior；Wave 2 为 Warrior + Warrior 02，两名当前均在入侵阶段开始时立即生成。找不到关卡资产时仍使用兼容旧流程的运行时单波配置。
- 当前职业资产池包含 `warrior`、`warrior_02`、`mage_01`、`mage_02`、`priest_01`、`priest_02`。新增五套目前只完成独立四向美术与职业配置，暂沿用 Warrior 的中性数值和 `Normal` 单体攻击；法术、治疗及职业差异需后续配置/行为任务另行定义。
- 勇者方向后缀统一按屏幕方向解释：`e` 朝右、`w` 朝左、`n` 朝上、`s` 朝下。首套 `warrior` 原始顺序正确；后导入的五套职业已在 TASK-095 纠正东西向像素内容，AnimationClip 与 Controller 继续按同名方向引用。
- 勇者沿最短路径（BFS/Dijkstra）向魔王单位移动。

### 魔王重新摆放时的模拟暂停（TASK-079）

- `DemonLordManager.RequestReposition()` 开始、合法 `TryPlaceAt()` 完成之间，场上全部魔物进入**全局模拟暂停**。
- 暂停期间不推进魔物位置、移动 HP、养分吸放、生命周期、区域聚合模拟或相关 tick 计时；恢复时不补算暂停期间错过的 tick。
- 怪物视图的坐标插值同样暂停，画面位置保持在暂停瞬间。
- Animator 不做硬冻结：每个怪物锁定暂停瞬间的当前状态并在该状态时间轴内循环播放；移动、吸收、释放以及未来进食等状态都遵循同一规则。
- 放置成功后，各怪物从暂停时的逻辑数据与动画状态继续运行。
- 该效果不修改 `Time.timeScale`，所以魔王放置输入、鼠标拖动相机及其他非怪物界面仍可正常响应。

### 胜负判定
- **失败条件**：任意勇者抓住魔王并带回入口
- **继续条件**：非最终波的全部勇者被击败后，进入下一波准备倒计时，不提前结算胜利
- **胜利条件**：所有已配置波次均已生成完毕，且场上全部勇者被魔物击败

### Demo 菜单与 UI 流程（TASK-086 / TASK-092）

- 当前 Demo 暂不增加独立菜单 Scene，仍以 Build Settings 中唯一的 `GameScene` 作为入口。首次加载时显示不透明主菜单并将 `Time.timeScale=0`；游戏系统可以完成初始化，但任何依赖 scaled time 的波次、生态、移动和战斗都不会推进。
- 主流程为：`Main Menu → Start Game → Gameplay → Pause / Settings → Resume → Victory or Defeat → Retry or Main Menu`。
- 游戏中右上角 `MENU` 按钮或 Esc 打开暂停菜单。暂停属于真正的全局暂停：逻辑与普通 Animator 都停止；这和魔王摆放阶段“只停怪物模拟、保留当前动画循环”的特殊暂停不是同一机制。
- Victory / Defeat 出现后同样冻结时间，避免结算背景继续运行；保留既有 R 键重开，同时提供 Retry / Settings / Main Menu 按钮。
- Settings 为 Demo 最小集合：主音量、全屏开关、三档 16:9 分辨率（1280×720 / 1600×900 / 1920×1080）。设置存入 PlayerPrefs；当前虽无正式音效，主音量已直接接到 `AudioListener.volume`，后续音频可以沿用。
- Restart 与返回主菜单都重载当前 `GameScene` 并清空 `FloatingResourcePool`；Restart 把下次入口标记为 Gameplay，Main Menu 把下次入口标记为 MainMenu。以后若制作独立菜单场景，只需替换 `DemoGameFlow` 的导航实现，不需要重写各菜单 UI 状态。
- 当前 UI 仍由 Unity 原生 Image / Text / Button / Slider 在运行时生成，但已通过 `Resources/UI/demo_ui_theme` 接入正式像素皮肤：菜单 / 设置面板、三套按钮四态、通用图标、16 段音量条与 `ui_font_demo_pixel`。文案暂保持英文；中文字体、过渡动效和音频反馈仍按后续任务独立接入，不与流程框架耦合。
- 当前 Demo 没有足够完整且风格一致的按钮图标集，因此所有可交互按钮统一只显示居中文字，不在左侧混用 Play / Home / OpenList 等标识；非按钮状态信息（如 Settings 音量旁的 Sound 图标）仍可使用图标。
- 主菜单的一级标题固定使用项目正式名称 `WHAT DID I DO TO DESERVE THIS, MY LORD`；`DUNGEON LORD` 不作为替代产品名。当前可完整走通最小试玩闭环，因此底部版本标识为 `MINIMUM PLAYABLE DEMO / VERSION 0.1.0`。
- 主题配置只保存构建所需的直接引用，正式图片与字体仍位于 `Assets/Art/UI/**`；运行时代码不依赖 `UnityEditor` / `AssetDatabase`，因此 Player 构建可以沿同一路径加载主题。

### Demo 阶段背景音乐（TASK-093）

- 主菜单循环播放 `bgm_magical_major_theme_07`。
- “非入侵 / 自由准备”是一个连续高层阶段：包含首波准备倒计时、等待玩家重新摆放魔王，以及非最终波全灭后的下一波准备倒计时。每次从其他阶段重新进入时，从 `bgm_dream_on_loop`、`bgm_simple_positive_01_loop`、`bgm_simple_positive_04_loop` 中重新随机一首，并在本阶段持续循环；准备倒计时切到魔王摆放不会再次抽取。
- 魔王完成合法落位、`HeroWaveDirector` 进入本波生成流程时即视为“勇者入侵”。从出生项的首次延迟开始，经过所有生成间隔，直到本波最后一名勇者死亡，持续循环 `bgm_magic_within_loop`。不得仅按场上勇者数量判断，否则会在尚未生成或生成间隔的零勇者空档误切音乐。
- 暂停菜单属于全局硬暂停：暂停当前 BGM 并保持曲目与播放位置，暂停菜单上打开 Settings 仍保持暂停；Resume 后从原位置继续，不重新随机或切曲。主菜单虽然也使用 `Time.timeScale=0`，但不是暂停菜单，仍正常播放主菜单音乐。
- Victory / Defeat 当前暂时静音，后续获得专属结算音乐后再单独配置；本任务不加入淡入淡出、AudioMixer 或音效总线。

---

## 技术约束（第一阶段）

- 已接入 Demo 最小阶段 BGM 调度；音效、AudioMixer、淡入淡出与胜负专属音乐仍未实现
- UI 已具备 Demo 级主菜单 / 暂停 / 设置 / 胜负闭环，并已切换到现有正式像素皮肤；胜负专属面板、动效与声音仍可后续补充
- 不做存档/读档
- 当前只制作 Level 1 的实际内容；数据框架允许后续增加多关卡，但本阶段不额外制作关卡内容

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
| 当前波所有勇者被击败，且仍有后续波次 | 开始下一波准备倒计时 |
| 最终波所有勇者被击败 | 胜利（Victory） |

**实现方式：**
- `HeroLevelConfig.cs` / `HeroArchetypeConfig.cs`：保存关卡波次结构、出生项与勇者职业模板
- `HeroWaveDirector.cs`：推进倒计时、每波魔王摆放、勇者生成、波次清空和最终胜利
- `HeroMover.cs`：使用 `HeroRouteState` 枚举（`GoingToDemonLord` / `ReturningToEntrance`），勇者寻路到魔王单位相邻可通行格，捕获后切换目标为 Entrance
- `DemonLordManager.cs` / `DemonLordRenderer.cs`：维护唯一携带者；携带者死亡时让魔王在当前拖拽位置掉落并恢复普通视图
- `MVPGameManager.cs`：`NotifyHeroEscapedToEntrance` 触发 Defeat；只接受 `HeroWaveDirector` 的“全部波次清空”通知触发 Victory

**魔王单位规则（当前方向）：**
- 魔王是特殊单位，不参与战斗，不能被击杀。
- 每波开始前均可重新摆放；若携带者途中死亡，魔王停留在死亡时的拖拽位置。
- 若携带者死亡后本波仍有其他勇者，其他勇者可以继续捕获；若本波全灭，魔王保持原位直到下波摆放阶段。
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

挖掘表现（TASK-096）：只有 `DigCell` 成功后才在被破坏格中心生成世界空间 `PF_TileBreak_DirtChip`。单次随机发射 4～6 个 32×32 土屑变体，速度 2.2～3.0、寿命 0.28～0.40 秒，理论径向位移约 0.62～1.20 格：应越过当前 1×1 格边界并进入相邻格的一部分，但不得铺满或飞出完整 3×3 区域。使用随机旋转、轻微重力、尾部淡出与轻微缩小；该效果只负责视觉反馈，不参与格子判定、资源扩散或伤害逻辑。非法格、非 Soil 或其他挖掘失败路径不得播放。

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
