# ART_INTAKE_RULES — 美术资源接入规则

本文档规定合作美术发来的资源包如何进入本 Unity 项目，包括目录结构、暂存策略、和不可越界的红线。
**所有 AI 操作必须严格遵守本规则，不得例外。**
配套文档：`ART_NAMING_RULES.md`（命名规则）、`ART_INTAKE_LOG.md`（接入批次日志）。

---

## 一、推荐目录结构

**两层制：Raw（暂存）+ Art（正式）**

```
Assets/
├── Art/                       ← 正式资源（已确认导入、命名规范、Import Setting 已调）
│   ├── Tiles/                 ← Soil 变体 / Entrance / DemonLordRoom（Empty 格无 sprite）
│   ├── Characters/
│   │   ├── Heroes/            ← 勇者（含未来多兵种）
│   │   └── Monsters/          ← Slime 及未来魔物
│   ├── DemonLord/             ← 魔王立绘 / 房间装饰（独立分类）
│   ├── UI/
│   │   ├── Icons/             ← 小图标
│   │   ├── Panels/            ← 胜负面板、HUD 背景
│   │   └── Fonts/             ← 字体（如提供）
│   ├── Backgrounds/           ← 场外背景
│   ├── Props/                 ← 装饰物（宝箱、火把等，后续）
│   └── FX/                    ← 粒子贴图、Sprite Sheet（后续）
└── Art/_Incoming/             ← 临时暂存区（直接平铺，已英文命名，未审核）
```

### 设计原则

- **`Art/`**：仅存放已审核、已命名、已设 Import Setting 的正式资源。
- **`Art/_Incoming/`**：仅供审核的临时区，**直接平铺文件，不按批次建子目录**。批次追踪靠 `ART_INTAKE_LOG.md`，不靠目录结构，避免子目录层级混乱。
- **`_Incoming/` 内的文件已使用规范英文命名**（进入项目即改名，不保留中文原名）。审核通过后直接 move 到 `Art/<category>/`，不需要二次改名。
- **不创建**：`Animations/` / `Materials/` / `Shaders/` 子目录在真正需要时再生，避免空目录污染。

---

## 二、原始资源包的存放策略

合作美术发来的资源包需走 **两段式（项目外 → 项目内）** 流程：

```
[美术发送]
  ↓
[D:\Game Developer Tools\Game Art Drops\MyLord\<原始包名>\]
                              ← Unity 外暂存（保留原始 PSD/AI/源文件，不受命名规则约束）
  ↓ 人类挑选 PNG/导出文件，AI 协助英文命名
[Assets/Art/_Incoming/]      ← Unity 内审核区（扁平 PNG，已规范英文命名）
  ↓ AI 协助应用 Import Setting，人类确认
[Assets/Art/<category>/]     ← 正式资源（move，.meta 随行）
  ↓
[ART_INTAKE_LOG.md]          ← 追加一行批次记录
```

### 为什么两段式

| 方案 | 优势 | 劣势 |
|---|---|---|
| **A. 项目外暂存** | 1. Unity 完全不感知，不会自动导入<br>2. 原始包可保留 PSD/AI 等大文件<br>3. 不污染 git | 需要一步手动/AI 辅助复制+改名 |
| **B. 项目内 `_Incoming`** | 1. Unity 内一站式预览<br>2. 跟 git 走，可回滚 | 任何放入都会被 Unity 自动 import → 立即生成 .meta |

**结论**：项目外保留全部源文件；`_Incoming/` 只放已英文命名的导出件，让 import 行为可控。

### 关键约束

- **项目外暂存路径**：`D:\Game Developer Tools\Game Art Drops\MyLord\`。AI **不要自动创建**，由人类/美术自行维护。
- **`_Incoming/` 直接平铺**：不按批次建子目录，批次信息记录到 `ART_INTAKE_LOG.md`，靠日志追踪不靠目录结构。
- **进入 Unity 即英文命名**：复制到 `_Incoming/` 同时完成改名，文件名符合 `ART_NAMING_RULES.md`。
- **只放扁平 PNG / 导出件**：禁止放 PSD/AI 等原始工程文件。

---

## 三、接入流程（每批必走）

```
1. 项目外：人类从美术交付包中挑选 PNG/导出文件
2. AI 协助：按 ART_NAMING_RULES 确定英文文件名
3. 复制到 Assets/Art/_Incoming/（直接平铺，使用英文命名，不建子目录）
   → Unity 自动 import，生成 .meta，GUID 锁定
