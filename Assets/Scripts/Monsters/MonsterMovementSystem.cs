using UnityEngine;

// Rule-based "low intelligence" movement for moss/slime. No pathfinding, no per-frame; one step per ecology tick.
// Movement rules (fix #5):
//   - go straight along current facing if open;
//   - if blocked: turn — if BOTH perpendiculars are open, pick one at random; if only one, take it;
//   - if both perpendiculars are blocked (dead end), reverse;
//   - if fully boxed, stay (keep facing).
// Monsters have NO collision volume (fix #1): other monsters are NOT obstacles — only terrain blocks them.
public static class MonsterMovementSystem
{
    // Pure decision (unit-testable). canEnter = terrain-only traversability (underground Empty).
    public static bool ComputeNextStep(Vector2Int pos, Vector2Int dir, System.Func<Vector2Int, bool> canEnter,
                                       out Vector2Int newPos, out Vector2Int newDir)
    {
        newDir = dir;
        newPos = pos;
        if (canEnter == null) return false;

        // 1) straight ahead
        if (canEnter(pos + dir)) { newPos = pos + dir; newDir = dir; return true; }

        // 2) blocked → turn toward an open perpendicular (random if both open)
        Vector2Int left  = new Vector2Int(-dir.y, dir.x);
        Vector2Int right = new Vector2Int(dir.y, -dir.x);
        bool lOpen = canEnter(pos + left);
        bool rOpen = canEnter(pos + right);
        if (lOpen && rOpen)
        {
            Vector2Int pick = Random.value < 0.5f ? left : right;
            newDir = pick; newPos = pos + pick; return true;
        }
        if (lOpen) { newDir = left;  newPos = pos + left;  return true; }
        if (rOpen) { newDir = right; newPos = pos + right; return true; }

        // 3) dead end → reverse
        Vector2Int back = new Vector2Int(-dir.x, -dir.y);
        if (canEnter(pos + back)) { newDir = back; newPos = pos + back; return true; }

        // 4) fully boxed → stay
        return false;
    }

    // Apply one movement step for `monster`. Terrain-only traversability (passes through other monsters).
    // Updates facing always; on a successful move updates the monster's Position. Returns whether it changed cells.
public static bool TryMoveStep(MonsterData monster, GridManager grid, MonsterManager manager, out Vector2Int newPos)
    {
        newPos = monster != null ? monster.Position : Vector2Int.zero;
        if (monster == null || grid == null || manager == null) return false;

        System.Func<Vector2Int, bool> canEnter = c => grid.IsMonsterTraversable(c.x, c.y);

        Vector2Int to, nd;
        bool moved = ComputeNextStep(monster.Position, monster.MoveDirection, canEnter, out to, out nd);
        monster.SetMoveDirection(nd);
        if (!moved) return false;
        if (!manager.Move(monster, to)) return false;

        newPos = to;
        return true;
    }
}
