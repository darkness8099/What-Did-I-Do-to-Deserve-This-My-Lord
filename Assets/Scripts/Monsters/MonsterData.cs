using System;
using System.Collections.Generic;
using UnityEngine;

// ===== Ecology role and movement strategy (fields reserved; behavior not yet implemented) =====

public enum MonsterEcologyRole
{
    None,
    NutrientCarrier, // basic moss/slime nutrient carrier (formerly "Carrier"; merged to remove duplicate)
    Consumer,
    Predator,
    Magical,
    Support,
    Apex,
}

public enum MonsterMoveStrategy
{
    Static,
    StraightUntilWall, // straight line, only turns on wall collision (Slime/Moss); behavior not yet implemented
    RandomWalk,
    WallFollow,
    SeekResource,
    SeekFood,
    Flee,
}

// Flower reproduction spawn placement order (data placeholder; behavior not yet implemented)
public enum SlimeSpawnOriginPriority
{
    OriginThenNeighborsFixedOrder,
}

// Lifecycle stage of a moss/slime line (TASK-060 scaffolding; transitions/behavior not yet implemented)
public enum SlimeLifecycleStage
{
    Crawling, // 匍匐苔藓 — base moving nutrient carrier
    Bud,      // 花苞
    Flower,   // 花
}

// ===== Archetype: stable identity + base stats. The single source of truth for "what kind of monster this is". =====

public class MonsterArchetype
{
    public string              Id                   { get; set; }
    public string              DisplayName          { get; set; }
    public MonsterEcologyRole  Role                 { get; set; }
    public MonsterMoveStrategy Move                 { get; set; }
    public int                 BaseMaxHP            { get; set; }
    public int                 BaseAttack           { get; set; }
    public float               AttackRange          { get; set; }
    public int                 NutrientCapacity     { get; set; }
    public int                 MagicCapacity        { get; set; }
    public int                 HungerMax            { get; set; } // reserved; v1 NOT used — Slime/Moss decay is via HpCostPerMove (see below)
    public TileElementType     SpawnElement         { get; set; }

    // ===== Lifecycle / ecology tuning (Slime/Moss; see GAME_DESIGN_SLIME.md). =====
    // Data placeholders only — behavior is NOT implemented yet. Numbers live here, never hard-coded in behavior logic.
    public int   InitialHP                         { get; set; }
    public int   BudRequiredNutrient               { get; set; }
    public int   BudHpThreshold                    { get; set; }

    public int   HpCostPerMove                     { get; set; }
    public int   HpCostPerMoveRandomMin            { get; set; }
    public int   HpCostPerMoveRandomMax            { get; set; }
    public bool  UseRandomMoveHpCost               { get; set; }
    public int   HpHealPerAbsorb                   { get; set; }
    public int   HpCostCooldownMoves               { get; set; } // moves required between each HpCostPerMove deduction
    public int   EcologyActionCooldownMoves        { get; set; } // moves required between absorb/release (must move ≥1)
    public float SpawnMoveDelayMaxSeconds          { get; set; } // newborn (flower-spawned) startup delay upper bound

    public float AbsorbReleaseTickSeconds          { get; set; }
    public float MoveTickSeconds                   { get; set; }
    public int   AbsorbWhenNutrientLessOrEqual     { get; set; }
    public int   ReleaseWhenNutrientGreaterOrEqual { get; set; }
    public int   KeepNutrientOnRelease             { get; set; }

    // Bud stage
    public int   BudMaxHP                          { get; set; }
    public int   BudAbsorbRadius                   { get; set; }
    public int   BudToFlowerNutrient               { get; set; }
    public int   BudHpDecayPerTick                 { get; set; }
    public int   BudStarvationCooldownTicks        { get; set; } // consecutive no-absorb Bud ticks before losing 1 HP
    public float BudTickSeconds                    { get; set; }

    // Flower stage
    public int   FlowerMaxHP                       { get; set; }
    public int   FlowerAbsorbRadius                { get; set; }
    public int   FlowerMaxAbsorb                   { get; set; }
    public int   FlowerHpDecayPerTick              { get; set; }
    public float FlowerTickSeconds                 { get; set; }

    // Flower reproduction
    public int                      FlowerMaxSpawn      { get; set; }
    public int                      NutrientPerSpawn    { get; set; }
    public SlimeSpawnOriginPriority SpawnOriginPriority { get; set; }
    public bool                     AllowStackSpawn     { get; set; }

