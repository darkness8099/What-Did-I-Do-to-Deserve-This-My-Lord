using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    [SerializeField] private Sprite spriteSoilSurface;
    [SerializeField] private Sprite spriteSoilDeep;

    private static readonly Color ColorEmpty         = new Color(0.10f, 0.10f, 0.10f);
    private static readonly Color ColorEntrance      = new Color(0.20f, 0.80f, 0.20f);
    private static readonly Color ColorDemonLordRoom = new Color(0.85f, 0.15f, 0.15f);
    private static readonly Color ColorSoilFallback  = new Color(0.55f, 0.35f, 0.15f);

    private GridManager    gridManager;
    private Sprite         _whitePlaceholder;
    private GameObject[,]  tileObjects;

private void Start()
    {
        gridManager = GetComponent<GridManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("[GridRenderer] GridManager not found in scene.");
            return;
        }

        _whitePlaceholder = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            4f);

        RenderGrid();
    }





private void RenderGrid()
    {
        GridData data = gridManager.GetGridData();

        var parent = new GameObject("GridTiles");
        tileObjects = new GameObject[data.Width, data.Height];

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                var go = new GameObject($"Tile_{x}_{y}");
                go.transform.SetParent(parent.transform, false);
                go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -10;

                ApplyCellVisual(sr, x, y, data.GetCell(x, y));

                tileObjects[x, y] = go;
            }
        }

        Debug.Log($"[GridRenderer] Grid rendered: {data.Width * data.Height} tiles ({data.Width}x{data.Height}).");
    }

public void RefreshCell(int x, int y)
    {
        if (tileObjects == null) return;
        if (x < 0 || x >= tileObjects.GetLength(0) || y < 0 || y >= tileObjects.GetLength(1))
        {
            Debug.LogWarning($"[GridRenderer] RefreshCell: ({x}, {y}) out of range.");
            return;
        }
        var go = tileObjects[x, y];
        if (go == null) return;
        ApplyCellVisual(go.GetComponent<SpriteRenderer>(), x, y, gridManager.GetCellType(x, y));
    }




private void ApplyCellVisual(SpriteRenderer sr, int x, int y, CellType type)
    {
        switch (type)
        {
            case CellType.Soil:
                bool useDeep = y < gridManager.GetGridData().Height / 2;
                Sprite soilSprite = useDeep ? spriteSoilDeep : spriteSoilSurface;
                sr.sprite = soilSprite != null ? soilSprite : _whitePlaceholder;
                sr.color  = soilSprite != null ? Color.white : ColorSoilFallback;
                break;
            case CellType.Empty:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorEmpty;
                break;
            case CellType.Entrance:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorEntrance;
                break;
            case CellType.DemonLordRoom:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorDemonLordRoom;
                break;
            default:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorSoilFallback;
                break;
        }
    }
}
