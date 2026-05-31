using System;
using System.Collections.Generic;
using UnityEngine;

// ===== Ecology role and movement strategy (fields reserved; behavior not yet implemented) =====

public enum MonsterEcologyRole
{
    None,
    Carrier,
    Consumer,
    Predator,
    Magical,
    Support,
    Apex,
}

public enum MonsterMoveStrategy
{
    Static,
    RandomWalk,
    WallFollow,
    SeekResource,
    SeekFood,
    Flee,
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
    public int                 HungerMax            { get; set; }
    public TileElementType     SpawnElement         { get; set; }

    // Slime: basic nutrient-carrier. MagicCapacity intentionally 0.
    public static readonly MonsterArchetype Slime = new MonsterArchetype
    {
        Id               = "slime",
        DisplayName      = "Slime",
        Role             = MonsterEcologyRole.Carrier,
        Move             = MonsterMoveStrategy.Static,
        BaseMaxHP        = 10,
        BaseAttack       = 2,
        AttackRange      = 1.0f,
        NutrientCapacity = 5,
        MagicCapacity    = 0,
        HungerMax        = 10,
        SpawnElement     = TileElementType.Slime,
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

    // Hunger — field reserved for future ecology tick
    public int Hunger    { get; private set; }
    public int HungerMax => Archetype.HungerMax;

    public MonsterData(MonsterArchetype archetype)
    {
        if (archetype == null)
        {
            Debug.LogError("[MonsterData] Null archetype, falling back to Slime.");
            archetype = MonsterArchetype.Slime;
        }
        Archetype       = archetype;
        MaxHP           = archetype.BaseMaxHP;
        CurrentHP       = archetype.BaseMaxHP;
        Attack          = archetype.BaseAttack;
        AttackRange     = archetype.AttackRange;
        CurrentNutrient = 0;
        CurrentMagic    = 0;
        Hunger          = 0;
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
