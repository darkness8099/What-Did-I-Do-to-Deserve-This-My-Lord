# AI + Unity MCP 制作工作流模板

**来源项目**：What Did I Do to Deserve This, My Lord  
**验证阶段**：TASK-000 → TASK-022（空项目 → First Playable MVP）  
**整理日期**：2026-05-27  

本文档整理了本项目从零到 First Playable MVP 过程中形成的 AI（Claude Code + Unity MCP）工作流。
内容来自实际执行记录（AI_WORKFLOW_LOG.md），不含未发生的内容。

---

## 一、总体原则

```
人类决策方向  →  AI 执行操作  →  人类验收结果
```

- **任务粒度**：每个 Task 只做一件事，完成后再开始下一个
- **边界明确**：每条 Task 指令中写清楚"不要做什么"，与"要做什么"同等重要
- **先验证后推进**：每个 Task 完成后必须确认 Console 无 Error，再进入下一 Task

---

## 二、任务拆分顺序

### 推荐层次顺序

```
1. 数据层      纯 C# 类，不继承 MonoBehaviour，只定义属性和方法
2. 管理层      MonoBehaviour，控制数据状态，提供操作接口
3. 表现层      MonoBehaviour，读取数据，创建/更新/删除 GameObject
4. 交互层      MonoBehaviour，处理输入或系统事件，调用管理层接口
5. 集成/测试   端到端验证，覆盖正常流程和边界保护
```

每个系统（如网格系统、魔物系统、勇者系统）都单独走这 5 层，不要跨系统合并实现。

### 本项目实践路径

```
【网格系统】
GridData(数据) → GridManager(管理) → GridRenderer(表现) → InputHandler-挖掘(交互)

【魔物系统】
MonsterData(数据) → MonsterManager(管理) → MonsterRenderer(表现) → InputHandler-放Slime(交互)

【勇者系统】
HeroData(数据) → HeroManager(管理) → HeroPathfinder(逻辑) → HeroRenderer(表现) → HeroMover(交互)

【游戏系统】
CombatSystem(逻辑) → MVPGameManager(状态) → MVPResultUI(表现)

【验证】
端到端测试（Defeat 流程 / Victory 流程 / 边界保护 / 对象层级）
```

---

## 三、Claude Code + Unity MCP 工具使用规则

### 3-1. 工具选择对照表

| 操作 | 推荐工具 | 注意事项 |
|------|---------|---------|
| 创建新脚本 | `create_script` | 自动触发 AssetDatabase.Import + 编译，无需手动 refresh |
| 修改现有脚本 | `script_apply_edits` | 结构化局部修改，优先于全文覆写 |
| 等待编译完成 | `refresh_unity(wait_for_ready=true)` | 编译完成前不要挂载组件或进入 Play Mode |
| 确认无编译错误 | `read_console(types=["error"])` | 每次脚本修改后必须执行 |
| 挂载组件 | `manage_components(add, component_type)` | 目标：instance ID 或 GameObject 名称 |
| 保存场景 | `manage_scene(save)` | 挂载组件后和退出 Play Mode 前执行 |
| 进入/退出 Play Mode | `manage_editor(play/stop)` | Play/Stop 之间等待状态稳定 |
| 程序化逻辑验证 | `execute_code` | 最可靠的功能测试方式，避免实时时序问题 |
| 视觉截图验证 | `execute_code` 内 `Camera.Render()` + `ReadPixels()` | 见 3-2 |
| 批量查询/操作 | `batch_execute` | 减少往返，parallel=true 用于无依赖的只读查询 |
| 查找场景对象 | `find_gameobjects(search_method)` | 支持 by_name / by_component / by_tag，include_inactive=true 可找到隐藏对象 |

### 3-2. 视觉验证的正确做法

