# UNITY_MCP_RULES — AI 操作 Unity 的行为规则

本文档规定 Claude Code 通过 Unity MCP 操作本项目的所有行为准则。
**所有 AI 操作必须严格遵守本规则，不得例外。**

---

## 一、操作前必须检查

1. **读取 Editor 状态**：每次工具操作前，确认 `editor_state.ready_for_tools == true`
2. **确认无编译中**：`is_compiling == false`，`is_domain_reload_pending == false`
3. **确认当前 Scene**：通过 `manage_scene(get_active)` 确认操作目标场景
4. **阅读相关资源**：使用 Resources 读取现有状态，再用 Tools 修改

---

## 二、代码修改规则

1. **修改代码后立即检查 Console**
   - 使用 `read_console(types=["error","warning"])` 确认无编译错误
   - 等待 `is_compiling == false` 后再进行下一步操作

2. **每次只修改一个系统**
   - 不要一次性生成多个相互依赖的脚本
   - 一个脚本编译通过后，再创建下一个

3. **禁止一次性生成完整复杂系统**
   - 每个脚本功能单一，保持在 100 行以内（第一阶段）
   - 复杂逻辑拆分为多个 Task 逐步实现

---

## 三、场景与资源规则

1. **非必要不保存 Scene**
   - 每个 Task 完成并测试通过后，由人工决定是否保存 Scene
   - AI 不得自动执行 `manage_scene(save)`

2. **不修改正式资源**
   - 不修改 Assets/Settings 下任何文件
   - 不修改 Package 配置
   - 不修改 Build Settings
   - 不修改 Project Settings

3. **创建资源命名规范**
   - 脚本：`PascalCase.cs`，放在 `Assets/Scripts/`
   - 场景：放在 `Assets/Scenes/`
   - 预制体：放在 `Assets/Prefabs/`
   - 文档：放在 `Assets/AI_DOCS/`

---

## 四、批量操作规则

1. **优先使用 `batch_execute`** 进行多个独立操作
2. **依赖操作必须串行**：A 编译完成 → 检查 Console → B 操作
3. **每批不超过 10 个命令**（保守上限，方便追踪问题）

---

## 五、问题处理规则

1. **遇到错误立即停止**，不要尝试绕过
2. **编译错误必须修复**，不得跳过进行下一步
3. **不确定的操作必须向人工确认**，不得自行扩大操作范围
4. **每次操作失败后记录原因**（写入 AI_WORKFLOW_LOG）

---

## 六、禁止操作清单

| 禁止操作 | 原因 |
|----------|------|
| 自动保存 Scene | 可能覆盖人工调整 |
| 自动保存 Project | 可能产生不可回滚的变更 |
| 修改 Package 依赖 | 影响全局编译环境 |
| 一次性生成超过 3 个脚本 | 难以追踪编译错误 |
| 删除非测试用 GameObject | 可能破坏正式场景 |
| 在 Production Branch 直接大幅修改 | 需要版本控制保护 |
| **执行任何 git 操作** | **Git 管理权限归属人类，见第八节** |

---

## 七、每个 Task 的标准流程

```
1. 读取 editor_state → 确认 ready_for_tools
2. 读取相关资源 → 了解当前状态
3. 执行操作（单一、小范围）
4. 检查 Console → 确认无错误
5. 逻辑验证：execute_code 直接调用 public 方法（见第九节 A 类测试）
6. 更新 TASKS.md 状态
7. 追加 AI_WORKFLOW_LOG 记录
8. 汇报格式遵循第十节
```

---

## 八、Git 操作权限规则（TASK-028 新增）

**Git 项目管理全部由人类开发者手动负责。**

### AI 绝对禁止执行的操作

- `git add` / `git commit` / `git push`
- `git merge` / `git rebase`
- `git checkout` 切换分支
- 删除分支 / 创建 tag

### AI 允许做的事情（仅限汇报和建议）

- 汇报有哪些文件被修改
- 建议 commit 标题和描述文字（供人类复制使用）
- 提醒用户在 GitHub Desktop 中查看 Changes
- 提醒用户手动创建分支或 tag

### 注意

AI 在任何情况下都不得绕过本规则执行 git 操作，即使用户指令中含糊地说"提交一下"。
正确做法：汇报变更文件列表 + 提供建议 commit 文字 + 请用户手动执行。

---

## 九、测试分类规则（TASK-028 新增）

### A 类：AI 必须执行的测试

- `refresh_unity` + `read_console(types=["error"])` — 每次脚本修改后
- 编译是否成功（零 Error）
- `execute_code` 直接调用 public 方法验证状态变化（不依赖帧推进）
- 检查对象是否存在、字段是否正确、Dictionary 状态

### B 类：AI 可简短执行的测试

- 检查 GameObject 挂载情况（`find_gameobjects`）
- 验证方法返回值是否符合预期（单次 `execute_code`）

### C 类：交给人类手动验证（AI 不等待）

AI 对以下内容只需说明"请由人类手动进入 Play Mode 验证"，不得反复等待帧推进：

| 不在 AI 测试范围内的内容 |
|--------------------------|
| 协程动画是否流畅 |
| Hero 实际移动表现 |
| Game View 视觉效果 |
| UI 显示位置是否美观 |
| 长时间 Play Mode 流程 |
| 摄像机截图 / 录屏确认 |
| 手感、节奏、可读性 |

### Unity MCP 时间推进限制

`execute_code` 在 Unity 主线程上同步执行，两次工具调用之间**不保证**游戏帧推进：

- `Time.time` 可能不推进
- 协程 (`yield return null` / `WaitForSeconds`) 可能不继续
- 依赖帧更新的验证会浪费大量 Token

**对策**：逻辑验证优先通过 `execute_code` 直接调用 public 方法；视觉表现由人类手动测试确认。

---

## 十、任务完成汇报格式（TASK-028 新增）

每次 Task 完成后，AI 必须按以下格式汇报：

```
1. 修改了哪些文件
2. 是否修改了代码（是/否，若是则列出文件）
3. 是否修改了场景（是/否）
4. 是否执行了 Git 操作
   → 正确答案：未执行，Git 操作由用户负责
5. Console 是否有 Error（是/否）
6. AI 已完成的验证（A/B 类测试结果）
7. 建议用户手动验证的项目（C 类测试清单）
8. 下一步建议（可选）
```
