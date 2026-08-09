using System.Collections.Generic;
using UnityEngine;
public sealed class HeroProjectileSystem : MonoBehaviour
{
    private const string FireballPrefabPath = "FX/PF_Hero_Fireball";
    [SerializeField, Min(0.01f)] private float projectileSpeed = 4f;
    private sealed class Shot
    {
        public GameObject View;
        public Vector2Int Cell, Direction;
        public int Damage;
        public float Progress;
    }
    private GridManager grid;
    private MonsterManager monsters;
    private CombatSystem combat;
    private GameObject fireballPrefab;
    private Transform projectileRoot;
    private readonly List<Shot> shots = new List<Shot>();
    private readonly List<MonsterData> cellTargets = new List<MonsterData>();
    public void Initialize(GridManager gridManager, MonsterManager monsterManager, CombatSystem combatSystem)
    {
        grid = gridManager; monsters = monsterManager; combat = combatSystem;
        fireballPrefab = Resources.Load<GameObject>(FireballPrefabPath);
        if (projectileRoot != null) return;
        projectileRoot = new GameObject("HeroProjectiles").transform;
    }
    public MonsterData FindTarget(Vector2Int origin, Vector2Int direction, float range)
    {
        direction = Cardinal(direction);
        if (grid == null || monsters == null || direction == Vector2Int.zero) return null;
        int steps = Mathf.Max(0, Mathf.FloorToInt(range));
        for (int i = 1; i <= steps; i++)
        {
            Vector2Int cell = origin + direction * i;
            if (!grid.IsInside(cell.x, cell.y) || !grid.IsWalkable(cell.x, cell.y)) return null;
            MonsterData target = FindFirstAlive(cell);
            if (target != null) return target;
        }
        return null;
    }
    public bool Launch(Vector2Int origin, Vector2Int direction, int damage)
    {
        direction = Cardinal(direction);
        if (grid == null || monsters == null || combat == null || direction == Vector2Int.zero || !grid.IsInside(origin.x, origin.y)) return false;
        GameObject view = fireballPrefab != null
            ? Instantiate(fireballPrefab, projectileRoot)
            : new GameObject("Fireball_LogicOnly");
        view.transform.SetParent(projectileRoot, true);
        view.transform.position = CellWorld(origin);
        view.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        shots.Add(new Shot { View = view, Cell = origin, Direction = direction, Damage = Mathf.Max(0, damage) });
        return true;
    }
    private void Update()
    {
        float distance = projectileSpeed * Time.deltaTime;
        for (int i = shots.Count - 1; i >= 0; i--)
            if (Advance(shots[i], distance)) RemoveAt(i);
    }
    private bool Advance(Shot shot, float distance)
    {
        while (distance > 0f)
        {
            Vector2Int next = shot.Cell + shot.Direction;
            bool blocked = !grid.IsInside(next.x, next.y) || !grid.IsWalkable(next.x, next.y);
            float segmentEnd = blocked ? 0.5f : 1f;
            float step = Mathf.Min(distance, segmentEnd - shot.Progress);
            shot.Progress += step;
            distance -= step;
            if (shot.View != null) shot.View.transform.position = CellWorld(shot.Cell) + (Vector3)(Vector2)shot.Direction * shot.Progress;
            if (shot.Progress + 0.0001f < segmentEnd) return false;
            if (blocked) return true;
            shot.Cell = next;
            shot.Progress = 0f;
            MonsterData hit = FindFirstAlive(next);
            if (hit != null && combat.ApplyRangedProjectileHit(shot.Damage, hit, shot.Direction)) return true;
        }
        return false;
    }
    private MonsterData FindFirstAlive(Vector2Int cell)
    {
        monsters.CollectInRect(new RectInt(cell.x, cell.y, 1, 1), cellTargets);
        for (int i = 0; i < cellTargets.Count; i++)
            if (cellTargets[i] != null && cellTargets[i].IsAlive()) return cellTargets[i];
        return null;
    }
    private void RemoveAt(int index)
    {
        if (shots[index].View != null) Destroy(shots[index].View);
        shots.RemoveAt(index);
    }
    private static Vector2Int Cardinal(Vector2Int direction)
    {
        if (direction == Vector2Int.zero) return Vector2Int.zero;
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) return direction.x >= 0 ? Vector2Int.right : Vector2Int.left;
        return direction.y >= 0 ? Vector2Int.up : Vector2Int.down;
    }
    private static Vector3 CellWorld(Vector2Int cell) => new Vector3(cell.x + 0.5f, cell.y + 0.5f, -0.25f);
}
