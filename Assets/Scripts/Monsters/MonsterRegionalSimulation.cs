using System;
using System.Collections.Generic;
using UnityEngine;

public struct MonsterRegionKey : IEquatable<MonsterRegionKey>
{
    public int X;
    public int Y;

    public MonsterRegionKey(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static MonsterRegionKey FromCell(Vector2Int cell, int regionSize)
    {
        int size = Mathf.Max(1, regionSize);
        return new MonsterRegionKey(FloorDiv(cell.x, size), FloorDiv(cell.y, size));
    }

    public RectInt GetBounds(int regionSize)
    {
        int size = Mathf.Max(1, regionSize);
        return new RectInt(X * size, Y * size, size, size);
    }

    public bool Equals(MonsterRegionKey other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is MonsterRegionKey && Equals((MonsterRegionKey)obj);
    public override int GetHashCode() => unchecked((X * 397) ^ Y);
    public override string ToString() => $"({X},{Y})";

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0))) result--;
        return result;
    }
}

public sealed class MonsterRegionState
{
    public MonsterRegionKey Key { get; private set; }
    public int Count { get; private set; }
    public int TotalHP { get; private set; }
    public int TotalNutrient { get; private set; }
    public int TotalMagic { get; private set; }
    public int LastAdvancedTick { get; set; }
    public int Seed { get; private set; }

    private readonly int[] directionCounts = new int[4];

    public MonsterRegionState(MonsterRegionKey key, int seed)
    {
        Key = key;
        Seed = seed == 0 ? unchecked(key.GetHashCode() * 1103515245 + 12345) : seed;
    }

    public void Absorb(MonsterData monster)
    {
        if (monster == null) return;
        Count++;
        TotalHP += Mathf.Max(1, monster.CurrentHP);
        TotalNutrient += Mathf.Max(0, monster.CurrentNutrient);
        TotalMagic += Mathf.Max(0, monster.CurrentMagic);
        directionCounts[DirectionIndex(monster.MoveDirection)]++;
    }

    public void Merge(MonsterRegionState other)
    {
        if (other == null || other.Count <= 0) return;
        Count += other.Count;
        TotalHP += other.TotalHP;
        TotalNutrient += other.TotalNutrient;
        TotalMagic += other.TotalMagic;
        for (int i = 0; i < 4; i++) directionCounts[i] += other.directionCounts[i];
        Seed ^= other.Seed;
    }

    public MonsterRegionState ExtractPopulation(int requested)
    {
        int take = Mathf.Clamp(requested, 0, Count);
        if (take <= 0) return null;

        int originalCount = Count;
        var extracted = new MonsterRegionState(Key, NextSeed());
        extracted.Count = take;
        extracted.TotalHP = Share(TotalHP, take, originalCount);
        extracted.TotalNutrient = Share(TotalNutrient, take, originalCount);
        extracted.TotalMagic = Share(TotalMagic, take, originalCount);

        TotalHP -= extracted.TotalHP;
        TotalNutrient -= extracted.TotalNutrient;
        TotalMagic -= extracted.TotalMagic;

        int assignedDirections = 0;
        for (int i = 0; i < 4; i++)
        {
            int share = Share(directionCounts[i], take, originalCount);
            extracted.directionCounts[i] = share;
            directionCounts[i] -= share;
            assignedDirections += share;
        }
        while (assignedDirections < take)
        {
            int index = LargestDirectionIndex();
            if (directionCounts[index] > 0) directionCounts[index]--;
            extracted.directionCounts[index]++;
            assignedDirections++;
        }

        Count -= take;
        return extracted;
    }

    public void ApplyApproximateHpDelta(int delta)
    {
        TotalHP = Mathf.Max(0, TotalHP + delta);
    }

    public void ConsumeForBud(int nutrientCost)
    {
        if (Count <= 0) return;
        int hpShare = Mathf.Max(1, TotalHP / Count);
        TotalHP = Mathf.Max(0, TotalHP - hpShare);
        TotalNutrient = Mathf.Max(0, TotalNutrient - Mathf.Max(0, nutrientCost));
        int direction = LargestDirectionIndex();
        if (directionCounts[direction] > 0) directionCounts[direction]--;
        Count--;
    }

