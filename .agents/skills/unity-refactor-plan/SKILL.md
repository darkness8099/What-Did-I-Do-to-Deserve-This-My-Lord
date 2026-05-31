---
name: unity-refactor-plan
description: Create a staged Unity refactor plan for this project without changing code. Use when planning GridManager decomposition, DemonLord system extraction, prefab migration, manager/renderer separation, or any multi-step gameplay architecture cleanup.
---

# Unity Refactor Plan

Produce a plan only. Do not edit code unless the user separately asks.

## Required Reads

- `AGENTS.md`
- `Assets/AI_DOCS/TASKS.md`
- `Assets/AI_DOCS/UNITY_MCP_RULES.md`
- `Assets/AI_DOCS/GAME_DESIGN_BASE.md`
- Relevant scripts in `Assets/Scripts`

## Method

1. Identify the current source of truth and current implemented behavior.
2. List coupling points with `rg`.
3. Separate behavior into stable concepts and temporary test scaffolding.
4. Propose small tasks that can compile independently.
5. For each task, list files likely touched, validation type, and human Play Mode checks.

## Refactor Rules

- One system per task.
- Prefer extraction over broad rewrites.
- Preserve current playable behavior until a replacement is verified.
- Do not save scenes automatically.
- Do not introduce new folders under `Assets/Scripts` until the user approves folder organization.
- Do not create prefabs as part of a refactor plan unless the task is explicitly prefab migration.

## GridManager Target Shape

Recommended direction:

- `GridManager`: grid state and tile mutation.
- `LevelRuntimeConfig` or equivalent: entrance, test DemonLord position, test tile attributes.
- `DemonLordManager`: DemonLord unit state and placement.
- `DemonLordRenderer`: DemonLord visual and captive visual.
- `InputHandler`: input routing only.

## Output

Use phases:

1. Current coupling map
2. Risks
3. Proposed task sequence
4. First task recommendation
5. Stop point for user discussion
