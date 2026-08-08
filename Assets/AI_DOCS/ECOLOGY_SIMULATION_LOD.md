# ECOLOGY_SIMULATION_LOD — 大量史莱姆生态模拟性能架构

> 建立日期：2026-08-05  
> 目标：史莱姆数量增长时，镜头内与勇者附近维持逐个精确模拟；镜头外降低更新频率；远区使用区域级近似数据流，避免个体 GameObject、Animator、Update 与逐个生态查询无限增长。

## 1. 当前瓶颈

- `EcologyTickDriver` 每个基础 tick 复制并遍历全部 `MonsterData`。
- `MonsterRenderer` 每个 tick 再次收集全部个体，并为所有个体维护视图、Animator 与 `MonsterViewMover.Update()`。
- `MonsterManager` 的按格查询、植物占位和勇者索敌使用全表线性扫描。
- 当前没有“摄像机 / 勇者兴趣区域”概念，也没有 Exact / Reduced / Aggregate 的模拟所有权边界。

## 2. 三级模拟语义

| Tier | 范围 | 数据精度 | Tick | 视觉 |
|---|---|---|---|---|
| Exact | 镜头预热区、勇者保护区、战斗区 | 每个个体精确位置、HP、携带资源和生命周期 | 基础 1 秒 | 完整视图 / Animator / 平滑移动 |
| Reduced | 预热区之外、远区之前 | 保留个体身份，允许时间降采样 | 默认每 4 个基础 tick 处理一次 | 无视图 |
| Aggregate | 长时间远离镜头与勇者的 region | Crawling 只保存人口与资源统计；Bud / Flower v1 保持 Reduced 个体 | 默认每 12 个基础 tick 结算一次 | 无视图 |

Exact 区不是单纯的摄像机矩形，而是以下兴趣源的并集：

1. 摄像机可见矩形 + `exactPaddingCells` 预热边距。
2. 所有勇者位置 + `heroProtectionRadiusCells`。
3. 正在战斗或被显式查询的格子。
4. 玩家刚挖掘、刚生成魔物或即将进入镜头的区域。

## 3. Region

- 默认 `regionSize = 8`，70×50 地图约为 9×7 个 region。
- `MonsterRegionKey` 使用整数 `(rx, ry)`；格子到 region 的映射为 floor division。
- `MonsterRegionState` 最小统计：Crawling 数量、低/中/高 HP 分桶、携带 Nutrient/Magic 总量、四方向数量、最后结算 tick、稳定随机种子。
- v1 不聚合 Bud / Flower，避免丢失植物占位、吸收半径和繁殖时刻等高价值状态。
- 地形连通性仍由 `GridManager` 权威控制；远区跨 region 流动只能经可通行边界格发生。

## 4. 所有权与守恒

每只 Crawling 同一时间只能属于一种所有权：

```text
Exact individual <-> Reduced individual <-> Aggregate region population
```

转换必须满足：

- 个体数守恒；只允许既有生命周期规则产生或消灭魔物。
- `CurrentNutrient` / `CurrentMagic` 总量守恒，定义明确的 Soil / FloatingPool 转移除外。
- Aggregate 展开使用 region seed，结果可复现；不要求恢复原始个体身份和逐格轨迹。
- 勇者索敌、战斗、相机预热或玩家交互发生前，目标 region 必须先物化为个体。
- 不允许个体同时留在 `MonsterManager` 和 `MonsterRegionState` 中，防止双重 tick。

## 5. 表现层策略

- `MonsterRenderer` 只接收 Exact 个体列表，不再自行收集全体魔物。
- 离开 Exact 预热区时，将视图停用并放入对象池；数据个体继续存在或进入聚合。
- 重新进入时从池中取出，重设位置、阶段 AnimatorController 与动画状态。
- 离屏个体不保留活动的 Animator、Coroutine 或 `MonsterViewMover.Update()`。
- 预热边距和层级滞回避免摄像机边缘反复创建 / 回收。

## 6. 调度策略

