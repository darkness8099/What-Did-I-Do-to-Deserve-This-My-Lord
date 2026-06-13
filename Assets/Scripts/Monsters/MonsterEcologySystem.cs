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

        // --- High (>= release threshold): release surplus to an adjacent Soil that ALREADY has nutrient ---
        // Keep KeepNutrientOnRelease for self. Never release onto a 0-nutrient tile; no averaging / no "find poorest".
        if (n >= a.ReleaseWhenNutrientGreaterOrEqual)
        {
            int surplus = n - a.KeepNutrientOnRelease;
            if (surplus <= 0) return EcologyAction.Stable;

            // Candidate neighbors that ALREADY hold nutrient (never release onto a 0-nutrient tile).
            var candidates = new System.Collections.Generic.List<Vector2Int>(4);
            foreach (Vector2Int dir in N4)
            {
                int cx = pos.x + dir.x;
                int cy = pos.y + dir.y;
                if (grid.HasAbsorbableNutrient(cx, cy)) candidates.Add(new Vector2Int(cx, cy));
            }
            if (candidates.Count == 0) return EcologyAction.NoReleaseTarget;

            // Release each surplus point to an INDEPENDENTLY random candidate (repeats allowed; not all fixed to one).
            for (int i = 0; i < surplus; i++)
            {
                Vector2Int c = candidates[Random.Range(0, candidates.Count)];
                TileAttributeData tile = grid.GetTileAttribute(c.x, c.y);
                tile.DepositNutrient(1);
                grid.SetTileAttribute(c.x, c.y, tile);
            }
            monster.WithdrawNutrient(surplus);
            return EcologyAction.Released;
        }

        return EcologyAction.Stable;
    }
}
