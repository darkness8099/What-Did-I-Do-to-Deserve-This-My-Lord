# GAME_DESIGN_SLIME — 匍匐苔藓 / 史莱姆 生态设计

> 本文档是「匍匐苔藓（基础养分系生态单位，美术与代码沿用 `slime` 命名）」的单独设计来源。
> 与 `GAME_DESIGN_BASE.md`（生态化重构总纲）、`GAME_DESIGN_ECOLOGY_REFERENCE.md`（生态参考）配套。
> **匍匐苔藓不是普通 Enemy**：它是生态链最底层的搬运者，玩家通过地形间接调控它，而不是把它当寻路追击的敌人。
>
> 版本：v2（2026-06-13，按用户修正补齐衰弱来源 / 养分不足死亡 / Bud·Flower 带 HP 与失败路径 / 繁殖公式与落位 / 数值入表）。

---

## 0. 命名与定位

| 维度 | 内容 |
|---|---|
| 设计名 | 匍匐苔藓（Creeping Moss） |
| 代码身份 | `MonsterArchetype.Slime`（`Id="slime"`），美术前缀 `monster_slime_*` |
| 生态角色 | `MonsterEcologyRole.NutrientCarrier`（养分搬运者；旧 `Carrier` 已合并删除） |
| 资源轴 | **仅处理 `Nutrient`，不处理 `Magic`**（`MagicCapacity = 0`，设计基线） |

核心作用：
1. 搬运养分（在 Soil 之间吸收 / 释放，制造养分流动）。
2. 支撑低级食物链（被咬咬虫等捕食者的猎物）。
3. 通过 **花苞 → 花** 进行繁殖。
4. 让玩家通过挖掘地形间接控制生态流动（影响其直线移动路线）。

---

## 1. 生命周期总览（状态机）

```text
        [挖开带 Slime 倾向的 Soil → 生成；初始 HP=BaseMaxHP，养分=0]
                               │
                               ▼
                  ┌──────────────────────────┐
                  │  匍匐苔藓 Crawling Moss   │  移动消耗 HP / 吸放养分 / 吸收回血 / 可战斗
                  └──────────────────────────┘
       ┌──────────────┬──────────────────────┬──────────────────────────┐
   被勇者杀死       被捕食              移动/行动使 HP→低           HP 归零(自然衰弱)
       │              │                       │                          │
       ▼              ▼                       ▼                          ▼
  ScatterOrdinary  Transfer        if Nutrient>=2 && HP<=BudHpThreshold   养分<2:
  DeathResources   ResourcesTo       → TransformToBud（不回流）           StarvationFailed
  (回流 Soil)      Predator          (LifecycleTransform)                 → 剩余资源进
  [HeroKill]       [PredatorEat]                                          FloatingResourcePool
                                                                          不生成 Slime
                                            │
                                            ▼
                              ┌──────────────────────────┐
                              │   花苞 Bud                │ 不动/不攻击 / 5×5 吸养分 / 有 HP
                              └──────────────────────────┘
                          养分>=BudToFlowerNutrient(8) │      │ HP 归零且未达阈值
                                      ▼                       ▼
                              ┌──────────────┐      WitherFailed：不生成 Slime，
                              │   花 Flower   │      剩余资源进 FloatingResourcePool
                              └──────────────┘
                       7×7 吸养分，CollectedNutrient 上限 FlowerMaxAbsorb(11)
                                      │ HP 归零 → 繁殖结算（LifecycleWither，非普通死亡）
                                      ▼
                       spawnCount = min(FlowerMaxSpawn, floor(CollectedNutrient / NutrientPerSpawn))
                       在 Flower 格及周围可通行格按固定顺序生成新匍匐苔藓
```

阶段沿用 `slime` 身份，建议以运行时 **生命周期阶段** 表达（见 §7），而非三个独立 archetype。

---

## 2. 基础数值（匍匐苔藓 Crawling Moss）

