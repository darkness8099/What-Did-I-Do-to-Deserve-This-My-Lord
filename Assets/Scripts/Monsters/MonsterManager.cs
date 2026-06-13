using System.Collections.Generic;
using UnityEngine;

// Per-monster storage (each MonsterData carries its own Position). Multiple monsters MAY share a cell:
// monsters have no collision volume and can interpenetrate (fix #1); flower offspring all spawn in one cell (fix #4).
public class MonsterManager : MonoBehaviour
{
    private static readonly Vector2Int[] Dirs4 =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0),
    };

    private GridManager gridManager;
    private readonly List<MonsterData> monsters = new List<MonsterData>();

    private void Start()
    {
        gridManager = GetComponent<GridManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
            Debug.LogError("[MonsterManager] GridManager not found in scene.");
        Debug.Log("[MonsterManager] Initialized.");
    }

    // Spawn a monster at (x,y) with a random initial facing (fix #5). Stacking allowed.
    public MonsterData Spawn(int x, int y, MonsterArchetype archetype)
    {
        if (archetype == null)
        {
            Debug.LogWarning("[MonsterManager] Spawn: archetype is null. Skipped.");
            return null;
        }
        var data = new MonsterData(archetype);
        data.SetPosition(new Vector2Int(x, y));
        data.SetMoveDirection(Dirs4[Random.Range(0, Dirs4.Length)]);
        monsters.Add(data);
        return data;
    }

    public bool PlaceMonster(int x, int y, MonsterArchetype archetype) => Spawn(x, y, archetype) != null;
    public bool PlaceSlime(int x, int y) => Spawn(x, y, MonsterArchetype.Slime) != null;

    public bool HasMonster(int x, int y)
    {
        for (int i = 0; i < monsters.Count; i++)
            if (monsters[i].Position.x == x && monsters[i].Position.y == y) return true;
        return false;
    }

    // First monster occupying (x,y) (used by combat / hero targeting). Null if none.
    public MonsterData GetMonster(int x, int y)
    {
        for (int i = 0; i < monsters.Count; i++)
            if (monsters[i].Position.x == x && monsters[i].Position.y == y) return monsters[i];
        return null;
    }

    // True if any monster at (x,y) is a Bud or Flower (only one plant allowed per cell).
    public bool HasBudOrFlowerAt(int x, int y)
    {
        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterData m = monsters[i];
            if (m.Position.x != x || m.Position.y != y) continue;
            if (m.Stage == SlimeLifecycleStage.Bud || m.Stage == SlimeLifecycleStage.Flower) return true;
        }
        return false;
    }

    public void Remove(MonsterData m)
    {
        if (m != null) monsters.Remove(m);
    }

    public bool RemoveMonster(int x, int y)
    {
        MonsterData m = GetMonster(x, y);
        if (m == null) return false;
        monsters.Remove(m);
        return true;
    }

    // Snapshot all monsters into `buffer` (cleared first) so callers can iterate while the list mutates.
    public void CollectAll(List<MonsterData> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        buffer.AddRange(monsters);
    }

    public int Count => monsters.Count;

    public Vector2Int? FindNearestMonsterInRange(Vector2Int heroPos, float range)
    {
        Vector2Int? nearest = null;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < monsters.Count; i++)
        {
            Vector2Int pos = monsters[i].Position;
            float dist = Mathf.Abs(pos.x - heroPos.x) + Mathf.Abs(pos.y - heroPos.y);
            if (dist <= range && dist < nearestDist) { nearest = pos; nearestDist = dist; }
        }
        return nearest;
    }
}
