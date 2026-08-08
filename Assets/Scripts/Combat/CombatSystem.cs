using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatResult { public bool HeroSurvived = true; }

public class CombatSystem : MonoBehaviour
{
    private HeroManager     heroManager;
    private HeroRenderer    heroRenderer;
    private MonsterManager  monsterManager;
    private MonsterRenderer monsterRenderer;
    private GridManager     gridManager;
    private EcologyTickDriver ecologyTickDriver;
    private readonly List<GameObject> heroAttackViews = new List<GameObject>(1);
    private readonly List<GameObject> monsterAttackViews = new List<GameObject>();

    private void Start()
    {
        heroManager     = GetComponent<HeroManager>()     ?? FindObjectOfType<HeroManager>();
        heroRenderer    = GetComponent<HeroRenderer>()    ?? FindObjectOfType<HeroRenderer>();
        monsterManager  = GetComponent<MonsterManager>()  ?? FindObjectOfType<MonsterManager>();
        monsterRenderer = GetComponent<MonsterRenderer>() ?? FindObjectOfType<MonsterRenderer>();
        gridManager     = GetComponent<GridManager>()     ?? FindObjectOfType<GridManager>();
        ecologyTickDriver = GetComponent<EcologyTickDriver>() ?? FindObjectOfType<EcologyTickDriver>();

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
        if (ecologyTickDriver != null)
            ecologyTickDriver.EnsureExactAround(gridPos, 0);

        MonsterData target = monsterManager != null
            ? monsterManager.GetMonster(gridPos.x, gridPos.y)
            : null;
        yield return ResolveCombat(heroId, target, result);
    }