> **规则 8：以下所有阈值 / 半径 / HP 消耗 / 吸收回血 / 生成数量都放入 `MonsterArchetype`（或怪物数据表），不得写死在行为逻辑里。**

| 字段 | 设计值 | 当前代码 | 说明 |
|---|---|---|---|
| `BaseMaxHP` | 由怪物表决定（暂 10） | `10` ✅ | 初始 HP 来自 archetype，**不由出生土块养分/魔力决定** |
| `BaseAttack` | 暂 2 | `2` ✅ | |
| `AttackRange` | 暂 1 | `1.0f` ✅ | |
| `NutrientCapacity` | **3** | `5` ⚠️ | **需改为 3** |
| `MagicCapacity` | **0** | `0` ✅ | 基础个体不处理魔力 |
| `Move` | 直线移动撞墙转向 | `Static` ⚠️ | 需新增/启用直线弹反策略（见 §4） |
| `HpCostPerMove` | **1~2 / 格**（新增） | — | 每移动 1 格扣 HP；衰弱的唯一来源（见 §3） |
| `HpHealPerAbsorb` | 待定（新增） | — | 吸收养分时恢复的 HP（不超过 `MaxHP`） |

HP 与养分的关系（明确）：
- 初始 HP **只**来自 `MonsterArchetype.BaseMaxHP`，**养分不是初始 HP 来源**。
- 养分用于：吸收时**恢复 HP**、生命周期转化与繁殖。

---

## 3. 自然衰弱来源 + 养分吸收 / 释放（核心生态行为）

### 3.1 衰弱来源（规则 1，2026-06-13 调参）
- 基础匍匐苔藓按 **「移动消耗 HP」** 处理，**不做站立全局掉血**（站着不动不掉血、也不吸放）。
- 节奏：**每移动 `HpCostCooldownMoves`(=2) 格扣 `HpCostPerMove`(=1) HP**。
- 吸收回血 `HpHealPerAbsorb=1`（不超过 MaxHP），**释放不回血**。**故意偏弱**：富养分区只能延缓而非阻止衰弱，否则永生→无法触发转 Bud。
- 完整循环参考（4 格）：HP −2（两次扣血）+ 1 次吸收 +1 = **净 −1**；富养分区慢慢衰弱、贫瘠区更快衰弱。

### 3.2 吸收 / 释放（养分阶梯，`NutrientCapacity = 3`，2026-06-13 修正：消除"卡在 2"死点）
- 交互对象：**上下左右相邻的 Soil**（4 邻，不含斜角）。
- 阶梯行为（在**移动完成后 / 生态 tick** 触发，非每帧）：
  | 当前养分 | 行为 |
  |---:|---|
  | `<= 1`（`AbsorbWhenNutrientLessOrEqual=1`） | 尝试从相邻 **Nutrient>0** 的 Soil **吸收 1 点**，并按 `HpHealPerAbsorb` 回血 |
  | `>= 2`（`ReleaseWhenNutrientGreaterOrEqual=2`） | 尝试向相邻 **Nutrient>0** 的 Soil **释放多余养分**，释放后自身保留 `KeepNutrientOnRelease=1` |
- 因此养分会在 **1↔2 之间持续震荡**（n=2 放到 1，n=1 又吸到 2），苔藓持续吸放、不会卡死；`n=3` 一次释放 2 点回到 1。
- **关键约束（按原作 / 用户实测修正）**：
  - 释放与吸收的目标 **都要求 Soil 且 Nutrient>0**；**绝不向 Nutrient=0 的土释放**。
  - **不做"富土搬到贫土"的平均化**，**不找最贫瘠土块**——只在 4 邻里取第一个 `Nutrient>0` 的 Soil。
  - 周围没有 `Nutrient>0` 土块时：不吸也不放（NoAbsorbTarget / NoReleaseTarget）。
  - **生态行为冷却**（`EcologyActionCooldownMoves=2`）：每移动 2 格最多触发 1 次吸/放；计数只在**实际移动**时累计，**不能原地连续吸放**（Wiki：一次吸放至少移动 1 格）。
  - **释放目标随机**：从 4 邻里 `Nutrient>0` 的土中**随机**选；`surplus>1` 时每 1 点**独立随机**一次目标（允许重复命中，但不固定全给同一格、不找最富/最贫）。
