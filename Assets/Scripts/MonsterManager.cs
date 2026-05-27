using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    private GridManager gridManager;
    private Dictionary<Vector2Int, MonsterData> monsters;

    private void Start()
    {
        gridManager = GetComponent<GridManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("[MonsterManager] GridManager not found in scene.");
            return;
        }

        monsters = new Dictionary<Vector2Int, MonsterData>();
        Debug.Log("[MonsterManager] Initialized.");
    }

    public bool CanPlaceMonster(int x, int y)
    {
        if (!gridManager.GetGridData().IsInside(x, y))
        {
            Debug.LogWarning($"[MonsterManager] CanPlaceMonster: ({x},{y}) is out of bounds.");
            return false;
        }

        if (gridManager.GetCellType(x, y) != CellType.Empty)
            return false;

        if (HasMonster(x, y))
            return false;

        return true;
    }

    public bool PlaceSlime(int x, int y)
    {
        if (!CanPlaceMonster(x, y))
            return false;

        monsters[new Vector2Int(x, y)] = new MonsterData(MonsterType.Slime);
        Debug.Log($"[MonsterManager] Slime placed at ({x},{y}).");
        return true;
    }

    public bool HasMonster(int x, int y)
    {
        return monsters != null && monsters.ContainsKey(new Vector2Int(x, y));
    }

    public MonsterData GetMonster(int x, int y)
    {
        var key = new Vector2Int(x, y);
        if (monsters != null && monsters.TryGetValue(key, out var data))
            return data;
        return null;
    }

    public bool RemoveMonster(int x, int y)
    {
        var key = new Vector2Int(x, y);
        return monsters != null && monsters.Remove(key);
    }
}
