---
name: unity-task-intake
description: Start a task safely in this Unity project by reading AI_DOCS, identifying current scope, forbidden operations, relevant systems, and required validation. Use before implementing Unity changes, planning a task, taking over context, or deciding whether to ask the user for clarification.
---

# Unity Task Intake

Use this before acting on a Unity project task.

## Required Reads

Read in this order:

1. `AGENTS.md`
2. `Assets/AI_DOCS/TASKS.md`
3. `Assets/AI_DOCS/UNITY_MCP_RULES.md`
4. `Assets/AI_DOCS/GAME_DESIGN_BASE.md`
5. `Assets/AI_DOCS/AI_WORKFLOW_LOG.md` if recent history matters
6. `Assets/AI_DOCS/AI_UNITY_WORKFLOW_TEMPLATE.md` for workflow shape

For art, sprite, prefab, or import work also read:

- `Assets/AI_DOCS/ART_INTAKE_RULES.md`
- `Assets/AI_DOCS/ART_NAMING_RULES.md`
- `Assets/AI_DOCS/ART_INTAKE_LOG.md`

## Intake Workflow

1. Restate the task in one sentence.
2. Identify the relevant current task or phase from `TASKS.md`.
3. List explicit do-not-do constraints.
4. Identify likely files/systems involved.
5. Decide whether this is read-only, plan-only, implementation, or validation.
6. Define A/B/C validation from `UNITY_MCP_RULES.md`.
7. Name any ambiguity that needs user confirmation before broad changes.

## Hard Boundaries

- Do not run git.
- Do not enter Play Mode unless explicitly requested.
- Do not save Scene or Project unless explicitly requested.
- Do not modify `ProjectSettings`, `Packages`, build settings, or `Assets/Settings`.
- Do not enable hooks.
- Do not copy CCGS files into this project.
- Keep changes outside `Assets` when creating Codex workflow helpers.

## Output

For plan-only tasks, output:

1. Current state
2. Proposed scope
3. Files likely involved
4. Validation plan
5. Stop/confirm points

For implementation tasks, provide a short working update before edits, then follow the project completion report format.
