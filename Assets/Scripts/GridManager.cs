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

        gridData.SetCell(EntrancePos.x,      EntrancePos.y,      CellType.Entrance);
        gridData.SetCell(DemonLordRoomPos.x,  DemonLordRoomPos.y, CellType.DemonLordRoom);

        // MVP 临时测试配置：预设带属性土块，用于验证挖掘自动生成逻辑。
        // 后续将替换为正式地图配置或资源数据。
        int[] testX = { 6, 10, 14, 18, 22 };
        foreach (int tx in testX)
            gridData.SetTileAttribute(tx, 9, new TileAttributeData(1, TileElementType.Slime));

        Debug.Log($"[GridManager] Grid initialized: {width}x{height}");
        Debug.Log($"[GridManager] Entrance: ({EntrancePos.x},{EntrancePos.y}), DemonLordRoom: ({DemonLordRoomPos.x},{DemonLordRoomPos.y})");
        Debug.Log("[GridManager] Test Slime attributes set at y=9, x=6/10/14/18/22.");
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

    public TileAttributeData GetTileAttribute(int x, int y)
    {
        return gridData.GetTileAttribute(x, y);
    }

    public void SetTileAttribute(int x, int y, TileAttributeData attribute)
    {
        gridData.SetTileAttribute(x, y, attribute);
    }
}
