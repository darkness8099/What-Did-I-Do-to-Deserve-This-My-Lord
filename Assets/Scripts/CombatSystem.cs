using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    private HeroManager     heroManager;
    private HeroRenderer    heroRenderer;
    private MonsterManager  monsterManager;
    private MonsterRenderer monsterRenderer;

    private void Start()
    {
        heroManager     = GetComponent<HeroManager>()     ?? FindObjectOfType<HeroManager>();
        heroRenderer    = GetComponent<HeroRenderer>()    ?? FindObjectOfType<HeroRenderer>();
        monsterManager  = GetComponent<MonsterManager>()  ?? FindObjectOfType<MonsterManager>();
        monsterRenderer = GetComponent<MonsterRenderer>() ?? FindObjectOfType<MonsterRenderer>();

        if (heroManager     == null) Debug.LogError("[CombatSystem] HeroManager not found.");
        if (heroRenderer    == null) Debug.LogError("[CombatSystem] HeroRenderer not found.");
        if (monsterManager  == null) Debug.LogError("[CombatSystem] MonsterManager not found.");
        if (monsterRenderer == null) Debug.LogError("[CombatSystem] MonsterRenderer not found.");

        Debug.Log("[CombatSystem] Initialized.");
    }

    // Returns true if hero is still alive; false if hero died.
    public bool ResolveCombatAt(int heroId, Vector2Int gridPos)
    {
        if (!monsterManager.HasMonster(gridPos.x, gridPos.y))
            return true;

        if (!heroManager.HasHero(heroId))
            return false;

        HeroData    hero    = heroManager.GetHero(heroId);
        MonsterData monster = monsterManager.GetMonster(gridPos.x, gridPos.y);

        Debug.Log($"[CombatSystem] Combat started at {gridPos}: Hero(HP={hero.CurrentHP}) vs {monster.DisplayName}(HP={monster.CurrentHP})");

        while (hero.IsAlive() && monster.IsAlive())
        {
            monster.TakeDamage(hero.Attack);
            if (!monster.IsAlive())
            {
                monsterManager.RemoveMonster(gridPos.x, gridPos.y);
                monsterRenderer.RemoveMonsterView(gridPos.x, gridPos.y);
                Debug.Log($"[CombatSystem] Slime defeated at {gridPos}. Hero HP remaining: {hero.CurrentHP}");
                return true;
            }

            hero.TakeDamage(monster.Attack);
            if (!hero.IsAlive())
            {
                heroManager.RemoveHero(heroId);
                heroRenderer.RemoveHeroView(heroId);
                Debug.Log($"[CombatSystem] Hero {heroId} defeated at {gridPos}.");
                return false;
            }
        }

        return hero.IsAlive();
    }
}