    // Slime: basic nutrient-carrier. MagicCapacity intentionally 0.
    public static readonly MonsterArchetype Slime = new MonsterArchetype
    {
        Id               = "slime",
        DisplayName      = "Slime",
        Role             = MonsterEcologyRole.NutrientCarrier,
        Move             = MonsterMoveStrategy.StraightUntilWall,
        BaseMaxHP        = 21,
        BaseAttack       = 2,
        AttackRange      = 1.0f,
        NutrientCapacity = 3,
        MagicCapacity    = 0,
        HungerMax        = 10,
        SpawnElement     = TileElementType.Slime,

        // ----- Lifecycle / ecology tuning (conservative v1; see GAME_DESIGN_SLIME.md) -----
        InitialHP                         = 16,
        BudRequiredNutrient               = 2,
        BudHpThreshold                    = 2,

        HpCostPerMove                     = 1,
        HpCostPerMoveRandomMin            = 1,
        HpCostPerMoveRandomMax            = 2,
        UseRandomMoveHpCost               = false, // v1: fixed HpCostPerMove for debugging lifecycle pacing
        HpHealPerAbsorb                   = 1,     // absorb heals only 1 (can't keep moss permanently full → Bud can still trigger)
        HpCostCooldownMoves               = 2,     // lose 1 HP every 2 moves (net ~ -1 HP per 4-move absorb/release cycle)
        EcologyActionCooldownMoves        = 2,     // every 2 moves → at most 1 absorb/release (can't churn in place)
        SpawnMoveDelayMaxSeconds          = 2.0f,  // flower-born slimes start moving after random(0, this) seconds

        AbsorbReleaseTickSeconds          = 1.0f,  // v1: kept separate per stage (Slime/Bud/Flower may differ later); not merged
        MoveTickSeconds                   = 1.0f,
        // ===== v1 Moss nutrient ladder (closer to original; no stable death-point) =====
        // Nutrient <=1 → absorb 1 from an adjacent Soil that HAS nutrient (>0), and heal.
        // Nutrient >=2 → release surplus to an adjacent Soil that HAS nutrient (>0), keeping KeepNutrientOnRelease(1).
        // This oscillates 1<->2 so the moss keeps foraging/depositing (no stable-at-2 lock-up).
        // BudRequiredNutrient(2) is ONLY the death→Bud gate, NOT a permanent reserve.
        // (No averaging / no "find poorest"; never release onto a 0-nutrient tile.)
        AbsorbWhenNutrientLessOrEqual     = 1,
        ReleaseWhenNutrientGreaterOrEqual = 2,
        KeepNutrientOnRelease             = 1,

        BudMaxHP                          = 10,
        BudAbsorbRadius                   = 2,
        BudToFlowerNutrient               = 8,
        BudHpDecayPerTick                 = 1,
        BudStarvationCooldownTicks        = 3,     // 3 consecutive no-absorb ticks → -1 HP (absorb-tick never costs HP)
        BudTickSeconds                    = 1.0f,

        FlowerMaxHP                       = 21,
        FlowerAbsorbRadius                = 3,
        FlowerMaxAbsorb                   = 11,
        FlowerHpDecayPerTick              = 4,
        FlowerTickSeconds                 = 1.0f,

        FlowerMaxSpawn                    = 5,
        NutrientPerSpawn                  = 2,
        SpawnOriginPriority               = SlimeSpawnOriginPriority.OriginThenNeighborsFixedOrder,
        AllowStackSpawn                   = false,
    };
}

// ===== Registry: lookup archetype by id (used by MonsterIdentity on prefab) =====

public static class MonsterArchetypeRegistry
{
    private static readonly Dictionary<string, MonsterArchetype> _byId
        = new Dictionary<string, MonsterArchetype>();

    static MonsterArchetypeRegistry()
    {
        Register(MonsterArchetype.Slime);
    }

    public static void Register(MonsterArchetype archetype)
    {
        if (archetype == null || string.IsNullOrEmpty(archetype.Id)) return;
        _byId[archetype.Id] = archetype;
    }

    public static MonsterArchetype Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _byId.TryGetValue(id, out var archetype);
        return archetype;
    }
}

// ===== Deprecated identity enum. Kept for compat; no live references after TASK-037 refactor. =====

[Obsolete("Use MonsterArchetype as identity. MonsterType will be removed once external references are confirmed clean.")]
public enum MonsterType
{
    Slime,
}

// ===== Runtime monster data =====

public class MonsterData
{
    public MonsterArchetype Archetype { get; private set; }

    public string              DisplayName  => Archetype.DisplayName;
    public MonsterEcologyRole  Role         => Archetype.Role;
    public MonsterMoveStrategy Move         => Archetype.Move;

    public int   MaxHP        { get; private set; }
    public int   CurrentHP    { get; private set; }
    public int   Attack       { get; private set; }
    public float AttackRange  { get; private set; }

    // Carried resources (ecology: empty at spawn; filled by AbsorbFromTile; resolved by cause-specific resource flow)
    public int CurrentNutrient    { get; private set; }
    public int CurrentMagic       { get; private set; }
    public int NutrientCapacity   => Archetype.NutrientCapacity;
    public int MagicCapacity      => Archetype.MagicCapacity;

    // Collected nutrient accumulator for Bud/Flower stages (uncapped by NutrientCapacity; Bud→Flower & reproduction use it). TASK-065/066.
    public int CollectedNutrient { get; private set; }
    public void SeedCollected(int amount) => CollectedNutrient = Mathf.Max(0, amount);
    public void AddCollected(int amount) { if (amount > 0) CollectedNutrient += amount; }

