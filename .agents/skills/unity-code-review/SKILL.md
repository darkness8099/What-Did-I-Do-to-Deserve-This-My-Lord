---
name: unity-code-review
description: Review Unity C# scripts in this project for architecture, responsibility boundaries, gameplay correctness, AI_DOCS compliance, and refactor risk. Use when asked to review code, inspect GridManager coupling, assess a changeset, or identify Unity gameplay technical debt without necessarily modifying files.
---

# Unity Code Review

Use a code-review stance: findings first, ordered by severity, with file references.

## Required Reads

Read before reviewing:

- `AGENTS.md`
- `Assets/AI_DOCS/UNITY_MCP_RULES.md`
- `Assets/AI_DOCS/GAME_DESIGN_BASE.md`
- Relevant scripts under `Assets/Scripts`

For art/prefab related code, also read `ART_INTAKE_RULES.md` and `ART_NAMING_RULES.md`.

## Review Checklist

Check:

- Does the code match `GAME_DESIGN_BASE.md`?
- Does each class have one clear reason to change?
- Are temporary MVP/test rules mixed into long-lived managers?
- Does a renderer own only visuals?
- Does a manager expose too much internal state?
- Are Unity calls appropriate for the current MVP stage?
- Are coroutine/frame-dependent behaviors marked for human validation?
- Are public methods sufficient for A/B class tests?
- Is any forbidden operation implied, such as scene save, git, ProjectSettings, package changes, or Play Mode?

## GridManager Special Check

For `GridManager`, classify responsibilities into:

- Keep: grid dimensions, `GridData`, cell mutation/query, tile attributes.
- Move soon: test map setup, test Slime attributes, DemonLord test position.
- Move later: entrance placement if level config becomes real.
- Never add: monster data, hero data, combat, rendering, prefab creation.

Treat over-centralization as architecture debt even if the file is short.

## Output

Use:

1. Findings
2. Open questions
3. Safe next step

Do not modify files unless the user explicitly asks for implementation.
