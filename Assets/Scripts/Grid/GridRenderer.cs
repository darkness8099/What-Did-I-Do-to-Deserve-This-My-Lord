using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    [SerializeField] private Sprite[] soilBrownSprites;
    [SerializeField] private Sprite[] soilDarkBlueSprites;
    [SerializeField] private Sprite[] soilDarkGreenSprites;
    [SerializeField] private Sprite[] soilDarkPurpleRedSprites;

    private static readonly Color ColorEmpty         = new Color(0.10f, 0.10f, 0.10f);
    private static readonly Color ColorSoilFallback  = new Color(0.55f, 0.35f, 0.15f);

    private GridManager    gridManager;
    private Sprite         _whitePlaceholder;
    private GameObject[,]  tileObjects;
    private GameObject     gridTilesParent;

private void OnEnable()
    {
        TryBindGridManager();
        if (gridManager != null)
            gridManager.TileAttributeChanged += RefreshCell;
    }

    private void OnDisable()
    {
        if (gridManager != null)
            gridManager.TileAttributeChanged -= RefreshCell;
    }

private void Start()
    {
        RebuildGrid();
    }

    // Public entry usable from Editor tools.
    public void RebuildGrid()
    {
        TryBindGridManager();
        if (gridManager == null)
        {
            Debug.LogError("[GridRenderer] GridManager not found in scene.");
            return;
        }

        if (!gridManager.IsInitialized)
        {
            Debug.LogWarning("[GridRenderer] GridManager not initialized; cannot render.");
            return;
        }

        EnsureWhitePlaceholder();
        ClearGeneratedGrid();
        RenderGrid();
    }

    // Editor button: initializes grid data + renders. Mirrors BackgroundLayerRenderer.GenerateDraftInEditor.
    public void GenerateGridInEditor()
    {
        TryBindGridManager();
        if (gridManager == null)
        {
            Debug.LogError("[GridRenderer] GridManager not found in scene.");
            return;
        }

        gridManager.InitializeGrid();
        RebuildGrid();
        MarkDirty();
    }

    // Editor button: destroys all rendered tiles + frees the parent GameObject.
    public void ClearGeneratedGrid()
    {
        if (gridTilesParent != null)
        {
            DestroyObjectSafe(gridTilesParent);
            gridTilesParent = null;
        }
        // Also clean up any orphaned GridTiles at scene root (e.g. from older edit-mode preview).
        var orphan = GameObject.Find("GridTiles");
        if (orphan != null) DestroyObjectSafe(orphan);

        tileObjects = null;
        MarkDirty();
    }

    private void EnsureWhitePlaceholder()
    {
        if (_whitePlaceholder != null) return;
        _whitePlaceholder = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            4f);
    }

    private static void DestroyObjectSafe(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    private void TryBindGridManager()
    {
        if (gridManager != null) return;

        gridManager = GetComponent<GridManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
    }





private void RenderGrid()
    {
        GridData data = gridManager.GetGridData();

        gridTilesParent = new GameObject("GridTiles");
        var parent = gridTilesParent;
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
        if (type == CellType.Empty && gridManager.IsSurfaceBackgroundRow(y))
        {
            sr.sprite = null;
            sr.color = Color.white;
            return;
        }

        switch (type)
        {
            case CellType.Soil:
                Sprite soilSprite = ResolveSoilSprite(x, y);
                sr.sprite = soilSprite != null ? soilSprite : _whitePlaceholder;
                sr.color  = soilSprite != null ? Color.white : ColorSoilFallback;
                break;
            case CellType.Empty:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorEmpty;
                break;
            case CellType.Entrance:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorEmpty;
                break;
            default:
                sr.sprite = _whitePlaceholder;
                sr.color  = ColorSoilFallback;
                break;
        }
    }

    private Sprite ResolveSoilSprite(int x, int y)
    {
        int rowFromTop = GetRowFromTop(y);
        int undergroundRowFromTop = rowFromTop - 10;
        if (undergroundRowFromTop <= 0)
            return GetFallbackSoilSprite();

        int bandIndex = GetSoilColorBandIndex(undergroundRowFromTop);
        Sprite[] bandSprites = GetSoilPaletteForBand(bandIndex);
        if (bandSprites == null || bandSprites.Length == 0)
            return GetFallbackSoilSprite();

        TileAttributeData attribute = gridManager.GetTileAttribute(x, y);
        int spriteIndex = Mathf.Clamp(attribute.GetNutrientVisualIndex(), 0, bandSprites.Length - 1);
        return bandSprites[spriteIndex];
    }

    private int GetSoilColorBandIndex(int undergroundRowFromTop)
    {
        if (undergroundRowFromTop <= 8) return 0;
        if (undergroundRowFromTop <= 16) return 1;
        if (undergroundRowFromTop <= 24) return 2;
        return 3;
    }

    private int GetRowFromTop(int y)
    {
        GridData data = gridManager.GetGridData();
        return data.Height - y;
    }

    private Sprite[] GetSoilPaletteForBand(int bandIndex)
    {
        switch (bandIndex % 4)
        {
            case 0: return soilBrownSprites;
            case 1: return soilDarkBlueSprites;
            case 2: return soilDarkGreenSprites;
            case 3: return soilDarkPurpleRedSprites;
            default: return soilBrownSprites;
        }
    }

    private Sprite GetFallbackSoilSprite()
    {
        Sprite[] brownSprites = soilBrownSprites;
        if (brownSprites != null && brownSprites.Length > 0)
            return brownSprites[0];

        return null;
    }
}