- `BudRequiredNutrient=2` **仅是死亡→转 Bud 的门槛**，不是平时必须保留的储备（平时下限是 `KeepNutrientOnRelease=1`）。
- 自然死亡时 `Nutrient >= 2` 才转 Bud，否则 `StarvationFailed`（见 §5/§6.1）。
- `Empty` Tile **不是资源容器**，禁止写入 Nutrient / Magic（资源只存在于 Soil）。

代码现状：`MonsterData.AbsorbFromTile` 仅生成时吸满；无周期吸放、无释放回 Soil 的怪物侧入口、无 HP 恢复 → 全部待建（见 §7）。

---

## 4. 移动规则

- **低智能移动**：直线前进，**撞墙（非可通行格）后才转向**。
- **禁止**寻路追击勇者（不要 `SeekFood`/A* 追人）。
- 玩家通过**挖掘地形**改变可通行空洞，间接引导苔藓路线。
- 仅在 `Empty`（可通行）格之间移动。
- 代码：建议新增 `MonsterMoveStrategy.StraightBounce` + `MonsterManager.MoveMonster(from,to)`（字典改键 + 渲染同步，当前无）。

### 4.1 接敌时的战斗优先级（TASK-078）

- 匍匐苔藓不会主动寻路追人，但勇者进入 `AttackRange` 后，战斗优先级高于移动与生态循环。
- tick 开始时已在攻击范围内：本 tick 不移动、不扣移动 HP、不吸放养分、不推进生命周期。
- 本 tick 移动后刚进入攻击范围：保留这次移动结果，但从接触发生处立即结束 tick，不再执行本次移动后的 HP / 养分 / 生命周期结算。
- Crawling 个体各自攻击范围内的勇者，因此同格复数 Crawling 可以分别出手；勇者一次普通攻击仍只伤害一个个体。
- Bud / Flower 不主动攻击勇者，但勇者接近时暂停吸收、衰弱、转化或繁殖，避免战斗中继续运行生态阶段。

### 4.2 魔王重新摆放期间（TASK-079）

- 魔王被抓起等待玩家重新放置期间，Crawling / Bud / Flower 全部暂停模拟：不移动、不扣 HP、不吸放、不衰弱、不转化、不繁殖。
- 暂停不积累生态 tick，魔王放好后从原数据继续，不瞬间追赶或补算经过的时间。
- 怪物根节点位置保持不动，但暂停瞬间正在播放的 Animator 状态会在原状态内循环；解除暂停后恢复该状态的正常速度与状态机流转。

---

## 5. 死亡 / 捕食 / 生命周期分流（必须严格区分）

| 触发 | DeathCause / 结果 | 处理 | 代码现状 |
|---|---|---|---|
| 被勇者杀死 | `HeroKill` | `ScatterOrdinaryDeathResources` 回流周围 Soil | ✅ 已实现 |
| 被捕食 | `PredatorEat` | `TransferResourcesToPredator` 进捕食者，不回流 | ✅ API 已实现 |
| 苔藓 `HP<=BudHpThreshold(2)` 且 `Nutrient>=2` | `LifecycleTransform` | **转花苞**（**可在存活时触发**，不必等 HP≤0），不回流 | ✅ 已实现 |
| 苔藓 `HP<=0` 且 `Nutrient<2` | **StarvationFailed** | **不转花苞、不生成 Slime**；剩余资源进 `FloatingResourcePool`（待后续蘑菇/空气资源机制） | ✅ 已实现 |
| 花苞 HP 归零 但养分未达阈值 | **WitherFailed** | **不生成 Slime**；剩余资源进 `FloatingResourcePool` | ✅ 已实现 |
| 花苞养分达标 | — | `Bud → Flower`，**不算死亡** | ✅ 已实现 |
| 花 HP 归零 | `LifecycleWither` | **繁殖结算**生成新苔藓（见 §6.4），**不调用普通死亡回流** | ✅ 已实现 |

