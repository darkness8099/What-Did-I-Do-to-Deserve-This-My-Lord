# Codex Project Entry

This Unity project uses `Assets/AI_DOCS` as the source of truth. Read these files before making or proposing project changes:

1. `Assets/AI_DOCS/TASKS.md`
2. `Assets/AI_DOCS/UNITY_MCP_RULES.md`
3. `Assets/AI_DOCS/GAME_DESIGN_BASE.md`
4. `Assets/AI_DOCS/AI_UNITY_WORKFLOW_TEMPLATE.md`
5. `Assets/AI_DOCS/GAME_DESIGN_SLIME.md`（匍匐苔藓 / 史莱姆生态设计）

For art or sprite work, also read:

1. `Assets/AI_DOCS/ART_INTAKE_RULES.md`
2. `Assets/AI_DOCS/ART_NAMING_RULES.md`
3. `Assets/AI_DOCS/ART_INTAKE_LOG.md`

Hard rules:

- Do not run git commands. Git is managed by the user.
- Do not enter Play Mode unless the user explicitly asks.
- Do not save scenes or project settings unless the user explicitly asks.
- Do not modify `Assets/Settings`, `ProjectSettings`, `Packages`, build settings, or package dependencies.
- Do not copy files from `D:\Github\Clone\Claude-Code-Game-Studios`; adapt ideas only.
- Do not enable hooks.
- Keep each task small, scoped, and verified according to `UNITY_MCP_RULES.md`.

Use `.agents/agents` for local role definitions and `.agents/skills` for local workflow skills. These are Codex workflow helpers, not Unity assets.
