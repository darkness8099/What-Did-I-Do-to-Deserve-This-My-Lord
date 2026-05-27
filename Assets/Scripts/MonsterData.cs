using UnityEngine;

public enum MonsterType
{
    Slime
}

public class MonsterData
{
    public MonsterType Type       { get; private set; }
    public string      DisplayName { get; private set; }
    public int         MaxHP       { get; private set; }
    public int         CurrentHP   { get; private set; }
    public int         Attack      { get; private set; }
    public float       AttackRange { get; private set; }

    public MonsterData(MonsterType type)
    {
        Type = type;

        switch (type)
        {
            case MonsterType.Slime:
            default:
                DisplayName = "Slime";
                MaxHP       = 10;
                CurrentHP   = 10;
                Attack      = 2;
                AttackRange = 1.0f;
                break;
        }
    }

    public bool IsAlive()
    {
        return CurrentHP > 0;
    }

    public void TakeDamage(int damage)
    {
        if (damage < 0)
        {
            Debug.LogWarning("[MonsterData] TakeDamage called with negative value. Ignored.");
            return;
        }
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
    }
}