**架构红线（已被现有代码保障）**：`ResourceFlow.AllowsOrdinaryDeathScatter` 只放行 `HeroKill`/`EnvironmentDeath`。
故 `HP<=0 → ScatterOrdinaryDeathResources` 不会对衰弱/捕食/生命周期误触发。
**实现时**：苔藓 `HP<=0` 必须**先进入生命周期判断**（`ResolveMonsterLifecycle`），按上表分流，绝不无条件普通散布。
> 实现注：`StarvationFailed` / `WitherFailed` 可作为 `DeathCause` 新成员，或复用 `Starvation`/`LifecycleWither` + 「失败」结果分支；二选一在实现时定，资源去向统一为 `FloatingResourcePool`。

---

## 6. 生命周期详规

### 6.1 转花苞条件（规则 2，2026-06-13 修正：可在存活时触发）
```text
每 tick 检查（不必等死）：
if CurrentHP <= BudHpThreshold(2)
   and CurrentNutrient >= BudRequiredNutrient(2)
→ TransformToBud（LifecycleTransform，不回流；HP 重置为 BudMaxHP，保留养分进 CollectedNutrient）

否则 if CurrentHP <= 0 and CurrentNutrient < 2 → StarvationFailed
否则 → 继续存活、继续衰弱
```
即「养分足够的虚弱苔藓」会主动结苞，而不是非得耗到 HP≤0。
养分不足（`<2`）即便衰弱致死也**不转花苞** → 走 §5 的 `StarvationFailed`。

### 6.2 花苞阶段 Bud（美术：`veg_plant_growth_*`，默认满帧 `veg_plant_growth_09`；枯萎 `veg_plant_death_*`）
- 不移动、不攻击；**有 HP**。
- 从**周围 2 格范围（5×5）**吸收养分（`budAbsorbRadius=2`）。
- **HP 节奏（2026-06-14 修正，避免边吸边掉血枯太快）**：
  - 本 tick **吸到养分** → 只累计 `CollectedNutrient`、**不扣 HP**、`reason=absorbed`、饥饿计数清零；
  - 本 tick **没吸到** → 饥饿计数 +1；只有连续 `BudStarvationCooldownTicks(=3)` 次没吸到才 **−`BudHpDecayPerTick`(1) HP** 并清零计数；
  - 吸收成功或扣血后都重置饥饿计数。
- 累计养分 `>= BudToFlowerNutrient`（**8**，本次不改）→ 转化为花。
- **吸不到养分、饥饿累积致 HP≤0** 且未达 8 → **WitherFailed**：不生成 Slime，剩余资源进 `FloatingResourcePool`。

### 6.3 花阶段 Flower（美术：`veg_flower_bloom_*`，默认 `veg_flower_bloom_05`；枯萎 `veg_flower_death_*`）
- 不作为普通移动怪；**有 HP**。
- 从**周围 3 格范围（7×7）**吸收养分（`flowerAbsorbRadius=3`）。
- `CollectedNutrient` 累计上限 `FlowerMaxAbsorb`（参考 **11**）。
- 后续可追加攻击；当前优先实现吸收与繁殖。
- **HP 归零时才进入繁殖结算**（`LifecycleWither`，非普通死亡，不回流）。

### 6.4 繁殖结算（规则 6 / 7）
- 数量公式：
  ```text
  spawnCount = min(FlowerMaxSpawn, floor(CollectedNutrient / NutrientPerSpawn))
  ```
  - `FlowerMaxSpawn` 参考 **5**（入配置）。
  - `NutrientPerSpawn` 参考 **2**（入配置）。