**背景**：在 Editor Play Mode 下：
- `ScreenCapture.CaptureScreenshotAsTexture()` 返回 null（仅在 Standalone Build 有效）
- `manage_camera(screenshot)` 使用相机渲染路径，不捕获 Screen Space Overlay Canvas
- `execute_code` 占用主线程，两次调用之间 Unity 的 Update() 不保证执行

**推荐做法**：在单次 `execute_code` 中一次性完成"状态修改 + UI 直接更新 + 截图"。

```csharp
// 在一次 execute_code 调用中完成所有操作：
// 1. 修改状态
// 2. 直接更新 UI（绕过 Update 帧边界）
// 3. Camera.Render() → RenderTexture → ReadPixels() → 保存 PNG

var cam = Camera.main;
var rt = new RenderTexture(w, h, 24);
cam.targetTexture = rt;
cam.Render();                        // 同步渲染当前帧
cam.targetTexture = null;
var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
RenderTexture.active = rt;
tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
tex.Apply();
RenderTexture.active = null;
Destroy(rt);
File.WriteAllBytes(path, tex.EncodeToPNG());
Destroy(tex);
```

**Canvas 建议**：使用 `RenderMode.ScreenSpaceCamera`（绑定 Camera.main），而非 `ScreenSpaceOverlay`。
前者可被 Camera.Render() 路径捕获，适合 URP 项目。

### 3-3. 编译与 Domain Reload 等待流程

```
create_script / script_apply_edits
  → [Unity 自动触发编译]
  → refresh_unity(wait_for_ready=true)    ← 等待 domain reload 完成
  → read_console(types=["error"])         ← 确认零 Error
  → [可以挂载组件、进入 Play Mode]
```

不要在编译完成前操作场景或进入 Play Mode，否则可能触发文件锁（"另一个程序正在使用此文件"）。

---

## 四、常见陷阱与规避方法

### 陷阱 ① AI 范围蔓延（Scope Creep）

**现象**：AI 在完成当前 Task 后，主动追加实现后续 Task，或自动将多个任务标记为完成。

**规避**：
- 每条 Task 指令中明确写"不要自动标记 TASK-XXX 及之后任务"
- 明确列出"本次不做什么"（如"不做动画、不做音效、不做 Restart"）

---

### 陷阱 ② 脚本整体覆写

**现象**：AI 修改现有脚本时，直接覆写全文而非局部修改，破坏相邻逻辑。

**规避**：
- 要求使用 `script_apply_edits`（replace_method / insert_method / anchor_replace）
- 明确说明"不要重写整个文件"
- 仅在新建文件时使用 `create_script`

---

### 陷阱 ③ "无 Unity 依赖"的误解

**现象**：AI 描述某脚本为"纯 C# 类，完全无 Unity 依赖"，但实际代码中仍使用 `UnityEngine.Mathf`、`UnityEngine.Debug` 等。

**规避**：
- 明确区分两个概念：
  - "不继承 MonoBehaviour"（不是组件，不挂载到 GameObject）
  - "不使用 UnityEngine 命名空间"（真正的纯 C#）
- MVP 阶段数据层允许使用 `UnityEngine.Mathf` 等工具类，只要不继承 MonoBehaviour 即可

---

### 陷阱 ④ Game View 无焦点时协程变慢

**现象**：Hero 移动速度异常缓慢，怀疑是 HeroMover 代码 bug。

**原因**：Unity Editor 的 Background Throttling 行为——Game View 未获焦点时降低帧率，导致 `Time.deltaTime` 变大但整体推进变慢。

**规避**：
- 不是 bug，不修改代码
- 可在 Edit → Project Settings → Player → Resolution and Presentation 中开启 "Run In Background"
- 测试时点击 Game View 窗口使其获焦

---

### 陷阱 ⑤ MCP 工具调用与 Unity 帧边界

**现象**：在 `execute_code` 调用 1 中修改了状态，调用 2 中检查 UI，发现 UI 未更新（Update() 未执行）。

**原因**：`execute_code` 在主线程上同步执行，占用主线程期间 Unity 不处理帧（Update、协程均暂停）。两次 MCP 调用之间虽然 Unity 可能处理若干帧，但不可依赖。

