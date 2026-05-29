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
│   ├── Tiles/                 ← Soil / Empty / Wall / Entrance / DemonLordRoom
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
└── Art/_Incoming/             ← 临时暂存区（合作美术新发来的原始包，未审核）
    └── YYYY-MM-DD_<batch>/    ← 按批次分日期，避免覆盖
```

### 设计原则

- **`Art/`**：仅存放已审核、已命名、已设 Import Setting 的正式资源。
- **`Art/_Incoming/`**：仅供人类挑选的临时区，**不允许**被运行时代码（`GridRenderer` / `HeroRenderer` / `MonsterRenderer` 等）直接引用。
- **批次命名**：`YYYY-MM-DD_<batch>` 保证可追溯到哪一批美术什么时候交付。
- **不创建**：`Animations/` / `Materials/` / `Shaders/` 子目录在真正需要时再生，避免空目录污染。

---

## 二、原始资源包的存放策略

合作美术发来的资源包需走 **两段式（项目外 → 项目内）** 流程：

```
[美术发送]
  ↓
[D:\Game Art Drops\MyLord\YYYY-MM-DD_<batch>\]   ← Unity 外暂存（保留原始 PSD/AI/源文件）
  ↓ 人类挑选 PNG/导出文件
[Assets/Art/_Incoming/YYYY-MM-DD_<batch>/]       ← Unity 内审核区（仅扁平 PNG/导出件）
  ↓ AI 协助审核 + 应用 Import Setting + 命名规范
[Assets/Art/<category>/]                          ← 正式资源
```

### 为什么两段式

| 方案 | 优势 | 劣势 |
|---|---|---|
| **A. 项目外**：`D:\Game Art Drops\MyLord\YYYY-MM-DD\` | 1. Unity 完全不感知，不会自动导入<br>2. 原始包可保留 PSD/AI 等大文件<br>3. 不污染 git | 1. 美术每次要走"手动复制进 Unity"流程<br>2. 离开仓库后版本信息丢失 |
| **B. 项目内 `_Incoming`** | 1. Unity 内一站式预览<br>2. 跟 git 走，可回滚 | 1. **任何放入都会被 Unity 自动 import** → 立即生成 .meta<br>2. PSD/巨大原始包污染仓库 |

**结论**：两段式取双方优点 —— 项目外保留全部源文件，项目内只放可直接使用的导出件。

### 关键约束

- **项目外暂存（A 段）**：路径固定为 `D:\Game Art Drops\MyLord\YYYY-MM-DD_<batch>\`。本目录不归 Unity 管，AI **不要自动创建**，由人类按需建立。
- **项目内审核（B 段）**：**只放扁平 PNG / 导出件**，禁止放 PSD/AI 等原始工程文件。
- **不允许**：直接把美术发来的整包 zip / 文件夹一次性拖入 `Assets/`，必须先在 A 段挑选。

---

## 三、批次接入流程（每批必走）

```
1. 项目外接收原始包，落到 D:\Game Art Drops\MyLord\YYYY-MM-DD_<batch>\
2. 人类挑选 PNG/导出文件
3. 创建 Assets/Art/_Incoming/YYYY-MM-DD_<batch>/，复制挑选后的文件
4. AI 协助：
   - 按 ART_NAMING_RULES 重命名（仅在移动到正式 Art/ 时执行，不改 _Incoming/ 内原名）
   - 应用 Import Setting（Texture Type / Filter Mode / Pixel Per Unit / Compression）
   - 移动到 Assets/Art/<category>/
5. 人类最终确认
6. 在 ART_INTAKE_LOG.md 追加一行记录（日期 / 批次 / 件数 / 处理结果）
```

每一步都需要人类**明确**授权 AI 才能执行，**不得**跳过确认。

---

## 四、Import Setting 默认值（2D URP 项目）

本项目为 Unity 2022.3.58f1 + URP 2D，所有美术资源默认按以下设置导入：

| 资源类型 | Texture Type | Filter Mode | Compression | Pixel Per Unit |
|---|---|---|---|---|
| 像素风 Tile / Sprite | Sprite (2D and UI) | **Point (no filter)** | None | 与美术沟通（常用 16 / 32 / 64） |
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
