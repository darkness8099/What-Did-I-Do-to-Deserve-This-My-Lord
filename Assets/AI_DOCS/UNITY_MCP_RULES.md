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

---

## 七、每个 Task 的标准流程

```
1. 读取 editor_state → 确认 ready_for_tools
2. 读取相关资源 → 了解当前状态
3. 执行操作（单一、小范围）
4. 检查 Console → 确认无错误
5. （可选）截图验证视觉效果
6. 更新 TASKS.md 状态
7. 追加 AI_WORKFLOW_LOG 记录
```