    public int NextSeed()
    {
        Seed = unchecked(Seed * 1664525 + 1013904223);
        return Seed;
    }

    public Vector2Int TakeDirection()
    {
        int total = 0;
        for (int i = 0; i < 4; i++) total += directionCounts[i];
        if (total <= 0) return Vector2Int.right;

        int pick = PositiveMod(NextSeed(), total);
        for (int i = 0; i < 4; i++)
        {
            if (pick < directionCounts[i])
            {
                directionCounts[i]--;
                return DirectionFromIndex(i);
            }
            pick -= directionCounts[i];
        }
        return Vector2Int.right;
    }

    public void ConsumeMaterialized(int hp, int nutrient, int magic)
    {
        if (Count <= 0) return;
        Count--;
        TotalHP = Mathf.Max(0, TotalHP - hp);
        TotalNutrient = Mathf.Max(0, TotalNutrient - nutrient);
        TotalMagic = Mathf.Max(0, TotalMagic - magic);
    }

    private int LargestDirectionIndex()
    {
        int result = 0;
        for (int i = 1; i < 4; i++)
            if (directionCounts[i] > directionCounts[result]) result = i;
        return result;
    }

    private static int Share(int total, int take, int count)
    {
        if (total <= 0 || take <= 0 || count <= 0) return 0;
        return (int)((long)total * take / count);
    }

    private static int DirectionIndex(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return 0;
        if (direction == Vector2Int.down) return 1;
        if (direction == Vector2Int.left) return 2;
        return 3;
    }

    private static Vector2Int DirectionFromIndex(int index)
    {
        if (index == 0) return Vector2Int.up;
        if (index == 1) return Vector2Int.down;
        if (index == 2) return Vector2Int.left;
        return Vector2Int.right;
    }

    private static int PositiveMod(int value, int divisor)
    {
        if (divisor <= 0) return 0;
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }


public MonsterRegionState RemoveStarvedPopulation(int requested)
    {
        int remove = Mathf.Clamp(requested, 0, Count);
        if (remove <= 0) return null;

        int originalCount = Count;
        var removed = new MonsterRegionState(Key, NextSeed());
        removed.Count = remove;
        removed.TotalHP = 0;
        removed.TotalNutrient = Share(TotalNutrient, remove, originalCount);
        removed.TotalMagic = Share(TotalMagic, remove, originalCount);
        TotalNutrient -= removed.TotalNutrient;
        TotalMagic -= removed.TotalMagic;

        int assigned = 0;
        for (int i = 0; i < 4; i++)
        {
            int share = Share(directionCounts[i], remove, originalCount);
            removed.directionCounts[i] = share;
            directionCounts[i] -= share;
            assigned += share;
        }
        while (assigned < remove)
        {
            int index = LargestDirectionIndex();
            if (directionCounts[index] > 0) directionCounts[index]--;
            removed.directionCounts[index]++;
            assigned++;
        }

        Count -= remove;
        TotalHP = Mathf.Min(TotalHP, Count * MonsterArchetype.Slime.BaseMaxHP);
        return removed;
    }
}

public sealed class MonsterRegionalSimulation
{
    private static readonly Vector2Int[] NeighborRegions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
    };

    private readonly Dictionary<MonsterRegionKey, MonsterRegionState> regions =
        new Dictionary<MonsterRegionKey, MonsterRegionState>();
    private readonly List<MonsterRegionKey> keyScratch = new List<MonsterRegionKey>();
    private readonly List<Vector2Int> cellScratch = new List<Vector2Int>();
    private readonly List<Vector2Int> nutrientSoilScratch = new List<Vector2Int>();
    private readonly List<MonsterRegionKey> neighborScratch = new List<MonsterRegionKey>();
    private readonly List<MonsterData> captureScratch = new List<MonsterData>();

    public int RegionSize { get; private set; }
    public int RegionCount => regions.Count;

    public int AggregateMonsterCount
    {
        get
        {
            int total = 0;
            foreach (KeyValuePair<MonsterRegionKey, MonsterRegionState> pair in regions)
                total += pair.Value.Count;
            return total;
        }
    }

    public int TotalAggregateNutrient
    {
        get
        {
            int total = 0;
            foreach (KeyValuePair<MonsterRegionKey, MonsterRegionState> pair in regions)
                total += pair.Value.TotalNutrient;
            return total;
        }
    }

    public MonsterRegionalSimulation(int regionSize)
    {
        RegionSize = Mathf.Max(1, regionSize);
    }

