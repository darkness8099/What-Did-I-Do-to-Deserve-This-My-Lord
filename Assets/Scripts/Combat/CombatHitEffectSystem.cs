using UnityEngine;

public sealed class CombatHitEffectSystem : MonoBehaviour
{
    private const string HeroBloodPrefabPath = "FX/PF_Hit_Hero_BloodArc";
    private const string MonsterImpactPrefabPath = "FX/PF_Hit_Monster_ImpactWhite";
    private const float EffectLifetimeSeconds = 0.4f;

    private GameObject heroBloodPrefab;
    private GameObject monsterImpactPrefab;
    private Transform effectRoot;

    public void Initialize()
    {
        heroBloodPrefab = Resources.Load<GameObject>(HeroBloodPrefabPath);
        monsterImpactPrefab = Resources.Load<GameObject>(MonsterImpactPrefabPath);

        if (effectRoot == null)
        {
            effectRoot = new GameObject("CombatHitEffects").transform;
            effectRoot.SetParent(transform, false);
        }

        if (heroBloodPrefab == null)
            Debug.LogWarning($"[CombatHitEffectSystem] Missing Resources prefab: {HeroBloodPrefabPath}");
        if (monsterImpactPrefab == null)
            Debug.LogWarning($"[CombatHitEffectSystem] Missing Resources prefab: {MonsterImpactPrefabPath}");
    }

    public bool PlayHeroBlood(GameObject heroView)
    {
        float randomCardinalAngle = Random.Range(0, 4) * 90f;
        return Spawn(heroBloodPrefab, heroView, randomCardinalAngle);
    }

    public bool PlayMonsterImpact(GameObject monsterView, Vector2Int forceDirection)
    {
        forceDirection = CombatSystem.GetCardinalDirection(forceDirection);
        if (forceDirection == Vector2Int.zero) forceDirection = Vector2Int.right;
        float angle = Mathf.Atan2(forceDirection.y, forceDirection.x) * Mathf.Rad2Deg;
        return Spawn(monsterImpactPrefab, monsterView, angle);
    }

    private bool Spawn(GameObject prefab, GameObject targetView, float angle)
    {
        if (prefab == null || targetView == null) return false;

        GameObject effect = Instantiate(
            prefab,
            targetView.transform.position,
            Quaternion.Euler(0f, 0f, angle),
            effectRoot);
        Destroy(effect, EffectLifetimeSeconds);
        return true;
    }
}
