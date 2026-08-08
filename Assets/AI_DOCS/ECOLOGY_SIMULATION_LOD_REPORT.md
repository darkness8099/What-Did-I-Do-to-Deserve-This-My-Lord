# ECOLOGY_SIMULATION_LOD_REPORT — 实现与性能预期

> 完成日期：2026-08-05  
> 状态：代码实现与 A/B 类验证完成；未进入 Play Mode，真实帧率与视觉切换留作 C 类人工验证。

## 1. 已完成任务

| Task | 状态 | 结果 |
|---|---|---|
| TASK-077 | 完成 | `ECOLOGY_SIMULATION_LOD.md`：三级模拟、region、守恒、预算与验证规格 |
| TASK-078 | 完成 | `MonsterSimulationPolicy`：Camera + padding + Hero 保护区分类 Exact / Reduced / Aggregate |
| TASK-079 | 完成 | `MonsterManager` 空间索引；Spawn / Move / Remove / RemoveMany 同步，按格查询不再全表扫描 |
| TASK-080 | 完成 | `MonsterRenderer` 只同步 Exact 列表；离屏视图进入 inactive 对象池并可复用 |
| TASK-081 | 完成 | Exact 每基础 tick；Reduced 默认每 4 tick；离屏不发送动画事件 |
| TASK-082 | 完成 | 8×8 `MonsterRegionState`；远区 Crawling 折叠为人口、HP、养分、方向统计，每 12 tick 粗结算 |
| TASK-083 | 完成 | 区域物化、资源/数量恢复、摄像机预热；勇者索敌与战斗前强制物化 |
| TASK-084 | 部分完成 | 编译、逻辑压力测试、性能模型和报告完成；Player Profiler / 长时间视觉测试待人工 |

## 2. 实现后的运行模型

```text
Camera / Hero interest
        |
        v
MonsterSimulationPolicy
  | Exact       -> per-monster 1s tick -> visible view / Animator / smooth mover
  | Reduced     -> per-monster 4s tick -> data only, no visual event
  | Aggregate   -> capture Crawling into 8x8 region state
                               |
                               v
                   per-region 12s approximate flow
                               |
             Camera/Hero approaches or combat query
                               |
                               v
              deterministic materialization to individuals
```

### Exact

- 摄像机可见区加 4 格预热边距。
- 勇者周围默认 8 格保护半径。
- 使用原有逐格移动、HP、吸放、Bud / Flower、战斗和动画流程。

### Reduced

- 默认位于摄像机 4～12 格缓冲带或远区植物。
- 保留完整 `MonsterData` 身份。
- 默认每 4 个基础 tick 执行一次，不保留离屏视图、不触发动画事件。
- Bud / Flower v1 不进入 Aggregate，避免植物占位和繁殖时刻失真。

### Aggregate

- 默认 region 8×8。
- Crawling 个体从 `MonsterManager` 批量移除，避免继续被逐个 tick。
- 区域只保留：数量、总 HP、携带 Nutrient/Magic、四方向分布、seed 和最后结算 tick。
- 每 12 个基础 tick 做一次近似 HP 经济、区域内养分搬运、Bud 生成与少量跨 region 流动。
- 区域靠近摄像机 / 勇者时，根据 seed 在可通行格确定性展开。

## 3. 关键性能处理

### 3.1 离屏视觉对象归零

旧版所有魔物长期持有 GameObject、Animator 和 `MonsterViewMover.Update()`。新版只为 Exact 个体保留活动视图；离开 Exact 区后 GameObject 被设为 inactive 并进入对象池。

结果：活动 Animator、每帧 MonoBehaviour Update、渲染器和 Transform 插值数量从总魔物数 `N` 收敛为可见/预热个体数 `E`。

### 3.2 生态主循环不再始终 O(N)

旧版每秒逐个处理全部魔物。新版基础 tick 近似成本：

```text
O(E + R / 4 + G / 12)
```

- `E`：Exact 个体数。
- `R`：Reduced 个体数。
- `G`：Aggregate region 数，通常最多几十，而不是远区个体数。

### 3.3 空间查询

- 按格战斗查询：由全表扫描改为 Dictionary cell lookup。
- Bud / Flower 占位：只扫描同格小列表。
- 勇者索敌：只扫描攻击半径覆盖的格子，而不是全部魔物。
- 大批远区折叠：使用 `RemoveMany + HashSet + List.RemoveAll`，避免逐只 `List.Remove` 的 O(N²)。

### 3.4 精确玩法保护

