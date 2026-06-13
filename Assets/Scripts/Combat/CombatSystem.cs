using System.Collections;
using UnityEngine;

public class CombatResult { public bool HeroSurvived = true; }

public class CombatSystem : MonoBehaviour
{
    private HeroManager     heroManager;
    private HeroRenderer    heroRenderer;
    private MonsterManager  monsterManager;
    private MonsterRenderer monsterRenderer;
    private GridManager     gridManager;

    private void Start()
    {
        heroManager     = GetComponent<HeroManager>()     ?? FindObjectOfType<HeroManager>();
        heroRenderer    = GetComponent<HeroRenderer>()    ?? FindObjectOfType<HeroRenderer>();
        monsterManager  = GetComponent<MonsterManager>()  ?? FindObjectOfType<MonsterManager>();
        monsterRenderer = GetComponent<MonsterRenderer>() ?? FindObjectOfType<MonsterRenderer>();
        gridManager     = GetComponent<GridManager>()     ?? FindObjectOfType<GridManager>();

        if (heroManager     == null) Debug.LogError("[CombatSystem] HeroManager not found.");
        if (heroRenderer    == null) Debug.LogError("[CombatSystem] HeroRenderer not found.");
        if (monsterManager  == null) Debug.LogError("[CombatSystem] MonsterManager not found.");
        if (monsterRenderer == null) Debug.LogError("[CombatSystem] MonsterRenderer not found.");
        if (gridManager     == null) Debug.LogError("[CombatSystem] GridManager not found.");

        Debug.Log("[CombatSystem] Initialized.");
    }

    // Returns true if hero is still alive; false if hero died.
    public IEnumerator ResolveCombatAt(int heroId, Vector2Int gridPos, CombatResult result)
    {
        result.HeroSurvived = true;

        if (!monsterManager.HasMonster(gridPos.x, gridPos.y))
            yield break;

        if (!heroManager.HasHero(heroId))
        {
            result.HeroSurvived = false;
            yield break;
        }

        HeroData    hero    = heroManager.GetHero(heroId);
        MonsterData monster = monsterManager.GetMonster(gridPos.x, gridPos.y);

        Debug.Log($"[CombatSystem] Combat at {gridPos}: Hero(HP={hero.CurrentHP}) vs {monster.DisplayName}(HP={monster.CurrentHP})");

        float interval = hero.AttackSpeed > 0f ? 1f / hero.AttackSpeed : 0.5f;

        while (hero.IsAlive() && monster.IsAlive())
        {
            monster.TakeDamage(hero.Attack);
            if (!monster.IsAlive())
            {
                ResourceFlow.ScatterOrdinaryDeathResources(
                    gridPos,
                    monster,
                    gridManager,
                    DeathCause.HeroKill,
                    monster.DisplayName);
                if (monsterRenderer != null) monsterRenderer.NotifyMonsterDied(monster); // play death anim then destroy view
                monsterManager.Remove(monster);
                Debug.Log($"[CombatSystem] {monster.DisplayName} defeated at {gridPos}. Hero HP: {hero.CurrentHP}");
                yield break;
            }

            yield return new WaitForSeconds(interval);

            hero.TakeDamage(monster.Attack);
            if (!hero.IsAlive())
            {
                heroManager.RemoveHero(heroId);
                heroRenderer.RemoveHeroView(heroId);
                Debug.Log($"[CombatSystem] Hero {heroId} defeated at {gridPos}.");
                result.HeroSurvived = false;
                yield break;
            }

            yield return new WaitForSeconds(interval);
        }
    }
}