4. AI 协助应用 Import Setting（via manage_asset(modify)）：
   - Texture Type: Sprite (2D and UI)
   - Filter Mode: Point (no filter)
   - Compression: None
   - Pixels Per Unit: 48（本项目当前规格）
   - Sprite Mode: Single（单帧）/ Multiple（Sprite Sheet 时）
   - Generate Mip Maps: 关闭
5. 人类在 Unity Inspector 确认 Import Setting 正确
6. AI 将文件 move 到 Assets/Art/<category>/（manage_asset(move)，.meta 随行）
7. 在 ART_INTAKE_LOG.md 追加一行记录
```

每一步都需要人类**明确**授权 AI 才能执行，**不得**跳过确认。

---

## 四、Import Setting 默认值（2D URP 项目）

本项目为 Unity 2022.3.58f1 + URP 2D，所有美术资源默认按以下设置导入：

| 资源类型 | Texture Type | Filter Mode | Compression | Pixel Per Unit |
|---|---|---|---|---|
| 像素风 Tile / Sprite | Sprite (2D and UI) | **Point (no filter)** | None | **48**（本项目当前规格：48×48 px，1格=1 Unity unit） |
| 高分辨率插画 / UI | Sprite (2D and UI) | Bilinear | Normal Quality | 100（Unity 默认） |
| 字体（位图） | Sprite (2D and UI) | Point | None | 按设计 |
| 背景大图 | Sprite (2D and UI) | Bilinear | Normal Quality | 100 |

**材质**：使用 URP 2D 提供的 `Sprite-Lit-Default` / `Sprite-Unlit-Default`，不使用旧 Standard Shader。

---

## 五、风险与红线

| 风险 | 红线 / 规避策略 |
|---|---|
| 覆盖现有资源 | 所有进入 `Art/` 的文件必须先确认不存在同名文件 |
| 污染 `Assets/` 根目录 | 美术资源**只能**进 `Assets/Art/**`；根目录不新增任何文件 |
| Unity 自动导入大批未确认资源 | `_Incoming/` 内只放扁平 PNG / 导出件；原始包留在项目外 |
| 重复 / 残留 `.meta` | 移动文件必须连 `.meta` 一起移动，**不要**手动删 .meta 让 Unity 重生 |
| 直接替换场景对象 | 渲染器替换走"修改 Renderer 代码"路径，不动 Scene 中已有 GameObject |
| 跳过用户确认 | 每个批次的"从 `_Incoming/` → 正式 `Art/`" 必须人工 OK 后才执行 |
| 改 URP Settings | **绝对禁止**改 `Assets/Settings/` 下任何文件（见 UNITY_MCP_RULES § 三-2） |
| 触发 git | 完全不动 Unity 项目内 git（见 UNITY_MCP_RULES § 八） |
| 引入超大 PSD 进仓库 | 原始 PSD 留在项目外 `D:\Game Art Drops\` |
| 像素美术被默认 Bilinear 糊掉 | Import Setting 必须强制设 `Point (no filter)` |
| 一次性导入海量资源 | 单批接入控制在合理规模；超过 50 文件须分批 |

---

## 六、与既有规则的关系

- **UNITY_MCP_RULES.md § 三**：本规则补充"美术资源命名"与"目录边界"维度，不覆盖既有"脚本 / 场景 / 预制体" 规则。
- **UNITY_MCP_RULES.md § 六**：本规则视"覆盖既有美术资源" / "改 URP Settings" 为新增禁止操作。
- **UNITY_MCP_RULES.md § 八**：Git 操作仍由人类负责，AI 接入资源完成后只汇报变更文件清单。
- **AI_UNITY_WORKFLOW_TEMPLATE.md § 二**：本规则是"美术资源接入"系统的数据层规则，遵循"每个 Task 只做一件事"。

---

## 七、未涵盖事项（暂不规定）

以下内容本文档暂不规定，留待后续 Task 决策：

- Animation Clip / AnimatorController 命名（待动画接入时定）
- Sprite Atlas / Sprite Pack 策略（待性能需求出现时定）
- 多分辨率适配（待 UI 设计稿到位时定）
- Addressables / Resources 加载策略（MVP 阶段不引入）
