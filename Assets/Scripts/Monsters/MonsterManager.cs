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
    private readonly Dictionary<Vector2Int, List<MonsterData>> monstersByCell = new Dictionary<Vector2Int, List<MonsterData>>();
    private float maxAttackRange;

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
        AddToSpatialIndex(data);
        maxAttackRange = Mathf.Max(maxAttackRange, Mathf.Max(0f, data.AttackRange));
        return data;
    }

    public bool PlaceMonster(int x, int y, MonsterArchetype archetype) => Spawn(x, y, archetype) != null;
    public bool PlaceSlime(int x, int y) => Spawn(x, y, MonsterArchetype.Slime) != null;

public bool HasMonster(int x, int y)
    {
        List<MonsterData> cell;
        return monstersByCell.TryGetValue(new Vector2Int(x, y), out cell) && cell.Count > 0;
    }

    // First monster occupying (x,y) (used by combat / hero targeting). Null if none.
public MonsterData GetMonster(int x, int y)
    {
        List<MonsterData> cell;
        if (!monstersByCell.TryGetValue(new Vector2Int(x, y), out cell) || cell.Count == 0)
            return null;
        return cell[0];
    }

    // True if any monster at (x,y) is a Bud or Flower (only one plant allowed per cell).
public bool HasBudOrFlowerAt(int x, int y)
    {
        List<MonsterData> cell;
        if (!monstersByCell.TryGetValue(new Vector2Int(x, y), out cell))
            return false;

        for (int i = 0; i < cell.Count; i++)
        {
            SlimeLifecycleStage stage = cell[i].Stage;
            if (stage == SlimeLifecycleStage.Bud || stage == SlimeLifecycleStage.Flower)
                return true;
        }
        return false;
    }

public void Remove(MonsterData m)
    {
        if (m == null) return;
        if (!monsters.Remove(m)) return;
        RemoveFromSpatialIndex(m, m.Position);
    }

public int RemoveMany(IList<MonsterData> items)
    {
        if (items == null || items.Count == 0) return 0;

        var removeSet = new HashSet<MonsterData>();
        for (int i = 0; i < items.Count; i++)
        {
            MonsterData monster = items[i];
            if (monster == null || !removeSet.Add(monster)) continue;
            RemoveFromSpatialIndex(monster, monster.Position);
        }

        return monsters.RemoveAll(monster => removeSet.Contains(monster));
    }


public bool RemoveMonster(int x, int y)
    {
        MonsterData m = GetMonster(x, y);
        if (m == null) return false;
        Remove(m);
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

public bool Contains(MonsterData monster)
    {
        if (monster == null) return false;
        List<MonsterData> cell;
        return monstersByCell.TryGetValue(monster.Position, out cell) && cell.Contains(monster);
    }

public MonsterData FindNearestMonsterTargetInRange(Vector2Int heroPos, float range)
    {
        MonsterData nearest = null;
        float nearestDist = float.MaxValue;
        int radius = Mathf.Max(0, Mathf.CeilToInt(range));

        for (int x = heroPos.x - radius; x <= heroPos.x + radius; x++)
        {
            for (int y = heroPos.y - radius; y <= heroPos.y + radius; y++)
            {
                List<MonsterData> cell;
                if (!monstersByCell.TryGetValue(new Vector2Int(x, y), out cell) || cell.Count == 0)
                    continue;

                float dist = Mathf.Abs(x - heroPos.x) + Mathf.Abs(y - heroPos.y);
                if (dist > range || dist >= nearestDist) continue;

                for (int i = 0; i < cell.Count; i++)
                {
                    MonsterData candidate = cell[i];
                    if (candidate == null || !candidate.IsAlive()) continue;
                    nearest = candidate;
                    nearestDist = dist;
                    break;
                }
            }
        }
        return nearest;
    }

public Vector2Int? FindNearestMonsterInRange(Vector2Int heroPos, float range)
    {
        MonsterData target = FindNearestMonsterTargetInRange(heroPos, range);
        return target != null ? (Vector2Int?)target.Position : null;
    }

public void CollectCombatAttackers(Vector2Int heroPos, List<MonsterData> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();

        int radius = Mathf.Max(0, Mathf.CeilToInt(maxAttackRange));
        for (int x = heroPos.x - radius; x <= heroPos.x + radius; x++)
        {
            for (int y = heroPos.y - radius; y <= heroPos.y + radius; y++)
            {
                List<MonsterData> cell;
                if (!monstersByCell.TryGetValue(new Vector2Int(x, y), out cell)) continue;

                float distance = Mathf.Abs(x - heroPos.x) + Mathf.Abs(y - heroPos.y);
                for (int i = 0; i < cell.Count; i++)
                {
                    MonsterData monster = cell[i];
                    if (monster == null || !monster.IsAlive()) continue;
                    if (monster.Stage != SlimeLifecycleStage.Crawling) continue;
                    if (monster.IsSpawnDelayed()) continue;
                    if (distance <= monster.AttackRange) buffer.Add(monster);
                }
            }
        }
    }


public bool Move(MonsterData monster, Vector2Int destination)
    {
        if (monster == null) return false;
        Vector2Int previous = monster.Position;

        List<MonsterData> currentCell;
        if (!monstersByCell.TryGetValue(previous, out currentCell) || !currentCell.Contains(monster))
            return false;
        if (previous == destination) return true;

        RemoveFromSpatialIndex(monster, previous);
        monster.SetPosition(destination);
        AddToSpatialIndex(monster);
        return true;
    }

    private void AddToSpatialIndex(MonsterData monster)
    {
        List<MonsterData> cell;
        if (!monstersByCell.TryGetValue(monster.Position, out cell))
        {
            cell = new List<MonsterData>();
            monstersByCell[monster.Position] = cell;
        }
        cell.Add(monster);
    }

    private void RemoveFromSpatialIndex(MonsterData monster, Vector2Int position)
    {
        List<MonsterData> cell;
        if (!monstersByCell.TryGetValue(position, out cell)) return;
        cell.Remove(monster);
        if (cell.Count == 0) monstersByCell.Remove(position);
    }

    public bool ValidateSpatialIndex()
    {
        int indexed = 0;
        foreach (KeyValuePair<Vector2Int, List<MonsterData>> pair in monstersByCell)
        {
            List<MonsterData> cell = pair.Value;
            indexed += cell.Count;
            for (int i = 0; i < cell.Count; i++)
            {
                MonsterData monster = cell[i];
                if (monster == null || monster.Position != pair.Key || !monsters.Contains(monster))
                    return false;
            }
        }
        return indexed == monsters.Count;
    }

    public void CollectInRect(RectInt rect, List<MonsterData> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                List<MonsterData> cell;
                if (monstersByCell.TryGetValue(new Vector2Int(x, y), out cell))
                    buffer.AddRange(cell);
            }
        }
    }
}
