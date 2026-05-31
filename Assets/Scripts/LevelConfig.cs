using UnityEngine;

public class LevelConfig : MonoBehaviour
{
    private const int AutoCenterColumn = -1;

    [Header("Grid Size")]
    [SerializeField] private int width = 70;
    [SerializeField] private int height = 50;

    [Header("Entrance Room")]
    [SerializeField] private int entranceColumn = AutoCenterColumn;
    [SerializeField] private int entranceRowsBelowSurface = 1;
    [SerializeField] private int openCellsBelowEntrance = 3;
    [SerializeField] private int demonLordCellsBelowEntrance = 3;

    [Header("Surface Region")]
    [SerializeField] private int surfaceBackgroundRows = 10;

    [Header("Hero Flow")]
    [SerializeField] private float heroSpawnDelaySeconds = 10f;

    [Header("Camera View")]
    [SerializeField] private float cameraViewColumns = 30f;
    [SerializeField] private float cameraViewRows = 16f;

    [Header("Test Tile Attributes")]
    [SerializeField] private Vector2Int[] testSlimeAttributePositions =
    {
        new Vector2Int(24, 9),
        new Vector2Int(28, 9),
        new Vector2Int(32, 9),
        new Vector2Int(36, 9),
    };

    public int Width => width;
    public int Height => height;
    public int SurfaceBackgroundRows => Mathf.Clamp(surfaceBackgroundRows, 0, height);
    public int UndergroundSurfaceY => Mathf.Clamp(height - SurfaceBackgroundRows - 1, 0, height - 1);
    public Vector2Int EntrancePosition => new Vector2Int(ResolveEntranceColumn(), ResolveEntranceY());
    public Vector2Int DemonLordStartPosition => new Vector2Int(
        ResolveEntranceColumn(),
        Mathf.Clamp(ResolveEntranceY() - demonLordCellsBelowEntrance, 0, height - 1));
    public float HeroSpawnDelaySeconds => Mathf.Max(0f, heroSpawnDelaySeconds);
    public float CameraViewColumns => Mathf.Max(1f, cameraViewColumns);
    public float CameraViewRows => Mathf.Max(1f, cameraViewRows);
    public Vector2 CameraStartCenter => new Vector2(width * 0.5f, height * 0.5f);

    public void ApplyInitialGrid(GridData gridData)
    {
        if (gridData == null) return;

        Vector2Int entrance = EntrancePosition;
        Vector2Int demonLord = DemonLordStartPosition;

        int surfaceY = UndergroundSurfaceY;
        for (int x = 0; x < gridData.Width; x++)
            for (int y = Mathf.Max(surfaceY + 1, 0); y < gridData.Height; y++)
                gridData.SetCell(x, y, CellType.Empty);

        gridData.SetCell(entrance.x, entrance.y, CellType.Entrance);

        int shaftDepth = Mathf.Max(openCellsBelowEntrance, demonLordCellsBelowEntrance);
        for (int i = 1; i <= shaftDepth; i++)
        {
            int y = entrance.y - i;
            if (gridData.IsInside(entrance.x, y))
                gridData.SetCell(entrance.x, y, CellType.Empty);
        }

        gridData.SetCell(demonLord.x, demonLord.y, CellType.Empty);

        foreach (Vector2Int pos in testSlimeAttributePositions)
        {
            if (gridData.IsInside(pos.x, pos.y) && gridData.GetCell(pos.x, pos.y) == CellType.Soil)
                gridData.SetTileAttribute(pos.x, pos.y, new TileAttributeData(3, 0, TileElementType.Slime));
        }
    }

    public bool IsSurfaceLayer(int y)
    {
        return y == UndergroundSurfaceY;
    }

    public bool IsSurfaceBackgroundRow(int y)
    {
        return y > UndergroundSurfaceY && y < height;
    }

    private int ResolveEntranceColumn()
    {
        int resolved = entranceColumn == AutoCenterColumn ? width / 2 : entranceColumn;
        return Mathf.Clamp(resolved, 0, width - 1);
    }

    private int ResolveEntranceY()
    {
        return Mathf.Clamp(
            UndergroundSurfaceY - Mathf.Max(1, entranceRowsBelowSurface),
            0,
            Mathf.Max(0, UndergroundSurfaceY - 1));
    }
}