**规避**：
- 将"修改状态 + 直接更新 UI + 截图"合并到单次 `execute_code`
- 不依赖 Update() 在两次工具调用之间自动执行

---

### 陷阱 ⑥ Destroy() 帧边界视觉残留

**现象**：战斗结算后截图，已被消灭的 Slime 视图仍然可见。

**原因**：Unity 的 `Destroy()` 不立即执行，在帧末才移除对象。若截图发生在同一帧内（Camera.Render() 在 Destroy() 之后但帧末之前），对象仍然可见。

**规避**：
- 这是 Unity 的正常行为，实际游戏中玩家看到的是正确的（下一帧消失）
- 测试截图时接受此视觉延迟，不误判为 bug

---

### 陷阱 ⑦ ScreenSpaceOverlay vs ScreenSpaceCamera

**现象**：`manage_camera(screenshot)` 截图不显示 Canvas 上的 UI 元素。

**原因**：`ScreenSpaceOverlay` Canvas 不走相机渲染路径，Camera.Render() 和 manage_camera 的截图均无法捕获。

**规避**：
- 使用 `RenderMode.ScreenSpaceCamera` 并绑定 Camera.main
- 此模式在 URP 中表现等同，且可被相机截图路径捕获

---

## 五、人类监督者需要判断的事项

以下决策不应完全交给 AI，需要人类确认后再执行：

### 5-1. 任务边界

- 当前 Task 的范围是什么（做什么/不做什么）
- 是否需要新增功能，还是只做最小修复
- Bug 修复的范围是否在任务允许范围内

### 5-2. MVP 设计决策

- 简化规则是否可接受（如"点击 Empty 放 Slime"而非自动生成）
- 当前数值基线是否合理（Hero HP/ATK、Slime HP/ATK）
- 胜负条件是否符合预期

### 5-3. 方案选择

- AI 提出多种实现方案时，选择符合当前阶段目标的最简方案
- 是否接受临时规则（后续重构），还是要求一次做对
- 是否可以跳过视觉验证（如截图工具受限时）

### 5-4. 阶段推进

- 当前 Task 是否真正完成（测试结果是否满意）
- 是否进入下一阶段，还是在当前阶段继续打磨
- 哪些功能放入 MVP，哪些推迟到后续阶段

### 5-5. 质量验收

- Console 的 Warning 是否可接受（不一定要求零 Warning）
- 运行时对象的视觉表现是否达到预期（Primitive 占位是否够用）
- 游戏流程是否完整可玩（不要求完美，但要求可验证）

---

## 六、Task 指令写作模板

以下是一条有效 Task 指令的结构参考（基于本项目积累）：

```
继续执行 TASK-XXX：[任务标题]

重要目标：
[用 1-3 句话说明本次 Task 要达到的结果]

重要限制（不做以下事项）：
- 不要做 [功能A]
- 不要做 [功能B]
- 不要修改 [现有系统]

具体要求：
1. [步骤1]
2. [步骤2]
...

测试要求：
- 测试 1：[测试场景和预期结果]
- 测试 2：[测试场景和预期结果]

完成后汇报：
- 创建/修改了哪些文件
- [关键验证项]
- Console 是否有 Error
- 下一步建议是什么

只更新 TASK-XXX，不要自动标记 TASK-YYY 及之后任务。
```

---

## 七、快速检查清单

每个 Task 完成后，依次确认：

- [ ] 脚本编译无 Error（`read_console(types=["error"])`）
- [ ] 新脚本已挂载到正确 GameObject
- [ ] Play Mode 进入无 Error
- [ ] 核心功能通过程序化验证（`execute_code`）
- [ ] 场景已保存（`manage_scene(save)`）
- [ ] TASKS.md 标记当前 Task 为 `[x]`
- [ ] AI_WORKFLOW_LOG.md 追加本次记录