    public IEnumerator ResolveCombat(int heroId, MonsterData initialTarget, CombatResult result)
    {
        if (result == null) yield break;
        result.HeroSurvived = true;

        if (heroManager == null || monsterManager == null || !heroManager.HasHero(heroId))
        {
            result.HeroSurvived = false;
            yield break;
        }

        HeroData hero = heroManager.GetHero(heroId);
        MonsterData target = initialTarget;
        var attackers = new List<MonsterData>();
        float interval = hero.AttackSpeed > 0f ? 1f / hero.AttackSpeed : 0.5f;

        while (hero.IsAlive() && heroManager.HasHero(heroId))
        {
            Vector2Int heroPos = heroManager.GetHeroPosition(heroId);
            if (ecologyTickDriver != null)
                ecologyTickDriver.EnsureExactAround(heroPos, Mathf.CeilToInt(hero.AttackRange));

            if (!IsValidHeroTarget(target, heroPos, hero.AttackRange))
                target = monsterManager.FindNearestMonsterTargetInRange(heroPos, hero.AttackRange);
            if (target == null) yield break;

            Debug.Log($"[CombatSystem] Hero {heroId} targets {target.DisplayName} at {target.Position} "
                    + $"(HeroHP={hero.CurrentHP}, MonsterHP={target.CurrentHP}).");

            // Normal attacks always damage exactly one MonsterData instance, even when its cell is stacked.
            Vector2Int attackDirection = GetCardinalDirection(target.Position - heroPos);
            if (attackDirection != Vector2Int.zero)
            {
                hero.SetFacingDirection(attackDirection);
                if (heroRenderer != null)
                    heroRenderer.SetHeroMotion(heroId, attackDirection, false);
            }

            float presentationDuration = CombatPresentation.GetPhaseDuration(interval);
            heroAttackViews.Clear();
            GameObject heroView = heroRenderer != null ? heroRenderer.GetHeroView(heroId) : null;
            if (heroView != null) heroAttackViews.Add(heroView);
            GameObject targetView = monsterRenderer != null ? monsterRenderer.GetMonsterView(target) : null;

            bool heroAttackApplied = false;
            yield return CombatPresentation.PlayAttack(
                heroAttackViews,
                targetView,
                presentationDuration,
                () => heroAttackApplied = ApplySingleTargetHeroAttack(hero, target));

            // Another hero may have defeated this same instance while both attack visuals were running.
            if (!heroAttackApplied)
            {
                target = monsterManager.FindNearestMonsterTargetInRange(heroPos, hero.AttackRange);
                if (target == null) yield break;
                continue;
            }

            if (!target.IsAlive())
            {
                ResourceFlow.ScatterOrdinaryDeathResources(
                    target.Position,
                    target,
                    gridManager,
                    DeathCause.HeroKill,
                    target.DisplayName);
                if (monsterRenderer != null) monsterRenderer.NotifyMonsterDied(target);
                Debug.Log($"[CombatSystem] {target.DisplayName} defeated at {target.Position}. Hero HP: {hero.CurrentHP}");
                monsterManager.Remove(target);
                target = monsterManager.FindNearestMonsterTargetInRange(heroPos, hero.AttackRange);
                if (target == null) yield break;
            }

            float waitAfterPresentation = interval - presentationDuration;
            if (waitAfterPresentation > 0f)
                yield return new WaitForSeconds(waitAfterPresentation);

            if (!heroManager.HasHero(heroId)) yield break;
            heroPos = heroManager.GetHeroPosition(heroId);
            monsterManager.CollectCombatAttackers(heroPos, attackers);

            monsterAttackViews.Clear();
            for (int i = 0; i < attackers.Count; i++)
            {
                MonsterData attacker = attackers[i];
                if (!CanMonsterAttack(attacker)) continue;
                GameObject attackerView = monsterRenderer != null
                    ? monsterRenderer.GetMonsterView(attacker)
                    : null;
                if (attackerView != null) monsterAttackViews.Add(attackerView);
            }

            heroView = heroRenderer != null ? heroRenderer.GetHeroView(heroId) : null;
            yield return CombatPresentation.PlayAttack(
                monsterAttackViews,
                heroView,
                presentationDuration,
                () => ApplyMonsterAttackPhase(hero, attackers));

            if (!hero.IsAlive())
            {
                heroManager.RemoveHero(heroId);
                if (heroRenderer != null) heroRenderer.RemoveHeroView(heroId);
                Debug.Log($"[CombatSystem] Hero {heroId} defeated by {attackers.Count} nearby monster(s).");
                result.HeroSurvived = false;
                yield break;
            }

            if (waitAfterPresentation > 0f)
                yield return new WaitForSeconds(waitAfterPresentation);
        }
    }

    public static bool ApplySingleTargetHeroAttack(HeroData hero, MonsterData target)
    {
        if (hero == null || target == null || !hero.IsAlive() || !target.IsAlive()) return false;
        target.TakeDamage(hero.Attack);
        return true;
    }

    public static int ApplyMonsterAttackPhase(HeroData hero, IList<MonsterData> attackers)
    {
        if (hero == null || attackers == null || !hero.IsAlive()) return 0;

        int attackCount = 0;
        for (int i = 0; i < attackers.Count && hero.IsAlive(); i++)
        {
            MonsterData monster = attackers[i];
            if (!CanMonsterAttack(monster)) continue;

            hero.TakeDamage(monster.Attack);
            attackCount++;
        }
        return attackCount;
    }

    public static bool CanMonsterAttack(MonsterData monster)
    {
        return monster != null
            && monster.IsAlive()
            && monster.Stage == SlimeLifecycleStage.Crawling
            && !monster.IsSpawnDelayed();
    }

    public static Vector2Int GetCardinalDirection(Vector2Int delta)
    {
        if (delta == Vector2Int.zero) return Vector2Int.zero;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && delta.x != 0)
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    private bool IsValidHeroTarget(MonsterData target, Vector2Int heroPos, float range)
    {
        if (target == null || !target.IsAlive() || !monsterManager.Contains(target)) return false;
        float distance = Mathf.Abs(target.Position.x - heroPos.x)
                       + Mathf.Abs(target.Position.y - heroPos.y);
        return distance <= range;
    }
}
