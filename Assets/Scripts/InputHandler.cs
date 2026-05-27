using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridRenderer gridRenderer;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private MonsterRenderer monsterRenderer;

    private void Start()
    {
        if (mainCamera == null)      mainCamera      = Camera.main;
        if (gridManager == null)     gridManager     = FindObjectOfType<GridManager>();
        if (gridRenderer == null)    gridRenderer    = FindObjectOfType<GridRenderer>();
        if (monsterManager == null)  monsterManager  = FindObjectOfType<MonsterManager>();
        if (monsterRenderer == null) monsterRenderer = FindObjectOfType<MonsterRenderer>();

        if (mainCamera == null)
            Debug.LogError("[InputHandler] Main Camera not found.");
        if (gridManager == null)
            Debug.LogError("[InputHandler] GridManager not found.");
        if (gridRenderer == null)
            Debug.LogError("[InputHandler] GridRenderer not found.");
        if (monsterManager == null)
            Debug.LogError("[InputHandler] MonsterManager not found.");
        if (monsterRenderer == null)
            Debug.LogError("[InputHandler] MonsterRenderer not found.");
    }

private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);

        if (!gridManager.GetGridData().IsInside(x, y))
        {
            Debug.Log($"[InputHandler] Click outside map: ({x},{y})");
            return;
        }

        CellType cell = gridManager.GetCellType(x, y);

        if (cell == CellType.Soil)
        {
            TileAttributeData attr = gridManager.GetTileAttribute(x, y);

            if (gridManager.DigCell(x, y))
            {
                gridRenderer.RefreshCell(x, y);

                if (attr.CanSpawnMonster() && attr.ElementType == TileElementType.Slime)
                {
                    if (monsterManager.PlaceSlime(x, y))
                    {
                        MonsterData data = monsterManager.GetMonster(x, y);
                        monsterRenderer.CreateMonsterView(x, y, data);
                        Debug.Log($"[InputHandler] Auto-spawned Slime at ({x},{y}) from tile attribute.");
                    }
                    gridManager.SetTileAttribute(x, y, TileAttributeData.Default);
                }
            }
            return;
        }

        if (cell == CellType.Empty)
        {
            Debug.Log($"[InputHandler] Empty tile clicked at ({x},{y}), no action.");
            return;
        }

        // CellType.Entrance / DemonLordRoom → 不做任何操作
    }
}
