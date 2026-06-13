using UnityEngine;

// TASK-063 — Post-move ecology check for moss/slime: absorb / release nutrient with the 4 cardinal Soil neighbors.
// Runs once per ecology tick / after a move completes (hook off MonsterManager.MonsterMoved). NOT per-frame.
// Each call visits at most the 4 cardinal neighbors. All thresholds come from MonsterArchetype (no magic numbers).
//
// Ladder (v1 Moss, cap 3): Nutrient<=AbsorbWhenNutrientLessOrEqual → absorb 1 (+heal);
//   ==BudRequiredNutrient (2) → stable, no action; >=ReleaseWhenNutrientGreaterOrEqual (3) → release surplus,
//   never dropping below KeepNutrientOnRelease. HP cost-per-move and lifecycle are handled in TASK-064.
public enum EcologyAction
{
    None,
    Absorbed,
    Stable,
    Released,
    NoAbsorbTarget,
    NoReleaseTarget,
}

public static class MonsterEcologySystem
{
    private static readonly Vector2Int[] N4 =
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
    };

    // Core: one ecology check for `monster` at its current cell. Reads/writes only Soil neighbor tiles via GridManager.
    public static EcologyAction ResolveAfterMove(MonsterData monster, GridManager grid)
    {
        if (monster == null || grid == null) return EcologyAction.None;

        Vector2Int pos = monster.Position;
        MonsterArchetype a = monster.Archetype;
        int n = monster.CurrentNutrient;

        // --- Low: try to absorb 1 from a neighbor Soil that has nutrient, then heal ---
        if (n <= a.AbsorbWhenNutrientLessOrEqual)
        {
            foreach (Vector2Int dir in N4)
            {
                int cx = pos.x + dir.x;
                int cy = pos.y + dir.y;
                if (!grid.HasAbsorbableNutrient(cx, cy)) continue;

                TileAttributeData tile = grid.GetTileAttribute(cx, cy);
                int took = tile.WithdrawNutrient(1);
                if (took <= 0) continue;

                grid.SetTileAttribute(cx, cy, tile);
                monster.ReceiveNutrient(took);
                monster.Heal(a.HpHealPerAbsorb);
                return EcologyAction.Absorbed;
            }
            return EcologyAction.NoAbsorbTarget; // 4 neighbors have no absorbable nutrient → no action
        }

        // --- Stable reserve band (between absorb and release thresholds): do nothing ---
        if (n < a.ReleaseWhenNutrientGreaterOrEqual)
            return EcologyAction.Stable;

        // --- High: release only the surplus above the breeding reserve; never drop below KeepNutrientOnRelease ---
        int surplus = n - a.KeepNutrientOnRelease;
        if (surplus <= 0) return EcologyAction.Stable;

        foreach (Vector2Int dir in N4)
        {
            int cx = pos.x + dir.x;
            int cy = pos.y + dir.y;
            if (!grid.IsInside(cx, cy)) continue;
            if (grid.GetCellType(cx, cy) != CellType.Soil) continue; // Empty is not a resource container

            TileAttributeData tile = grid.GetTileAttribute(cx, cy);
            tile.DepositNutrient(surplus);
            grid.SetTileAttribute(cx, cy, tile);
            monster.WithdrawNutrient(surplus);
            return EcologyAction.Released;
        }

        return EcologyAction.NoReleaseTarget; // nowhere to release → hold (Nutrient unchanged)
    }
}
