using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    private static readonly Vector2Int[] CardinalDirections =
    {
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int(-1,  0),
        new Vector2Int( 1,  0),
    };

    private LevelConfig levelConfig;
    private GridData gridData;

    public event System.Action<int, int> TileAttributeChanged;

    private void Awake()
    {
        InitializeGrid();
    }

    public bool IsInitialized => gridData != null;

    // Public so Edit Mode tools can rebuild grid data without entering Play Mode.
    public void InitializeGrid()
    {
        levelConfig = GetComponent<LevelConfig>() ?? FindObjectOfType<LevelConfig>();
        if (levelConfig == null)
        {
            Debug.LogError("[GridManager] LevelConfig not found in scene.");
            return;
        }

        gridData = new GridData(levelConfig.Width, levelConfig.Height);
        levelConfig.ApplyInitialGrid(gridData);

        Vector2Int entrance = levelConfig.EntrancePosition;
        Debug.Log($"[GridManager] Grid initialized: {levelConfig.Width}x{levelConfig.Height}");
        Debug.Log($"[GridManager] Entrance: ({entrance.x},{entrance.y}).");
        Vector2Int demonLord = levelConfig.DemonLordStartPosition;
        Debug.Log($"[GridManager] DemonLord start: ({demonLord.x},{demonLord.y}).");
    }

    public bool DigCell(int x, int y)
    {
        if (!IsDiggable(x, y))
        {
            Debug.Log($"[GridManager] DigCell: ({x}, {y}) is not diggable.");
            return false;
        }

        gridData.SetCell(x, y, CellType.Empty);
        return true;
    }

    public CellType GetCellType(int x, int y)
    {
        return gridData.GetCell(x, y);
    }

    public bool IsInside(int x, int y)
    {
        return gridData.IsInside(x, y);
    }

    public bool IsWalkable(int x, int y)
    {
        CellType type = gridData.GetCell(x, y);
        return type == CellType.Empty || type == CellType.Entrance;
    }

    // Where monsters may move into / spawn on: an Empty cell inside the underground area only.
    // Excludes the surface/sky rows and the Entrance, so monsters can't climb out of the dungeon.
    public bool IsMonsterTraversable(int x, int y)
    {
        return IsInside(x, y)
            && gridData.GetCell(x, y) == CellType.Empty
            && !IsSurfaceBackgroundRow(y);
    }

    public bool IsDiggable(int x, int y)
    {
        if (!gridData.IsInside(x, y)) return false;
        if (gridData.GetCell(x, y) != CellType.Soil) return false;

        foreach (Vector2Int direction in CardinalDirections)
        {
            int nx = x + direction.x;
            int ny = y + direction.y;
            if (gridData.IsInside(nx, ny) && IsWalkable(nx, ny))
                return true;
        }

        return false;
    }

    public GridData GetGridData()
    {
        return gridData;
    }

    public Vector2Int GetEntrancePosition()
    {
        return levelConfig.EntrancePosition;
    }

    public bool IsSurfaceLayer(int y)
    {
        return levelConfig != null && levelConfig.IsSurfaceLayer(y);
    }

    public bool IsSurfaceBackgroundRow(int y)
    {
        return levelConfig != null && levelConfig.IsSurfaceBackgroundRow(y);
    }

    public TileAttributeData GetTileAttribute(int x, int y)
    {
        if (!gridData.IsInside(x, y)) return TileAttributeData.Default;
        if (gridData.GetCell(x, y) != CellType.Soil) return TileAttributeData.Default;
        return gridData.GetTileAttribute(x, y);
    }

    public void SetTileAttribute(int x, int y, TileAttributeData attribute)
    {
        if (!gridData.IsInside(x, y)) return;
        if (gridData.GetCell(x, y) != CellType.Soil)
        {
            Debug.LogWarning($"[GridManager] SetTileAttribute on non-Soil cell ({x},{y}) ignored.");
            return;
        }
        gridData.SetTileAttribute(x, y, attribute);
        TileAttributeChanged?.Invoke(x, y);
    }

    // ===== 4-cardinal neighbor queries (TASK-061) =====
    // v1 ecology budget: each ecology check visits at most these 4 cells; no per-frame / whole-grid scan.

    // Non-allocating: fills `buffer` (length >= 4) with in-bounds up/down/left/right neighbor coords; returns count.
    // Pass a reusable Vector2Int[4] to avoid GC when called per move / per ecology tick.
    public int GetNeighborCells4(int x, int y, Vector2Int[] buffer)
    {
        if (buffer == null || buffer.Length < 4)
        {
            Debug.LogWarning("[GridManager] GetNeighborCells4 requires a buffer of length >= 4.");
            return 0;
        }

        int count = 0;
        foreach (Vector2Int dir in CardinalDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsInside(nx, ny)) buffer[count++] = new Vector2Int(nx, ny);
        }
        return count;
    }

    // Convenience: clears and fills `results` with in-bounds 4-neighbors; returns true if any.
    public bool TryGetNeighborCells4(int x, int y, List<Vector2Int> results)
    {
        if (results == null) return false;
        results.Clear();
        foreach (Vector2Int dir in CardinalDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsInside(nx, ny)) results.Add(new Vector2Int(nx, ny));
        }
        return results.Count > 0;
    }

    // Derived helper (no stored bool): true only if (x,y) is Soil and holds Nutrient.
    public bool HasAbsorbableNutrient(int x, int y)
    {
        return gridData != null && gridData.HasAbsorbableNutrient(x, y);
    }
}
