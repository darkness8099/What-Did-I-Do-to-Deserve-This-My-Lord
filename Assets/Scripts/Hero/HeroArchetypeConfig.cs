using UnityEngine;

[CreateAssetMenu(menuName = "Game/Hero Archetype", fileName = "hero_archetype")]
public class HeroArchetypeConfig : ScriptableObject
{
    [SerializeField] private string heroId = "warrior";
    [SerializeField] private string displayName = "Warrior";
    [SerializeField] private int maxHP = 30;
    [SerializeField] private int attack = 3;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackSpeed = 2f;
    [SerializeField] private HeroAttackType attackType = HeroAttackType.Normal;
    [SerializeField] private Sprite sprite;
    [SerializeField] private RuntimeAnimatorController animatorController;

    private static HeroArchetypeConfig runtimeDefault;

    public string HeroId => string.IsNullOrWhiteSpace(heroId) ? "hero" : heroId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? HeroId : displayName;
    public int MaxHP => Mathf.Max(1, maxHP);
    public int Attack => Mathf.Max(0, attack);
    public float MoveSpeed => Mathf.Max(0.01f, moveSpeed);
    public float AttackRange => Mathf.Max(0f, attackRange);
    public float AttackSpeed => Mathf.Max(0.01f, attackSpeed);
    public HeroAttackType AttackType => attackType;
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;

    public static HeroArchetypeConfig RuntimeDefault
    {
        get
        {
            if (runtimeDefault != null) return runtimeDefault;
            runtimeDefault = CreateInstance<HeroArchetypeConfig>();
            runtimeDefault.hideFlags = HideFlags.HideAndDontSave;
            runtimeDefault.name = "RuntimeDefaultWarrior";
            return runtimeDefault;
        }
    }

    public void Configure(
        string id,
        string nameValue,
        int hp,
        int attackValue,
        float moveSpeedValue,
        float attackRangeValue,
        float attackSpeedValue,
        HeroAttackType type,
        Sprite spriteValue = null,
        RuntimeAnimatorController controllerValue = null)
    {
        heroId = id;
        displayName = nameValue;
        maxHP = hp;
        attack = attackValue;
        moveSpeed = moveSpeedValue;
        attackRange = attackRangeValue;
        attackSpeed = attackSpeedValue;
        attackType = type;
        sprite = spriteValue;
        animatorController = controllerValue;
    }
}
