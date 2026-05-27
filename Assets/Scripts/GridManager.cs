using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 32;
    [SerializeField] private int height = 18;

    private GridData gridData;

    private static readonly Vector2Int EntrancePos    = new Vector2Int(0, 9);
    private static readonly Vector2Int DemonLordRoomPos = new Vector2Int(31, 9);

    private void Awake()
    {
        gridData = new GridData(width, height);

        gridData.SetCell(EntrancePos.x,     EntrancePos.y,     CellType.Entrance);
        gridData.SetCell(DemonLordRoomPos.x, DemonLordRoomPos.y, CellType.DemonLordRoom);

        Debug.Log($"[GridManager] Grid initialized: {width}x{height}");
        Debug.Log($"[GridManager] Entrance position: ({EntrancePos.x}, {EntrancePos.y})");
        Debug.Log($"[GridManager] DemonLordRoom position: ({DemonLordRoomPos.x}, {DemonLordRoomPos.y})");
    }

    public bool DigCell(int x, int y)
    {
        if (!gridData.IsInside(x, y))
        {
            Debug.LogWarning($"[GridManager] DigCell: ({x}, {y}) is out of bounds. Ignored.");
            return false;
        }

        CellType current = gridData.GetCell(x, y);

        if (current != CellType.Soil)
            return false;

        gridData.SetCell(x, y, CellType.Empty);
        return true;
    }

    public CellType GetCellType(int x, int y)
    {
        return gridData.GetCell(x, y);
    }

    public GridData GetGridData()
    {
        return gridData;
    }
}