- 落位规则（2026-06-14 最终：**同格生成**，靠延迟错开而非分散落位）：
  - **所有新 Slime 都生成在花自身格**（魔物无碰撞体积，允许同格）；`actualSpawn` 通常 == `plannedSpawn`，`failReason=none`。
  - **每只新 Slime 设独立随机启动延迟** `random(0, SpawnMoveDelayMaxSeconds=2)`：延迟期间待机（不移动 / 不吸放 / 不扣 HP），延迟结束才进入正常 Crawling。靠延迟错开"逐渐散开"，而非一帧爆散。
- **占位规则**：**同一格只允许 1 个 Bud 或 Flower**（新生 Crawling Slime 不受此限，可同格）。Slime 转 Bud 前检查本格是否已有 Bud/Flower；若有则不转（继续 Crawling 移动，到空格再转）。

---

## 7. 实现建议（与现有架构对接）

> **进度（2026-06-13）**：TASK-060 阶段字段已加（`SlimeLifecycleStage` + `MonsterData.Stage`）。
> TASK-061 Grid 侧就绪：养分字段 `TileAttributeData.Nutrient/Magic` 本就存在；新增 `GridManager.GetNeighborCells4`（非分配 buffer 版）/ `TryGetNeighborCells4`（List 版）/ `HasAbsorbableNutrient`（派生 `IsSoil && Nutrient>0`），每次最多访问 4 邻格。
> 行为侧（吸放/移动/生命周期）按 TASKS 阶段 10 的 062→067 顺序逐步接入。

1. **数值入表（规则 8）**：在 `MonsterArchetype` 增补生命周期字段——
   `HpCostPerMove`、`HpHealPerAbsorb`、`releaseKeepMin`、`BudHpThreshold`、`BudToFlowerNutrient`、`budAbsorbRadius`、`flowerAbsorbRadius`、`FlowerMaxAbsorb`、`FlowerMaxSpawn`、`NutrientPerSpawn`。行为逻辑只读这些字段，不出现魔法数字。
2. **生命周期阶段**：运行时加 `enum SlimeLifecycleStage { Crawling, Bud, Flower }`（挂 `MonsterData`），保持 `slime` 单一 archetype。
3. **释放入口 + HP 恢复**：`MonsterData` 增加把养分写回 Soil（配合 `TileAttributeData.DepositNutrient`，保留≥1）与吸收回血。
4. **移动**：`MonsterMoveStrategy.StraightBounce` + `MonsterManager.MoveMonster`，每格扣 `HpCostPerMove`。
5. **生态 tick 驱动**：当前**无**系统周期遍历怪物（`MonsterData.Tick()` 为空）。新建 `EcologyTickManager`（或 `MonsterManager` 固定间隔 tick），驱动：吸放/回血 → 移动(扣 HP) → `ResolveMonsterLifecycle`。逻辑用 `execute_code` 直接调方法验证（帧推进不保证）。
6. **分流挂点**：统一 `ResolveMonsterLifecycle(monster, cause)`，内部分派 转花苞 / StarvationFailed / Bud→Flower / WitherFailed / Flower 繁殖 / 普通回流。
7. **繁殖落位 helper**：固定顺序遍历 origin + 邻格，跳过非 `Empty`/有怪格。

### 美术映射
| 阶段 | 默认帧 | 动画 |
|---|---|---|
| 匍匐苔藓 Crawling | `monster_slime_move_00` | move / attack(复用 move) / absorb / emit(absorb 倒放) / death |
| 花苞 Bud | `veg_plant_growth_09` | plant_growth(成长) / plant_death(枯萎) |
| 花 Flower | `veg_flower_bloom_05` | flower_bloom(绽放) / flower_death(枯萎) |

---

## 8. 配置参数清单（全部入 `MonsterArchetype` / 数据表）