- 基础时钟仍由 `EcologyTickDriver.tickSeconds` 驱动。
- Exact：每个基础 tick 调用现有精确移动 / HP / 吸放 / 生命周期路径。
- Reduced：每 `reducedTickMultiplier` 个基础 tick 处理一次；保持个体，但允许生态时间近似变慢。
- Aggregate：每 `aggregateTickMultiplier` 个基础 tick 按 region 做 O(region) 结算，而非 O(monster)。
- 每帧只累积一个 timer；不为每只怪物增加新的 MonoBehaviour 或协程。

## 7. 空间索引

`MonsterManager` 同时维护：

- `List<MonsterData>`：稳定遍历与快照。
- `Dictionary<Vector2Int, List<MonsterData>>`：按格查询、堆叠、Bud/Flower 占位和战斗。

所有 Spawn / Move / Remove 必须通过 `MonsterManager` 更新两份结构；`MonsterData.SetPosition` 不再作为外部移动入口。

## 8. 任务清单（阶段 12，拟接 TASK-077 ～ TASK-084）

- [x] **TASK-077** — 本技术规格、三级语义、region、守恒规则、性能预算与验证口径
- [ ] **TASK-078** — `MonsterSimulationPolicy`：兴趣区域与 Exact / Reduced / Aggregate 纯逻辑分类
- [ ] **TASK-079** — `MonsterManager` 空间索引，替换按格和索敌的全表扫描
- [ ] **TASK-080** — `MonsterRenderer` 视图虚拟化与对象池，只维护 Exact 视图
- [ ] **TASK-081** — Exact / Reduced 调度，Reduced 个体降频且无动画事件
- [ ] **TASK-082** — Aggregate region 状态与 Crawling 近似数据流
- [ ] **TASK-083** — 聚合物化、交互保护、数量 / 资源守恒与确定性展开
- [ ] **TASK-084** — 压力验证、回归清单和对外性能说明

## 9. 验收指标

### A 类

- 层级分类边界、摄像机 padding、勇者保护区可直接调用验证。
- Spawn / Move / Remove 后列表与空间索引一致。
- Exact -> Aggregate -> Exact 后数量和携带资源守恒。
- 同一个个体不会被双重模拟。
- 固定 seed 的 region 展开结果可复现。

### B 类

- 场景无需新增组件；现有 `EcologyTickDriver` / `MonsterRenderer` 能发现 Camera、HeroManager、MonsterManager。
- 活动视图数不超过 Exact 个体数；离屏池对象为 inactive。
- Console 0 Error。

### C 类（人工 Play Mode）

- 镜头拖动时无明显刷怪、闪烁或 Animator 状态错乱。
- 勇者在镜头外仍能触发精确索敌和战斗。
- 长时间运行后远区生态趋势合理，重新进入时数量与资源没有明显跳变。
- 500 / 2000 / 更高数量级下记录 CPU frame time、生态 tick 峰值、活动 Animator 数与 GC Alloc。

## 10. 预期性能模型

设总魔物数为 `N`，Exact 个体数为 `E`，Reduced 个体数为 `R`，Aggregate region 数为 `G`：

- 旧生态主循环约为 `O(N)` / 基础 tick，表现同步同样为 `O(N)`，并长期保留 `N` 个 GameObject / Animator / Update。
- 新结构目标约为 `O(E + R/reducedMultiplier + G)` / 基础 tick。
- 活动视图、Animator 和 `MonsterViewMover.Update()` 从 `N` 收敛到约 `E`。
- 当大多数魔物在镜头外且进入 Aggregate 时，远区模拟成本主要随 region 数增长，而不是随远区魔物数增长。

性能数字必须以目标设备 Profiler 为准。架构预期而非实测承诺：

- 仅视图虚拟化：离屏视觉 CPU / Animator / Update 开销按离屏占比近似线性下降。
- Reduced=4：未聚合的离屏个体生态调用量理论下降约 75%。
- Aggregate=12 且每区一次结算：同一区域 100～1000 个远区 Crawling 的生态主循环可从逐个处理压缩为单次区域处理。
- 在 70×50 地图、约 28×16 视窗下，若 Exact 仅占地图局部，预计可把“随全图怪物数线性增长”的主要成本转为“随可见个体 + 活跃 region 增长”。

