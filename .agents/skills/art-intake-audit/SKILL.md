---
name: art-intake-audit
description: Audit this Unity project's art assets and sprite pipeline against ART_INTAKE_RULES and ART_NAMING_RULES. Use for checking Assets/Art naming, directory placement, Unity import setting risks, orphaned art, and prefab migration readiness.
---

# Art Intake Audit

This is primarily read-only unless the user explicitly asks for fixes.

## Required Reads

- `Assets/AI_DOCS/ART_INTAKE_RULES.md`
- `Assets/AI_DOCS/ART_NAMING_RULES.md`
- `Assets/AI_DOCS/ART_INTAKE_LOG.md`
- `Assets/AI_DOCS/GAME_DESIGN_BASE.md`

## Checks

- Files under `Assets/Art/**` use lowercase snake_case where required.
- Tile assets are only tile concepts; DemonLord is a unit asset.
- `_Incoming` is flat and temporary.
- PNG assets have corresponding `.meta`.
- Pixel sprites should use Sprite, Point filter, no compression, PPU 48.
- Imported assets match current code references.
- No PSD/AI/source files are inside Unity project art folders.

## Output

Report:

1. Summary health
2. Naming issues
3. Directory issues
4. Import setting risks
5. Reference/orphan risks
6. Recommended next action

Do not move or rename assets unless explicitly asked.