| 参数 | 参考值 | 说明 |
|---|---|---|
| `NutrientCapacity` | 3 | 匍匐苔藓个体养分上限 |
| `HpCostPerMove` | 1 | 每次 HP 消耗扣的量（衰弱唯一来源） |
| `HpCostCooldownMoves` | 2 | 每移动几格扣 1 次 HP（仅按实际移动计数） |
| `HpHealPerAbsorb` | 1 | 每次吸收回的 HP（**不超过 MaxHP**；故意偏弱，避免富养分区永生 → 仍能触发转 Bud） |
| `AbsorbWhenNutrientLessOrEqual` | 1 | 养分 ≤ 此值 → 从 Nutrient>0 邻土吸 1 + 回血 |
| `ReleaseWhenNutrientGreaterOrEqual` | 2 | 养分 ≥ 此值 → 向 Nutrient>0 邻土释放多余（**释放不回血**） |
| `KeepNutrientOnRelease` | 1 | 释放后自身保留（平时下限，非繁殖储备） |
| `BudRequiredNutrient` | 2 | 转花苞所需养分门槛（非平时保留量） |
| `EcologyActionCooldownMoves` | 2 | 每移动几格才允许 1 次吸/放（仅按实际移动计数） |
| `BudHpThreshold` | 2 | **HP ≤ 此值且养分 ≥ BudRequiredNutrient → 转花苞（可在存活时触发，不必等 HP≤0）** |
| `BudToFlowerNutrient` | 8 | 花苞累计养分达此值 → 转花 |
| `budAbsorbRadius` | 2（5×5） | 花苞吸养分范围 |
| `BudHpDecayPerTick` | 1 | 花苞饥饿扣血量 |
| `BudStarvationCooldownTicks` | 3 | 连续几次没吸到养分才扣 1 次 HP（吸到则不扣、并清零） |
| `flowerAbsorbRadius` | 3（7×7） | 花吸养分范围 |
| `FlowerMaxAbsorb` | 11 | 花 `CollectedNutrient` 上限 |
| `FlowerMaxSpawn` | 5 | 单朵花繁殖上限 |
| `NutrientPerSpawn` | 2 | 每只新苔藓消耗的养分 |
| `SpawnMoveDelayMaxSeconds` | 2.0 | 花生新苗的随机启动延迟上限（random(0,此)，延迟期待机） |

---

## 9. 剩余待澄清（少量数值，可后续补）

1. `HpHealPerAbsorb`：每次吸收回多少 HP。
2. **吸放/移动的 tick 周期**：生态 tick 间隔（当前各阶段均 1.0s，可考虑合并为单一 `EcologyTickSeconds`）。
3. `HpCostPerMove` 在 1~2 内取定值还是随机（`UseRandomMoveHpCost`）。

> 已定（2026-06-13）：养分阶梯 ≤1 吸 / =2 稳定 / =3 释放回 2；释放下限＝繁殖储备 2。详见 §3.2。

---

## 10. 实现任务拆分（建议，遵守「单系统小步」规则；编号待确认）

1. 数值入表：`Slime.NutrientCapacity` 5→3，并补全 §8 全部 archetype 字段（占位默认值）。
2. `SlimeLifecycleStage` 枚举 + `MonsterData` 阶段字段。
3. 养分**释放** + **HP 恢复**（`MonsterData` 方法，`execute_code` 单测）。
4. `MonsterMoveStrategy.StraightBounce` + `MonsterManager.MoveMonster`（每格扣 HP）。
5. `EcologyTickManager`：固定间隔驱动 吸放/回血（先不接移动与生命周期）。
6. `ResolveMonsterLifecycle`：转花苞 + StarvationFailed（剩余进 FloatingResourcePool）。
7. 花苞 5×5 吸收 + →花阈值；WitherFailed 路径。
8. 花 7×7 吸收（上限 11）+ HP 归零繁殖结算（公式 + 固定顺序落位）。
9. 渲染：阶段切换 sprite/动画。
10. 移动接入 tick + 撞墙转向；玩家挖掘改变路线验证。

> 每步：改后 `read_console` 0 Error → `execute_code` 逻辑验证 → 更新 `TASKS.md` + `AI_WORKFLOW_LOG.md`。视觉/手感交人工（C 类）。