- 勇者索敌前调用 `EnsureExactAround`。
- 战斗开始前再次确保目标 region 已物化。
- 摄像机以 0.2 秒间隔刷新兴趣视图，并使用预热边距减少镜头边缘刷出感。
- `GridData` 仍然是逐格 Soil 养分真相源，区域模拟通过有限次数的 tile delta 反映大致资源流。

## 4. 已完成验证

### 分类

- 镜头内 -> Exact。
- 缓冲带 -> Reduced。
- 远区 -> Aggregate。
- 镜头外但靠近 Hero -> 强制 Exact。

### 空间索引

- 同格 2 个个体可堆叠。
- Move 后旧格 / 新格查询正确。
- Remove 后索引与主列表一致。

### 视图池

```text
1 个 Exact：active=1, pool=0
离开 Exact：active=0, pool=1
另一只进入：active=1, pool=0
```

说明重新进入时复用了池对象，没有创建第二个活动视图。

### 折叠 / 展开守恒

测试 2 个个体：

```text
折叠前：individual=2, HP=20, nutrient=3
折叠后：individual=0, aggregate=2, nutrient=3
展开后：individual=2, HP=20, nutrient=3, aggregate=0
```

空间索引保持一致，无双重所有权。

### 完整 Driver 边界

20 个远区 Crawling 在第 12 个基础 tick 后：

```text
individual=0, aggregate=20, regions=1
```

调用勇者/战斗保护物化后：

```text
individual=20, aggregate=0, spatialIndexValid=true
```

### 合成压力测试

在 Edit Mode 主线程直接调用纯逻辑路径；不含渲染、Player Loop 和真实设备差异：

| 数量 | 折叠后 region | 批量折叠耗时 | 单次 region 粗结算 |
|---:|---:|---:|---:|
| 500 | 13 | 首次约 3.7 ms（包含首次 JIT / 热身影响） | 首次约 3.0 ms（同样包含热身） |
| 2000 | 30 | 热身后 5 次约 0.827～1.135 ms | 热身后约 0.041～0.059 ms |

这些数字只证明算法已经从“每次处理 2000 个个体”压缩为“处理约 30 个 region”，不能替代目标设备上的 Unity Profiler。

## 5. 可对外使用的优化效果描述

### 保守技术版

项目为大量生态单位实现了三级 Simulation LOD。镜头内和勇者附近继续逐个模拟移动、战斗与生命周期；镜头外个体降低更新频率；长时间远离交互区的史莱姆被折叠为 8×8 区域级人口与资源流。与此同时，离屏 GameObject、Animator 和逐帧移动组件会被回收到对象池。由此，核心开销从“随全地图魔物总数线性增长”，转变为主要随“当前可见个体数与活跃区域数”增长。

### 展示版

为了让生态规模能够持续扩张，项目没有简单冻结屏幕外单位，而是采用了分层生态模拟：玩家能看到、能战斗的区域保持完整精度；邻近区域以低频个体模拟维持连续性；远区则转化为区域级数据流。大量史莱姆即使遍布地图，也不再要求每只都保有 Animator、GameObject 和每秒 AI tick；重新进入区域时再根据守恒数据恢复出合理的个体状态。

## 6. 性能预期（非实测承诺）

假设 2000 个史莱姆中：200 个 Exact、300 个 Reduced、1500 个折叠到约 30 个 region：

- 活动视图 / Animator：约从 2000 降到 200，理论减少约 90%。
- 个体生态调用：Exact 200 + Reduced 平均 75；远区改为约 30 region / 12 tick，不再逐只处理。
- 生态主循环的主要工作量可比全量逐个 tick 下降约 80%～90% 量级。
- 实际 FPS 增益取决于目标设备、可见个体密度、Animator 成本、GridRenderer 与 URP 渲染，必须以 Player Profiler 为最终结论。

## 7. 人工 C 类验证

1. 进入 Play Mode，拖动镜头跨越 region，确认无明显闪烁、重复生成或动画状态错乱。
2. 让勇者在镜头外接近史莱姆，确认索敌和战斗仍精确发生。
3. 观察远区重新进入时的数量、HP、养分和 Bud 状态是否自然。
4. 用 500 / 2000 / 更高数量记录：CPU Main Thread、Scripts、Animator、GC Alloc、活动 GameObject 与生态 tick 峰值。
5. 长时间运行，确认 region 近似不会快速清空生态或造成不合理爆发。

## 8. 边界

- 未进入 Play Mode。
- 未修改或保存 Scene。
- 未修改 ProjectSettings / Packages / Build Settings。
- 未执行任何 Git 操作。
- `TASKS.md` 因本次 Windows 补丁工具无法读取已有 `Assets` 文件而未直接追加；正式阶段清单保存在 `ECOLOGY_SIMULATION_LOD.md`，实现状态以本报告为准。

