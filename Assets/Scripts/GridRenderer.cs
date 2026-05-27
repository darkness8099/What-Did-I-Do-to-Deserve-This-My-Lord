using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    private GridManager gridManager;

    private Material matSoil;
    private Material matEmpty;
    private Material matEntrance;
    private Material matDemonLordRoom;

    private GameObject[,] tileObjects;

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

        CreateMaterials();
        RenderGrid();
    }

    private void CreateMaterials()
    {
        matSoil          = MakeMat(new Color(0.55f, 0.35f, 0.15f));
        matEmpty         = MakeMat(new Color(0.15f, 0.15f, 0.15f));
        matEntrance      = MakeMat(new Color(0.20f, 0.80f, 0.20f));
        matDemonLordRoom = MakeMat(new Color(0.85f, 0.15f, 0.15f));
    }

    private static Material MakeMat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
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
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Tile_{x}_{y}";
                quad.transform.SetParent(parent.transform, false);
                quad.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                quad.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

                Destroy(quad.GetComponent<MeshCollider>());

                quad.GetComponent<MeshRenderer>().sharedMaterial =
                    CellToMaterial(data.GetCell(x, y));

                tileObjects[x, y] = quad;
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
        go.GetComponent<MeshRenderer>().sharedMaterial =
            CellToMaterial(gridManager.GetCellType(x, y));
    }

    private Material CellToMaterial(CellType type) => type switch
    {
        CellType.Empty         => matEmpty,
        CellType.Entrance      => matEntrance,
        CellType.DemonLordRoom => matDemonLordRoom,
        _                      => matSoil,
    };
}
