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

    // Crawling natural-decay resolution (checked every tick, not only on death):
    //   HP <= BudHpThreshold AND Nutrient >= BudRequiredNutrient → transform to Bud (can trigger while still alive);
    //   HP <= 0 AND Nutrient <  BudRequiredNutrient            → StarvationFailed (resources → FloatingResourcePool, removed);
    //   otherwise (still alive, not yet eligible)               → Alive (keep losing HP).
    public static LifecycleOutcome ResolveNaturalDeath(MonsterData monster, MonsterManager monsters, GridManager grid)
    {
        if (monster == null) return LifecycleOutcome.Alive;
        if (monster.Stage != SlimeLifecycleStage.Crawling) return LifecycleOutcome.Alive;

        MonsterArchetype a = monster.Archetype;
        Vector2Int pos = monster.Position;

        // Low HP + enough nutrient → bud. Only one Bud/Flower per cell: if this cell already has one,
        // don't make a second here (the still-Crawling slime keeps moving and may bud on a free cell later).
        if (monster.CurrentHP <= a.BudHpThreshold && monster.CurrentNutrient >= a.BudRequiredNutrient
            && (monsters == null || !monsters.HasBudOrFlowerAt(pos.x, pos.y)))
        {
            int hp = monster.CurrentHP, nut = monster.CurrentNutrient;
            int areaN, areaCells; SlimeEcologyDiagnostics.Area5x5(grid, pos, out areaN, out areaCells);
            monster.SeedCollected(monster.CurrentNutrient);
            monster.TransformTo(SlimeLifecycleStage.Bud, a.BudMaxHP);
            monster.ResetBudStarve();
            SlimeEcologyDiagnostics.BudSpawn(Time.time, pos, hp, nut, areaN, areaCells);
            return LifecycleOutcome.TransformedToBud;
        }

        // Dead with too little nutrient → starvation failure.
        if (!monster.IsAlive())
        {
            FloatingResourcePool.Deposit(monster.CurrentNutrient, monster.CurrentMagic, $"starvation-failed:{monster.DisplayName}");
            if (monsters != null) monsters.Remove(monster);
            return LifecycleOutcome.StarvationFailed;
        }

        return LifecycleOutcome.Alive;
    }

    // ===== TASK-065: Bud stage =====
    public static StageTickOutcome BudTick(MonsterData m, MonsterManager monsters, GridManager grid)
    {
        if (m == null || grid == null) return StageTickOutcome.StillBud;
        MonsterArchetype a = m.Archetype;
        Vector2Int pos = m.Position;

        int hpBefore = m.CurrentHP;
        int absorbed = AbsorbFromRadius(m, grid, a.BudAbsorbRadius, a.BudToFlowerNutrient);

        if (m.CollectedNutrient >= a.BudToFlowerNutrient)
        {
            int areaN, ac; SlimeEcologyDiagnostics.Area5x5(grid, pos, out areaN, out ac);
            m.TransformTo(SlimeLifecycleStage.Flower, a.FlowerMaxHP);
            SlimeEcologyDiagnostics.BudResult(Time.time, pos, "Flower", m.CurrentHP, m.CollectedNutrient, areaN);
            return StageTickOutcome.Flowered;
        }

        // Absorbed this tick → grow only, NO HP loss; reset starvation counter.
        if (absorbed > 0)
        {
            m.ResetBudStarve();
            SlimeEcologyDiagnostics.BudTick(Time.time, pos, m.CurrentHP, m.CollectedNutrient, absorbed, 0, "absorbed");
            return StageTickOutcome.StillBud;
        }

        // No food this tick → count toward starvation; only lose HP after BudStarvationCooldownTicks misses.
        m.RegisterBudStarve();
        int hpDelta = 0;
        if (m.BudStarveCounter >= Mathf.Max(1, a.BudStarvationCooldownTicks))
        {
            m.TakeDamage(a.BudHpDecayPerTick);
            hpDelta = m.CurrentHP - hpBefore;
            m.ResetBudStarve();

            if (!m.IsAlive())
            {
                int areaN2, ac2; SlimeEcologyDiagnostics.Area5x5(grid, pos, out areaN2, out ac2);
                FloatingResourcePool.Deposit(m.CollectedNutrient + m.CurrentNutrient, m.CurrentMagic, "bud-wither-failed");
                if (monsters != null) monsters.Remove(m);
                SlimeEcologyDiagnostics.BudResult(Time.time, pos, "Dead", m.CurrentHP, m.CollectedNutrient, areaN2);
                return StageTickOutcome.WitherFailed;
            }
        }

        SlimeEcologyDiagnostics.BudTick(Time.time, pos, m.CurrentHP, m.CollectedNutrient, 0, hpDelta, "starved");
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
            int collected = m.CollectedNutrient;
            int per = Mathf.Max(1, a.NutrientPerSpawn);
            int spawnCount = Mathf.Min(a.FlowerMaxSpawn, collected / per);
            Vector2Int pos = m.Position;

            int actual = 0;
            var delays = new System.Collections.Generic.List<string>();
            if (monsters != null)
            {
                monsters.Remove(m); // free the flower's own cell first

                // All offspring spawn in the flower's own cell (monsters have no collision volume).
                // Each newborn still gets an independent random startup delay (idles before moving).
                for (int i = 0; i < spawnCount; i++)
                {
                    MonsterData ns = monsters.Spawn(pos.x, pos.y, MonsterArchetype.Slime);
                    if (ns == null) continue;
                    float d = Random.Range(0f, a.SpawnMoveDelayMaxSeconds);
                    ns.SetSpawnDelay(d);
                    delays.Add(d.ToString("F2"));
                    actual++;
                }
            }
            string fail = (actual >= spawnCount) ? "none" : "occupied";
            SlimeEcologyDiagnostics.FlowerResult(Time.time, pos, collected, spawnCount, actual, fail, string.Join(";", delays));
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