public int CaptureFarCrawling(
        IList<MonsterData> snapshot,
        MonsterManager manager,
        MonsterSimulationInterest interest,
        int simulationTick)
    {
        if (snapshot == null || manager == null) return 0;
        captureScratch.Clear();

        for (int i = 0; i < snapshot.Count; i++)
        {
            MonsterData monster = snapshot[i];
            if (monster == null || monster.Stage != SlimeLifecycleStage.Crawling || monster.IsSpawnDelayed())
                continue;
            if (MonsterSimulationPolicy.Classify(monster.Position, interest) != MonsterSimulationTier.Aggregate)
                continue;

            MonsterRegionKey key = MonsterRegionKey.FromCell(monster.Position, RegionSize);
            MonsterRegionState state = GetOrCreate(key);
            state.Absorb(monster);
            state.LastAdvancedTick = simulationTick;
            captureScratch.Add(monster);
        }

        return manager.RemoveMany(captureScratch);
    }

    public int PromoteInterestedRegions(
        MonsterManager manager,
        GridManager grid,
        MonsterSimulationInterest interest)
    {
        if (manager == null || grid == null || regions.Count == 0) return 0;
        CopyKeys();
        int promoted = 0;

        for (int i = 0; i < keyScratch.Count; i++)
        {
            MonsterRegionKey key = keyScratch[i];
            MonsterRegionState state;
            if (!regions.TryGetValue(key, out state)) continue;
            if (MonsterSimulationPolicy.ClassifyRegion(key.GetBounds(RegionSize), interest) == MonsterSimulationTier.Aggregate)
                continue;
            int countBefore = state.Count;
            if (Materialize(key, state, manager, grid))
            {
                promoted += countBefore;
                regions.Remove(key);
            }
        }
        return promoted;
    }

    public int EnsureMaterializedAt(Vector2Int cell, MonsterManager manager, GridManager grid)
    {
        MonsterRegionKey key = MonsterRegionKey.FromCell(cell, RegionSize);
        MonsterRegionState state;
        if (!regions.TryGetValue(key, out state)) return 0;
        int count = state.Count;
        if (!Materialize(key, state, manager, grid)) return 0;
        regions.Remove(key);
        return count;
    }

    public void Advance(int elapsedTicks, int simulationTick, MonsterManager manager, GridManager grid)
    {
        if (elapsedTicks <= 0 || grid == null || regions.Count == 0) return;
        CopyKeys();

        for (int i = 0; i < keyScratch.Count; i++)
        {
            MonsterRegionKey key = keyScratch[i];
            MonsterRegionState state;
            if (!regions.TryGetValue(key, out state) || state.Count <= 0) continue;

            RectInt bounds = key.GetBounds(RegionSize);
            CollectNutrientSoils(bounds, grid);

            int hpCostsPerMonster = elapsedTicks / Mathf.Max(1, MonsterArchetype.Slime.HpCostCooldownMoves);
            int ecologyActions = elapsedTicks / Mathf.Max(1, MonsterArchetype.Slime.EcologyActionCooldownMoves);
            int approximateHeals = nutrientSoilScratch.Count > 0
                ? (ecologyActions + 1) / 2 * MonsterArchetype.Slime.HpHealPerAbsorb
                : 0;
            int netDamagePerMonster = Mathf.Max(0,
                hpCostsPerMonster * MonsterArchetype.Slime.HpCostPerMove - approximateHeals);
            state.ApplyApproximateHpDelta(-netDamagePerMonster * state.Count);

            MixRegionalNutrients(state, grid);

            int averageHp = state.Count > 0 ? state.TotalHP / state.Count : 0;
            if (averageHp <= MonsterArchetype.Slime.BudHpThreshold &&
                state.TotalNutrient >= MonsterArchetype.Slime.BudRequiredNutrient)
            {
                int eligible = state.TotalNutrient / Mathf.Max(1, MonsterArchetype.Slime.BudRequiredNutrient);
                int budCount = Mathf.Min(eligible, Mathf.Max(1, state.Count / 4));
                SpawnApproximateBuds(state, budCount, manager, grid);
            }

            if (state.Count <= 0)
            {
                regions.Remove(key);
                continue;
            }

            if (state.TotalHP > 0 && state.TotalHP < state.Count)
            {
                int starvedCount = state.Count - state.TotalHP;
                MonsterRegionState starved = state.RemoveStarvedPopulation(starvedCount);
                if (starved != null)
                    FloatingResourcePool.Deposit(
                        starved.TotalNutrient,
                        starved.TotalMagic,
                        $"aggregate-partial-starvation:{key}");
            }

            if (state.TotalHP <= 0)
            {
                FloatingResourcePool.Deposit(
                    state.TotalNutrient,
                    state.TotalMagic,
                    $"aggregate-starvation:{key}");
                regions.Remove(key);
                continue;
            }

            state.LastAdvancedTick = simulationTick;
            TryFlowToNeighbor(state, grid);
            if (state.Count <= 0) regions.Remove(key);
        }
    }

    public bool TryGetState(MonsterRegionKey key, out MonsterRegionState state)
    {
        return regions.TryGetValue(key, out state);
    }

    private MonsterRegionState GetOrCreate(MonsterRegionKey key)
    {
        MonsterRegionState state;
        if (!regions.TryGetValue(key, out state))
        {
            int seed = unchecked(key.GetHashCode() * 486187739 + 31);
            state = new MonsterRegionState(key, seed);
            regions[key] = state;
        }
        return state;
    }

    private bool Materialize(
        MonsterRegionKey key,
        MonsterRegionState state,
        MonsterManager manager,
        GridManager grid)
    {
        CollectTraversableCells(key.GetBounds(RegionSize), grid);
        if (cellScratch.Count == 0 || state.Count <= 0) return false;

        int count = state.Count;
        int hpRemaining = state.TotalHP;
        int nutrientRemaining = state.TotalNutrient;
        int magicRemaining = state.TotalMagic;

        for (int i = 0; i < count; i++)
        {
            int remaining = count - i;
            int hp = Mathf.Clamp(hpRemaining / remaining, 1, MonsterArchetype.Slime.BaseMaxHP);
            int nutrient = Mathf.Clamp(nutrientRemaining / remaining, 0, MonsterArchetype.Slime.NutrientCapacity);
            int magic = Mathf.Clamp(magicRemaining / remaining, 0, MonsterArchetype.Slime.MagicCapacity);
            int cellIndex = PositiveMod(state.NextSeed(), cellScratch.Count);
            Vector2Int cell = cellScratch[cellIndex];
            Vector2Int direction = state.TakeDirection();

            MonsterData monster = manager.Spawn(cell.x, cell.y, MonsterArchetype.Slime);
            if (monster == null) return false;
            monster.RestoreSimulationState(hp, nutrient, magic, direction);

            hpRemaining -= hp;
            nutrientRemaining -= nutrient;
            magicRemaining -= magic;
            state.ConsumeMaterialized(hp, nutrient, magic);
        }
        return true;
    }

    private void SpawnApproximateBuds(
        MonsterRegionState state,
        int requested,
        MonsterManager manager,
        GridManager grid)
    {
        if (requested <= 0 || manager == null) return;
        CollectTraversableCells(state.Key.GetBounds(RegionSize), grid);
        if (cellScratch.Count == 0) return;

        int count = Mathf.Min(requested, state.Count);
        for (int i = 0; i < count; i++)
        {
            int index = PositiveMod(state.NextSeed(), cellScratch.Count);
            Vector2Int cell = cellScratch[index];
            if (manager.HasBudOrFlowerAt(cell.x, cell.y)) continue;

            MonsterData bud = manager.Spawn(cell.x, cell.y, MonsterArchetype.Slime);
            if (bud == null) continue;
            int nutrient = MonsterArchetype.Slime.BudRequiredNutrient;
            bud.RestoreSimulationState(
                Mathf.Max(1, MonsterArchetype.Slime.BudHpThreshold),
                nutrient,
                0,
                Vector2Int.right);
            bud.SeedCollected(nutrient);
            bud.TransformTo(SlimeLifecycleStage.Bud, MonsterArchetype.Slime.BudMaxHP);
            bud.ResetBudStarve();
            state.ConsumeForBud(nutrient);
        }
    }

    private void MixRegionalNutrients(MonsterRegionState state, GridManager grid)
    {
        if (nutrientSoilScratch.Count < 2 || state.Count <= 0) return;
        int operations = Mathf.Min(32, Mathf.Max(1, state.Count / 4));

        for (int i = 0; i < operations; i++)
        {
            int sourceIndex = PositiveMod(state.NextSeed(), nutrientSoilScratch.Count);
            int targetIndex = PositiveMod(state.NextSeed(), nutrientSoilScratch.Count);
            if (sourceIndex == targetIndex) targetIndex = (targetIndex + 1) % nutrientSoilScratch.Count;

            Vector2Int source = nutrientSoilScratch[sourceIndex];
            Vector2Int target = nutrientSoilScratch[targetIndex];
            TileAttributeData sourceTile = grid.GetTileAttribute(source.x, source.y);
            int moved = sourceTile.WithdrawNutrient(1);
            if (moved <= 0) continue;
            TileAttributeData targetTile = grid.GetTileAttribute(target.x, target.y);
            targetTile.DepositNutrient(moved);
            grid.SetTileAttribute(source.x, source.y, sourceTile);
            grid.SetTileAttribute(target.x, target.y, targetTile);
        }
    }

    private void TryFlowToNeighbor(MonsterRegionState state, GridManager grid)
    {
        if (state == null || state.Count < 8) return;
        CollectAccessibleNeighbors(state.Key, grid);
        if (neighborScratch.Count == 0) return;

        int moving = Mathf.Max(1, state.Count / 20);
        MonsterRegionState transfer = state.ExtractPopulation(moving);
        if (transfer == null) return;

        int index = PositiveMod(state.NextSeed(), neighborScratch.Count);
        MonsterRegionKey destinationKey = neighborScratch[index];
        MonsterRegionState destination = GetOrCreate(destinationKey);
        destination.Merge(transfer);
    }

    private void CollectAccessibleNeighbors(MonsterRegionKey key, GridManager grid)
    {
        neighborScratch.Clear();
        RectInt bounds = key.GetBounds(RegionSize);

        for (int i = 0; i < NeighborRegions.Length; i++)
        {
            Vector2Int direction = NeighborRegions[i];
            if (HasBoundaryConnection(bounds, direction, grid))
                neighborScratch.Add(new MonsterRegionKey(key.X + direction.x, key.Y + direction.y));
        }
    }

    private static bool HasBoundaryConnection(RectInt bounds, Vector2Int direction, GridManager grid)
    {
        if (direction.x != 0)
        {
            int insideX = direction.x > 0 ? bounds.xMax - 1 : bounds.xMin;
            int outsideX = insideX + direction.x;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                if (grid.IsMonsterTraversable(insideX, y) && grid.IsMonsterTraversable(outsideX, y))
                    return true;
        }
        else
        {
            int insideY = direction.y > 0 ? bounds.yMax - 1 : bounds.yMin;
            int outsideY = insideY + direction.y;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
                if (grid.IsMonsterTraversable(x, insideY) && grid.IsMonsterTraversable(x, outsideY))
                    return true;
        }
        return false;
    }

    private void CollectTraversableCells(RectInt bounds, GridManager grid)
    {
        cellScratch.Clear();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                if (grid.IsMonsterTraversable(x, y))
                    cellScratch.Add(new Vector2Int(x, y));
    }

    private void CollectNutrientSoils(RectInt bounds, GridManager grid)
    {
        nutrientSoilScratch.Clear();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                if (grid.HasAbsorbableNutrient(x, y))
                    nutrientSoilScratch.Add(new Vector2Int(x, y));
    }

    private void CopyKeys()
    {
        keyScratch.Clear();
        foreach (KeyValuePair<MonsterRegionKey, MonsterRegionState> pair in regions)
            keyScratch.Add(pair.Key);
    }

    private static int PositiveMod(int value, int divisor)
    {
        if (divisor <= 0) return 0;
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
