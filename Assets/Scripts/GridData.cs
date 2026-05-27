using UnityEngine;

public enum CellType
{
    Soil,
    Empty,
    Entrance,
    DemonLordRoom
}

public class GridData
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private CellType[,] cells;

    public GridData(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new CellType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells[x, y] = CellType.Soil;
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public CellType GetCell(int x, int y)
    {
        if (!IsInside(x, y))
        {
            Debug.LogWarning($"GridData.GetCell: ({x}, {y}) is out of bounds (Width={Width}, Height={Height}).");
            return CellType.Soil;
        }
        return cells[x, y];
    }

    public void SetCell(int x, int y, CellType type)
    {
        if (!IsInside(x, y))
        {
            Debug.LogWarning($"GridData.SetCell: ({x}, {y}) is out of bounds (Width={Width}, Height={Height}). Ignored.");
            return;
        }
        cells[x, y] = type;
    }
}
