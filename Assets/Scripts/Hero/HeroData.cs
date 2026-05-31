using UnityEngine;

public enum HeroAttackType { Normal, AoE, Magic, Ranged }

public class HeroData
{
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
    {
        DisplayName = "Hero";
        MaxHP       = 30;
        CurrentHP   = 30;
        Attack      = 3;
        MoveSpeed   = 2.0f;
        AttackRange = 1.0f;
        AttackSpeed = 2.0f;
        AttackType      = HeroAttackType.Normal;
        FacingDirection = Vector2Int.right;
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