    // Hunger — reserved, NOT used in v1. Slime/Moss natural decay is driven by MonsterArchetype.HpCostPerMove
    // (HP consumed per move), per design rule "先按移动/生态行动消耗 HP". Kept in case hunger-based decay returns.
    public int Hunger    { get; private set; }
    public int HungerMax => Archetype.HungerMax;

    // ===== Lifecycle stage (TASK-060 scaffolding) =====
    // Default Crawling at spawn. Stage transitions (→Bud/→Flower) are driven by future lifecycle logic, not here.
    public SlimeLifecycleStage Stage { get; private set; }
    public void SetLifecycleStage(SlimeLifecycleStage stage) => Stage = stage;

    // Consecutive Bud ticks with no absorption (gates Bud HP decay; reset on absorb or after a deduction).
    public int BudStarveCounter { get; private set; }
    public void RegisterBudStarve() => BudStarveCounter++;
    public void ResetBudStarve() => BudStarveCounter = 0;

    // Newborn startup delay: until Time.time reaches SpawnReadyTime, the slime idles (no move/ecology/HP).
    public float SpawnReadyTime { get; private set; }
    public void SetSpawnDelay(float seconds) => SpawnReadyTime = Time.time + Mathf.Max(0f, seconds);
    public bool IsSpawnDelayed() => Time.time < SpawnReadyTime;

    // Lifecycle transform (TASK-064): switch stage and reset the HP pool to the new stage's max.
    // Carried Nutrient/Magic are preserved (Bud/Flower keep accumulating).
    public void TransformTo(SlimeLifecycleStage stage, int maxHp)
    {
        Stage     = stage;
        MaxHP     = Mathf.Max(1, maxHp);
        CurrentHP = MaxHP;
    }

    // ===== Movement facing (TASK-062) — for rule-based straight-until-wall movement =====
    public Vector2Int MoveDirection { get; private set; }
    public void SetMoveDirection(Vector2Int dir) => MoveDirection = dir;

    // ===== Grid position (per-monster identity; multiple monsters may share a cell — no collision volume) =====
    public Vector2Int Position { get; private set; }
    public void SetPosition(Vector2Int p) => Position = p;

    // Move counters gating ecology actions and HP cost (each reset independently when its action fires).
    public int MovesSinceEcology { get; private set; }
    public int MovesSinceHpCost  { get; private set; }
    public void RegisterMove() { MovesSinceEcology++; MovesSinceHpCost++; }
    public void ResetEcologyCounter() => MovesSinceEcology = 0;
    public void ResetHpCostCounter() => MovesSinceHpCost = 0;

    public MonsterData(MonsterArchetype archetype)
    {
        if (archetype == null)
        {
            Debug.LogError("[MonsterData] Null archetype, falling back to Slime.");
            archetype = MonsterArchetype.Slime;
        }
        Archetype       = archetype;
        MaxHP           = archetype.BaseMaxHP;
        // Spawn at InitialHP (not full MaxHP) when configured; fall back to MaxHP. (TASK-064)
        CurrentHP       = archetype.InitialHP > 0
            ? Mathf.Clamp(archetype.InitialHP, 1, MaxHP)
            : MaxHP;
        Attack          = archetype.BaseAttack;
        AttackRange     = archetype.AttackRange;
        CurrentNutrient = 0;
        CurrentMagic    = 0;
        Hunger           = 0;
        CollectedNutrient = 0;
        Stage            = SlimeLifecycleStage.Crawling;
        MoveDirection    = Vector2Int.right;
    }

    public bool IsAlive() => CurrentHP > 0;

    public void TakeDamage(int damage)
    {
        if (damage < 0)
        {
            Debug.LogWarning("[MonsterData] TakeDamage called with negative value. Ignored.");
            return;
        }
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
    }

    // Heal up to MaxHP (TASK-063: absorbing nutrient restores HP). Negative/zero ignored.
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    // Absorb resources from a tile up to remaining capacity. tile is modified by ref.
    public void AbsorbFromTile(ref TileAttributeData tile)
    {
        int wantN = NutrientCapacity - CurrentNutrient;
        if (wantN > 0) CurrentNutrient += tile.WithdrawNutrient(wantN);
        int wantM = MagicCapacity - CurrentMagic;
        if (wantM > 0) CurrentMagic    += tile.WithdrawMagic(wantM);
    }

    public int WithdrawNutrient(int request)
    {
        int take = Mathf.Clamp(request, 0, CurrentNutrient);
        CurrentNutrient -= take;
        return take;
    }

    public int WithdrawMagic(int request)
    {
        int take = Mathf.Clamp(request, 0, CurrentMagic);
        CurrentMagic -= take;
        return take;
    }

    public int ReceiveNutrient(int amount)
    {
        int room = NutrientCapacity - CurrentNutrient;
        int take = Mathf.Clamp(amount, 0, room);
        CurrentNutrient += take;
        return amount - take;
    }

    public int ReceiveMagic(int amount)
    {
        int room = MagicCapacity - CurrentMagic;
        int take = Mathf.Clamp(amount, 0, room);
        CurrentMagic += take;
        return amount - take;
    }

    // Lifecycle tick reserved (future: hunger++, behavior step).
    public void Tick() { }
}
