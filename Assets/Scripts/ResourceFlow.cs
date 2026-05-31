using System.Collections.Generic;
using UnityEngine;

public enum DeathCause
{
    HeroKill,
    PredatorEat,
    NaturalDecay,
    Starvation,
    LifecycleTransform,
    LifecycleWither,
    EnvironmentDeath,
    Unknown,
}

// Centralized resource flow helpers. Public entry points describe intent first;
// the shared algorithm only moves resources to nearby Soil or the floating pool.
public static class ResourceFlow
{
    public static void TransferResourcesToPredator(MonsterData prey, MonsterData predator, string reason)
    {
        if (prey == null || predator == null)
        {
            Debug.LogWarning($"[Resource] Predator transfer skipped. prey={prey != null}, predator={predator != null}, reason={reason}");
            return;
        }

        int preyNutrient = prey.CurrentNutrient;
        int preyMagic = prey.CurrentMagic;
        int overflowNutrient = predator.ReceiveNutrient(prey.WithdrawNutrient(preyNutrient));
        int overflowMagic = predator.ReceiveMagic(prey.WithdrawMagic(preyMagic));
        int absorbedNutrient = preyNutrient - overflowNutrient;
        int absorbedMagic = preyMagic - overflowMagic;

        Debug.Log($"[Resource] PredatorEat {prey.DisplayName}->{predator.DisplayName}: absorbed N={absorbedNutrient} M={absorbedMagic}; overflow N={overflowNutrient} M={overflowMagic}; reason={reason}");

        if (overflowNutrient > 0 || overflowMagic > 0)
            FloatingResourcePool.Deposit(overflowNutrient, overflowMagic, $"predator-overflow:{reason}");
    }

    public static void ScatterDigLeftoverResources(Vector2Int origin, int nutrient, int magic, GridManager gridManager, string reason)
    {
        ScatterToNearbySoil(origin, nutrient, magic, gridManager, $"dig-leftover:{reason}");
    }

    public static void ScatterOrdinaryDeathResources(Vector2Int origin, MonsterData monster, GridManager gridManager, DeathCause cause, string reason)
    {
        if (monster == null) return;

        if (!AllowsOrdinaryDeathScatter(cause))
        {
            Debug.Log($"[Resource] Death scatter skipped for {monster.DisplayName} at {origin}; cause={cause}, reason={reason}");
            return;
        }

        int nutrient = monster.CurrentNutrient;
        int magic = monster.CurrentMagic;
        if (nutrient > 0 || magic > 0)
            Debug.Log($"[Resource] Death@{origin}: {monster.DisplayName} drops N={nutrient} M={magic} cause={cause}");

        ScatterToNearbySoil(origin, nutrient, magic, gridManager, $"ordinary-death:{cause}:{reason}");
    }

    public static bool AllowsOrdinaryDeathScatter(DeathCause cause)
    {
        return cause == DeathCause.HeroKill || cause == DeathCause.EnvironmentDeath;
    }

    [System.Obsolete("Use ScatterDigLeftoverResources or ScatterOrdinaryDeathResources so the resource-flow intent is explicit.")]
    public static void Scatter(Vector2Int origin, int nutrient, int magic, GridManager gridManager, string reason)
    {
        ScatterToNearbySoil(origin, nutrient, magic, gridManager, reason);
    }

    private static void ScatterToNearbySoil(Vector2Int origin, int nutrient, int magic, GridManager gridManager, string reason)
    {
        if (nutrient <= 0 && magic <= 0) return;

        if (gridManager == null)
        {
            FloatingResourcePool.Deposit(nutrient, magic, $"{reason}(no-grid)");
            return;
        }

        for (int r = 1; r <= 3; r++)
        {
            List<Vector2Int> soilCells = CollectSoilCellsAtRadius(origin, r, gridManager);
            if (soilCells.Count > 0)
            {
                Distribute(soilCells, nutrient, magic, gridManager);
                Debug.Log($"[Resource] Scatter origin={origin} r={r} → {soilCells.Count} Soil cells; N={nutrient} M={magic} (reason: {reason})");
                return;
            }
        }

        FloatingResourcePool.Deposit(nutrient, magic, $"{reason}(no-soil-within-r3)");
    }

    private static List<Vector2Int> CollectSoilCellsAtRadius(Vector2Int origin, int radius, GridManager gm)
    {
        var result = new List<Vector2Int>();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                // Chebyshev distance equals radius → cells on the ring of this radius
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;

                int x = origin.x + dx;
                int y = origin.y + dy;
                if (!gm.IsInside(x, y)) continue;
                if (gm.GetCellType(x, y) != CellType.Soil) continue;
                result.Add(new Vector2Int(x, y));
            }
        }
        return result;
    }

    private static void Distribute(List<Vector2Int> cells, int nutrient, int magic, GridManager gm)
    {
        int n = cells.Count;
        int nutrientShare = nutrient / n;
        int nutrientRest  = nutrient - nutrientShare * n;
        int magicShare    = magic / n;
        int magicRest     = magic - magicShare * n;

        for (int i = 0; i < n; i++)
        {
            int giveN = nutrientShare + (i < nutrientRest ? 1 : 0);
            int giveM = magicShare    + (i < magicRest    ? 1 : 0);
            if (giveN <= 0 && giveM <= 0) continue;

            var attr = gm.GetTileAttribute(cells[i].x, cells[i].y);
            attr.DepositNutrient(giveN);
            attr.DepositMagic(giveM);
            gm.SetTileAttribute(cells[i].x, cells[i].y, attr);
        }
    }
}
