---
name: unity-qa-checklist
description: Build a Unity task QA checklist using this project's A/B/C testing policy. Use after implementation, before handoff, or when planning validation for gameplay, rendering, prefab, UI, and refactor tasks.
---

# Unity QA Checklist

Use the testing categories in `UNITY_MCP_RULES.md`.

## Required Reads

- `Assets/AI_DOCS/UNITY_MCP_RULES.md`
- `Assets/AI_DOCS/TASKS.md`
- Relevant task notes or changed files

## Categories

A class: AI should run.

- Script refresh/compile.
- Console error check.
- Direct public method verification with `execute_code` when possible.
- Object/component existence checks if no Play Mode frame progression is needed.

B class: AI may run briefly.

- Focused scene object or serialized field inspection.
- Single-call sanity checks.

C class: Human validates.

- Play Mode visual flow.
- Coroutine timing.
- Hero movement feel.
- UI layout and readability.
- Camera screenshots or long-running gameplay.

## Output

Produce:

1. A tests
2. B tests
3. C manual checks
4. Regression risks
5. Stop condition if errors appear

Never enter Play Mode unless the user explicitly asks.
