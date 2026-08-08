using UnityEngine;

public enum HeroAttackType { Normal, AoE, Magic, Ranged }

public class HeroData
{
    public HeroArchetypeConfig Archetype { get; private set; }
    public string ArchetypeId => Archetype != null ? Archetype.HeroId : "hero";
    public string DisplayName { get; private set; }
    public int    MaxHP       { get; private set; }
    public int    CurrentHP   { get; private set; }
    public int    Attack      { get; private set; }
    public float  MoveSpeed   { get; private set; }
    public float          AttackRange { get; private set; }
    public float          AttackSpeed { get; private set; }
    public HeroAttackType AttackType      { get; private set; }
    public Vector2Int     FacingDirection { get; private set; }

    public HeroData()
        : this(HeroArchetypeConfig.RuntimeDefault)
    {
    }

    public HeroData(HeroArchetypeConfig archetype)
    {
        Archetype = archetype != null ? archetype : HeroArchetypeConfig.RuntimeDefault;
        DisplayName = Archetype.DisplayName;
        MaxHP       = Archetype.MaxHP;
        CurrentHP   = MaxHP;
        Attack      = Archetype.Attack;
        MoveSpeed   = Archetype.MoveSpeed;
        AttackRange = Archetype.AttackRange;
        AttackSpeed = Archetype.AttackSpeed;
        AttackType      = Archetype.AttackType;
        FacingDirection = Vector2Int.down;
    }

    public bool IsAlive()
    {
        return CurrentHP > 0;
    }

    public void TakeDamage(int damage)
    {
        if (damage < 0) return;
        CurrentHP = System.Math.Max(0, CurrentHP - damage);
    }


public void SetFacingDirection(Vector2Int direction) { FacingDirection = direction; }
}
