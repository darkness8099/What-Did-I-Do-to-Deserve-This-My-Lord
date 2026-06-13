using UnityEngine;

// HP move cost + natural-death routing + Bud/Flower stage ticks for moss/slime.
// Natural decay (HP from move cost / stage decay) NEVER scatters ordinary death resources.
public enum LifecycleOutcome
{
    Alive,
    TransformedToBud,
    StarvationFailed,
}

// Outcome of a Bud / Flower stage tick (TASK-065 / TASK-066).
public enum StageTickOutcome
{
    StillBud,
    Flowered,
    WitherFailed,
    StillFlower,
    Reproduced,
}

public static class MonsterLifecycleSystem
{
    // Apply the per-move HP cost. Fixed by default; random in [min,max] when UseRandomMoveHpCost is set.
    public static void ApplyMoveHpCost(MonsterData monster)
    {
        if (monster == null) return;

        MonsterArchetype a = monster.Archetype;
        int cost = a.HpCostPerMove;
        if (a.UseRandomMoveHpCost)
        {
            int lo = Mathf.Min(a.HpCostPerMoveRandomMin, a.HpCostPerMoveRandomMax);
            int hi = Mathf.Max(a.HpCostPerMoveRandomMin, a.HpCostPerMoveRandomMax);
            cost = Random.Range(lo, hi + 1);
        }
        monster.TakeDamage(Mathf.Max(0, cost));
    }

    // Resolve a Crawling monster that may have died from natural decay (HP <= 0).
    //   Nutrient >= BudRequiredNutrient → transform to Bud (stays, keeps position);
    //   else → StarvationFailed: resources → FloatingResourcePool, removed.
    public static LifecycleOutcome ResolveNaturalDeath(MonsterData monster, MonsterManager monsters)
    {
        if (monster == null || monster.IsAlive()) return LifecycleOutcome.Alive;
        if (monster.Stage != SlimeLifecycleStage.Crawling) return LifecycleOutcome.Alive;

        MonsterArchetype a = monster.Archetype;
        Vector2Int pos = monster.Position;

        if (monster.CurrentNutrient >= a.BudRequiredNutrient)
        {
            monster.SeedCollected(monster.CurrentNutrient);
            monster.TransformTo(SlimeLifecycleStage.Bud, a.BudMaxHP);
            Debug.Log($"[Lifecycle] {monster.DisplayName}@{pos} natural decay → Bud (nutrient={monster.CurrentNutrient}).");
            return LifecycleOutcome.TransformedToBud;
        }

        FloatingResourcePool.Deposit(monster.CurrentNutrient, monster.CurrentMagic, $"starvation-failed:{monster.DisplayName}");
        if (monsters != null) monsters.Remove(monster);
        Debug.Log($"[Lifecycle] {monster.DisplayName}@{pos} StarvationFailed (nutrient<{a.BudRequiredNutrient}); resources → FloatingPool.");
        return LifecycleOutcome.StarvationFailed;
    }

    // ===== TASK-065: Bud stage =====
    public static StageTickOutcome BudTick(MonsterData m, MonsterManager monsters, GridManager grid)
    {
        if (m == null || grid == null) return StageTickOutcome.StillBud;
        MonsterArchetype a = m.Archetype;

        AbsorbFromRadius(m, grid, a.BudAbsorbRadius, a.BudToFlowerNutrient);

        if (m.CollectedNutrient >= a.BudToFlowerNutrient)
        {
            m.TransformTo(SlimeLifecycleStage.Flower, a.FlowerMaxHP);
            Debug.Log($"[Lifecycle] Bud@{m.Position} → Flower (collected={m.CollectedNutrient}).");
            return StageTickOutcome.Flowered;
        }

        m.TakeDamage(a.BudHpDecayPerTick);
        if (!m.IsAlive())
        {
            FloatingResourcePool.Deposit(m.CollectedNutrient + m.CurrentNutrient, m.CurrentMagic, "bud-wither-failed");
            if (monsters != null) monsters.Remove(m);
            Debug.Log($"[Lifecycle] Bud@{m.Position} WitherFailed (collected={m.CollectedNutrient} < {a.BudToFlowerNutrient}).");
            return StageTickOutcome.WitherFailed;
        }
        return StageTickOutcome.StillBud;
    }

    // ===== TASK-066: Flower stage =====
    // On death: reproduce spawnCount new Crawling slimes ALL in the flower's own cell (fix #4), then remove the flower.
    public static StageTickOutcome FlowerTick(MonsterData m, MonsterManager monsters, GridManager grid)
    {
        if (m == null || grid == null) return StageTickOutcome.StillFlower;
        MonsterArchetype a = m.Archetype;

        AbsorbFromRadius(m, grid, a.FlowerAbsorbRadius, a.FlowerMaxAbsorb);

        m.TakeDamage(a.FlowerHpDecayPerTick);
        if (!m.IsAlive())
        {
            int per = Mathf.Max(1, a.NutrientPerSpawn);
            int spawnCount = Mathf.Min(a.FlowerMaxSpawn, m.CollectedNutrient / per);
            Vector2Int pos = m.Position;

            if (monsters != null)
            {
                monsters.Remove(m);
                for (int i = 0; i < spawnCount; i++)
                    monsters.Spawn(pos.x, pos.y, MonsterArchetype.Slime); // all in the same cell
            }
            Debug.Log($"[Lifecycle] Flower@{pos} reproduce: collected={m.CollectedNutrient} → spawned {spawnCount} in-cell.");
            return StageTickOutcome.Reproduced;
        }
        return StageTickOutcome.StillFlower;
    }

    // Pull 1 nutrient from the nearest Soil-with-nutrient cell within chebyshev `radius` (rings outward),
    // adding to CollectedNutrient (capped at `collectedCap`). Returns amount absorbed (0 or 1).
    private static int AbsorbFromRadius(MonsterData m, GridManager grid, int radius, int collectedCap)
    {
        if (m.CollectedNutrient >= collectedCap) return 0;
        Vector2Int pos = m.Position;

        for (int r = 1; r <= radius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                    int cx = pos.x + dx;
                    int cy = pos.y + dy;
                    if (!grid.HasAbsorbableNutrient(cx, cy)) continue;

                    TileAttributeData tile = grid.GetTileAttribute(cx, cy);
                    int took = tile.WithdrawNutrient(1);
                    if (took <= 0) continue;

                    grid.SetTileAttribute(cx, cy, tile);
                    m.AddCollected(took);
                    return took;
                }
            }
        }
        return 0;
    }
}
